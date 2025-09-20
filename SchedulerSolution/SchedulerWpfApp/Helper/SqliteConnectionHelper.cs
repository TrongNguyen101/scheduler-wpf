using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SchedulerWpfApp.Helper
{
    /// <summary>
    /// Helper class to manage SQLite connections with proper configuration
    /// Handles Microsoft.Data.Sqlite limitations regarding connection string parameters
    /// </summary>
    public static class SqliteConnectionHelper
    {
        /// <summary>
        /// Gets the standardized database path for the application
        /// </summary>
        public static string GetDatabasePath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SchedulerApp",
                "app.db"
            );
        }

        /// <summary>
        /// Creates a proper SQLite connection string with only supported parameters
        /// Microsoft.Data.Sqlite only supports: Data Source, Mode, Cache, Password, Foreign Keys, Recursive Triggers
        /// </summary>
        /// <param name="databasePath">Path to the database file</param>
        /// <param name="enableForeignKeys">Whether to enable foreign key constraints</param>
        /// <returns>Valid connection string for Microsoft.Data.Sqlite</returns>
        public static string CreateConnectionString(string databasePath, bool enableForeignKeys = true)
        {
            // Ensure directory exists
            var directory = Path.GetDirectoryName(databasePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Use only supported connection string parameters
            return $"Data Source={databasePath};Cache=Shared;Foreign Keys={enableForeignKeys}";
        }

        /// <summary>
        /// Creates and configures a SQLite connection with optimized settings
        /// </summary>
        /// <param name="databasePath">Path to the database file</param>
        /// <param name="enableForeignKeys">Whether to enable foreign key constraints</param>
        /// <returns>Configured SqliteConnection</returns>
        public static async Task<SqliteConnection> CreateConfiguredConnectionAsync(string databasePath, bool enableForeignKeys = true)
        {
            var connectionString = CreateConnectionString(databasePath, enableForeignKeys);
            var connection = new SqliteConnection(connectionString);

            await connection.OpenAsync();
            await ConfigureConnectionAsync(connection);

            return connection;
        }

        /// <summary>
        /// Configures SQLite connection with optimal settings using PRAGMA commands
        /// </summary>
        /// <param name="connection">Open SQLite connection</param>
        public static async Task ConfigureConnectionAsync(SqliteConnection connection)
        {
            if (connection.State != System.Data.ConnectionState.Open)
            {
                throw new InvalidOperationException("Connection must be open before configuration");
            }

            try
            {
                // Set WAL mode for better concurrent access
                await ExecutePragmaAsync(connection, "journal_mode", "WAL");

                // Set busy timeout (30 seconds) - using PRAGMA command, not connection string
                await ExecutePragmaAsync(connection, "busy_timeout", "30000");

                // Set synchronous mode for better performance with WAL
                await ExecutePragmaAsync(connection, "synchronous", "NORMAL");

                // Set cache size (10MB)
                await ExecutePragmaAsync(connection, "cache_size", "10000");

                // Store temporary tables in memory
                await ExecutePragmaAsync(connection, "temp_store", "MEMORY");

                // Enable query planner optimization
                await ExecutePragmaAsync(connection, "optimize");
            }
            catch (Exception ex)
            {
                // Log but don't fail - these are optimization settings
                System.Diagnostics.Debug.WriteLine($"Failed to configure SQLite connection: {ex.Message}");
            }
        }

        /// <summary>
        /// Executes a PRAGMA command on the connection
        /// </summary>
        /// <param name="connection">Open SQLite connection</param>
        /// <param name="pragmaName">Name of the PRAGMA setting</param>
        /// <param name="value">Value to set (optional for some PRAGMAs like optimize)</param>
        private static async Task ExecutePragmaAsync(SqliteConnection connection, string pragmaName, string? value = null)
        {
            var command = connection.CreateCommand();
            if (string.IsNullOrEmpty(value))
            {
                command.CommandText = $"PRAGMA {pragmaName};";
            }
            else
            {
                command.CommandText = $"PRAGMA {pragmaName}={value};";
            }

            await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Configures DbContextOptionsBuilder for Entity Framework with proper SQLite settings
        /// </summary>
        /// <param name="optionsBuilder">The DbContextOptionsBuilder to configure</param>
        /// <param name="databasePath">Path to the database file (optional, uses default if not provided)</param>
        public static void ConfigureDbContext(DbContextOptionsBuilder optionsBuilder, string? databasePath = null)
        {
            if (optionsBuilder.IsConfigured) return;

            databasePath ??= GetDatabasePath();
            var connectionString = CreateConnectionString(databasePath);

            optionsBuilder.UseSqlite(connectionString, options =>
            {
                options.CommandTimeout(30);
            });

            // Add logging for debug builds
#if DEBUG
            optionsBuilder.EnableSensitiveDataLogging();
            optionsBuilder.LogTo(message => System.Diagnostics.Debug.WriteLine(message), Microsoft.Extensions.Logging.LogLevel.Warning);
#endif
        }

        /// <summary>
        /// Performs database cleanup operations
        /// </summary>
        /// <param name="databasePath">Path to the database file</param>
        public static async Task CleanupDatabaseAsync(string databasePath)
        {
            try
            {
                using var connection = await CreateConfiguredConnectionAsync(databasePath);

                // Checkpoint WAL file
                await ExecutePragmaAsync(connection, "wal_checkpoint", "TRUNCATE");

                // Optimize database
                await ExecutePragmaAsync(connection, "optimize");

                await connection.CloseAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Database cleanup failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Forces garbage collection and waits to help release database file locks
        /// </summary>
        public static async Task ForceReleaseLocksAsync()
        {
            // Force garbage collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // Wait for any pending I/O operations
            await Task.Delay(500);
        }

        /// <summary>
        /// Forces aggressive database connection cleanup for backup/restore operations
        /// Uses multiple strategies to ensure all connections are closed
        /// </summary>
        /// <param name="databasePath">Path to the database file</param>
        /// <returns>True if database is ready for file operations</returns>
        public static async Task<bool> ForceCloseAllConnectionsAsync(string databasePath)
        {
            const int maxAttempts = 5;
            const int delayBetweenAttempts = 1000; // 1 second

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    // Strategy 1: Clear connection pools
                    SqliteConnection.ClearAllPools();

                    // Strategy 2: Force garbage collection
                    await ForceReleaseLocksAsync();

                    // Strategy 3: Try to acquire exclusive access
                    using var testConnection = new SqliteConnection($"Data Source={databasePath};Mode=ReadWriteCreate;Cache=Private");
                    await testConnection.OpenAsync();

                    // Strategy 4: Execute aggressive cleanup
                    await ExecutePragmaAsync(testConnection, "wal_checkpoint", "TRUNCATE");
                    await ExecutePragmaAsync(testConnection, "journal_mode", "DELETE");
                    await ExecutePragmaAsync(testConnection, "journal_mode", "WAL");
                    await ExecutePragmaAsync(testConnection, "optimize");

                    await testConnection.CloseAsync();

                    // Strategy 5: Final verification - try to access file
                    if (CanAccessFileExclusively(databasePath))
                    {
                        System.Diagnostics.Debug.WriteLine($"Database ready for file operations after {attempt} attempts");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Attempt {attempt} failed: {ex.Message}");
                }

                if (attempt < maxAttempts)
                {
                    System.Diagnostics.Debug.WriteLine($"Waiting {delayBetweenAttempts}ms before retry...");
                    await Task.Delay(delayBetweenAttempts);
                }
            }

            System.Diagnostics.Debug.WriteLine("Failed to prepare database for file operations after all attempts");
            return false;
        }

        /// <summary>
        /// Tests if a file can be accessed exclusively (not locked)
        /// </summary>
        /// <param name="filePath">Path to the file to test</param>
        /// <returns>True if file is not locked</returns>
        private static bool CanAccessFileExclusively(string filePath)
        {
            try
            {
                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Creates a connection with aggressive settings for backup operations
        /// </summary>
        /// <param name="databasePath">Path to the database file</param>
        /// <returns>Configured SQLite connection for backup operations</returns>
        public static async Task<SqliteConnection> CreateBackupConnectionAsync(string databasePath)
        {
            // Use private cache to avoid connection pool issues
            var connectionString = $"Data Source={databasePath};Mode=ReadOnly;Cache=Private;Pooling=false";
            var connection = new SqliteConnection(connectionString);

            await connection.OpenAsync();

            // Set aggressive timeout settings for backup
            await ExecutePragmaAsync(connection, "busy_timeout", "30000"); // 30 seconds
            await ExecutePragmaAsync(connection, "synchronous", "NORMAL");

            return connection;
        }

        /// <summary>
        /// Performs comprehensive database shutdown for backup/restore operations
        /// This method ensures ALL connections are closed and file locks are released
        /// </summary>
        /// <param name="databasePath">Path to the database file</param>
        /// <returns>True if database is ready for file operations</returns>
        public static async Task<bool> PerformDatabaseShutdownAsync(string databasePath)
        {
            try
            {
                // Phase 1: Clear all connection pools
                SqliteConnection.ClearAllPools();

                // Phase 2: Force garbage collection multiple times
                for (int i = 0; i < 3; i++)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    await Task.Delay(200);
                }

                // Phase 3: Try to perform WAL checkpoint and close
                try
                {
                    using var cleanupConnection = new SqliteConnection($"Data Source={databasePath};Cache=Private;Pooling=false");
                    await cleanupConnection.OpenAsync();
                    await ExecutePragmaAsync(cleanupConnection, "wal_checkpoint", "TRUNCATE");
                    await ExecutePragmaAsync(cleanupConnection, "optimize");
                    await cleanupConnection.CloseAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"WAL checkpoint warning: {ex.Message}");
                }

                // Phase 4: Final delay for file system operations
                await Task.Delay(1000);

                // Phase 5: Verify file access
                bool canAccess = CanAccessFileExclusively(databasePath);

                return canAccess;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Database shutdown failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Creates a backup of the database using VACUUM INTO command
        /// This is the most efficient way to backup SQLite databases (available since SQLite 3.44.0)
        /// VACUUM INTO creates a clean, compact copy without WAL files or temporary data
        /// </summary>
        /// <param name="sourceDatabasePath">Path to the source database</param>
        /// <param name="backupDatabasePath">Path where backup will be created</param>
        /// <param name="progress">Optional progress reporting callback</param>
        /// <returns>True if backup was successful</returns>
        public static async Task<bool> CreateBackupUsingVacuumIntoAsync(
            string sourceDatabasePath,
            string backupDatabasePath,
            IProgress<(string message, double percentage)>? progress = null)
        {
            try
            {
                progress?.Report(("Starting VACUUM INTO backup...", 0));
                System.Diagnostics.Debug.WriteLine($"Creating backup using VACUUM INTO: {sourceDatabasePath} -> {backupDatabasePath}");

                // Ensure backup directory exists
                var backupDirectory = Path.GetDirectoryName(backupDatabasePath);
                if (!string.IsNullOrEmpty(backupDirectory) && !Directory.Exists(backupDirectory))
                {
                    Directory.CreateDirectory(backupDirectory);
                }

                // Delete existing backup file if it exists
                if (File.Exists(backupDatabasePath))
                {
                    File.Delete(backupDatabasePath);
                    progress?.Report(("Cleaned existing backup file...", 10));
                }

                // Create connection with optimal settings for VACUUM INTO
                using var connection = await CreateVacuumConnectionAsync(sourceDatabasePath);
                progress?.Report(("Connected to source database...", 20));

                // Execute VACUUM INTO command
                // This creates a complete, compact copy of the database
                var command = connection.CreateCommand();
                command.CommandText = $"VACUUM INTO '{backupDatabasePath.Replace("'", "''")}';";
                command.CommandTimeout = 300; // 5 minutes timeout for large databases

                progress?.Report(("Executing VACUUM INTO operation...", 30));
                await command.ExecuteNonQueryAsync();
                progress?.Report(("VACUUM INTO completed...", 80));

                // Verify backup file was created and has content
                if (!File.Exists(backupDatabasePath))
                {
                    throw new InvalidOperationException("Backup file was not created");
                }

                var backupSize = new FileInfo(backupDatabasePath).Length;
                if (backupSize == 0)
                {
                    throw new InvalidOperationException("Backup file is empty");
                }

                progress?.Report(("Verifying backup integrity...", 90));

                // Verify backup integrity by opening it
                await VerifyBackupIntegrityAsync(backupDatabasePath);

                progress?.Report(("Backup completed successfully!", 100));
                System.Diagnostics.Debug.WriteLine($"VACUUM INTO backup completed successfully. Size: {backupSize:N0} bytes");

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"VACUUM INTO backup failed: {ex.Message}");
                progress?.Report(($"Backup failed: {ex.Message}", 0));

                // Clean up failed backup file
                try
                {
                    if (File.Exists(backupDatabasePath))
                    {
                        File.Delete(backupDatabasePath);
                    }
                }
                catch
                {
                    // Ignore cleanup errors
                }

                return false;
            }
        }

        /// <summary>
        /// Creates a connection optimized for VACUUM INTO operations
        /// </summary>
        /// <param name="databasePath">Path to the database</param>
        /// <returns>Configured SQLite connection</returns>
        private static async Task<SqliteConnection> CreateVacuumConnectionAsync(string databasePath)
        {
            // Use read-only mode with shared cache for VACUUM INTO source
            var connectionString = $"Data Source={databasePath};Mode=ReadOnly;Cache=Shared;Pooling=false";
            var connection = new SqliteConnection(connectionString);

            await connection.OpenAsync();

            // Configure for optimal VACUUM performance
            await ExecutePragmaAsync(connection, "busy_timeout", "300000"); // 5 minutes
            await ExecutePragmaAsync(connection, "cache_size", "50000"); // Larger cache for VACUUM
            await ExecutePragmaAsync(connection, "temp_store", "MEMORY");
            await ExecutePragmaAsync(connection, "synchronous", "NORMAL");

            return connection;
        }

        /// <summary>
        /// Verifies the integrity of a backup database file
        /// </summary>
        /// <param name="backupDatabasePath">Path to the backup database</param>
        /// <returns>True if backup is valid</returns>
        private static async Task<bool> VerifyBackupIntegrityAsync(string backupDatabasePath)
        {
            try
            {
                using var connection = await CreateConfiguredConnectionAsync(backupDatabasePath, false);

                // Run integrity check
                var command = connection.CreateCommand();
                command.CommandText = "PRAGMA integrity_check;";

                var result = await command.ExecuteScalarAsync();
                bool isIntact = result?.ToString() == "ok";

                if (!isIntact)
                {
                    System.Diagnostics.Debug.WriteLine($"Backup integrity check failed: {result}");
                }

                return isIntact;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Backup integrity verification failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Restores a database from a backup using file copy
        /// The backup should be created using VACUUM INTO for best results
        /// </summary>
        /// <param name="backupDatabasePath">Path to the backup database</param>
        /// <param name="targetDatabasePath">Path where database will be restored</param>
        /// <param name="progress">Optional progress reporting callback</param>
        /// <returns>True if restore was successful</returns>
        public static async Task<bool> RestoreFromBackupAsync(
            string backupDatabasePath,
            string targetDatabasePath,
            IProgress<(string message, double percentage)>? progress = null)
        {
            try
            {
                progress?.Report(("Starting database restore...", 0));
                System.Diagnostics.Debug.WriteLine($"Restoring database: {backupDatabasePath} -> {targetDatabasePath}");

                // Verify backup file exists and is valid
                if (!File.Exists(backupDatabasePath))
                {
                    throw new FileNotFoundException($"Backup file not found: {backupDatabasePath}");
                }

                progress?.Report(("Verifying backup file...", 10));

                // Verify backup integrity before restore
                bool isValid = await VerifyBackupIntegrityAsync(backupDatabasePath);
                if (!isValid)
                {
                    throw new InvalidOperationException("Backup file is corrupted or invalid");
                }

                progress?.Report(("Preparing target database...", 20));

                // Ensure target database is properly closed
                await PerformDatabaseShutdownAsync(targetDatabasePath);

                progress?.Report(("Copying backup file...", 40));

                // Create target directory if needed
                var targetDirectory = Path.GetDirectoryName(targetDatabasePath);
                if (!string.IsNullOrEmpty(targetDirectory) && !Directory.Exists(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                }

                // Remove existing target files (main DB, WAL, SHM)
                var baseFileName = Path.GetFileNameWithoutExtension(targetDatabasePath);
                var targetDir = Path.GetDirectoryName(targetDatabasePath);
                var extensions = new[] { "", "-wal", "-shm" };

                foreach (var ext in extensions)
                {
                    var fileToDelete = Path.Combine(targetDir!, baseFileName + Path.GetExtension(targetDatabasePath) + ext);
                    if (File.Exists(fileToDelete))
                    {
                        File.Delete(fileToDelete);
                    }
                }

                progress?.Report(("Copying database file...", 60));

                // Copy backup to target location
                File.Copy(backupDatabasePath, targetDatabasePath, overwrite: true);

                progress?.Report(("Verifying restored database...", 80));

                // Verify restored database
                bool restoreValid = await VerifyBackupIntegrityAsync(targetDatabasePath);
                if (!restoreValid)
                {
                    throw new InvalidOperationException("Restored database failed integrity check");
                }

                progress?.Report(("Database restore completed!", 100));
                System.Diagnostics.Debug.WriteLine("Database restore completed successfully");

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Database restore failed: {ex.Message}");
                progress?.Report(($"Restore failed: {ex.Message}", 0));
                return false;
            }
        }

        /// <summary>
        /// Gets SQLite version information to verify VACUUM INTO support
        /// VACUUM INTO is available since SQLite 3.44.0
        /// </summary>
        /// <returns>SQLite version string</returns>
        public static async Task<string> GetSqliteVersionAsync()
        {
            try
            {
                var tempDb = ":memory:";
                using var connection = new SqliteConnection($"Data Source={tempDb}");
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = "SELECT sqlite_version();";

                var version = await command.ExecuteScalarAsync();
                return version?.ToString() ?? "Unknown";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to get SQLite version: {ex.Message}");
                return "Unknown";
            }
        }

        /// <summary>
        /// Checks if VACUUM INTO is supported by the current SQLite version
        /// </summary>
        /// <returns>True if VACUUM INTO is supported</returns>
        public static async Task<bool> IsVacuumIntoSupportedAsync()
        {
            try
            {
                var version = await GetSqliteVersionAsync();
                if (Version.TryParse(version, out var sqliteVersion))
                {
                    // VACUUM INTO was introduced in SQLite 3.44.0
                    var minimumVersion = new Version(3, 44, 0);
                    return sqliteVersion >= minimumVersion;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to check VACUUM INTO support: {ex.Message}");
            }

            return false;
        }
    }
}