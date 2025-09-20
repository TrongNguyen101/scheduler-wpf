using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using SchedulerWpfApp.Model;
using SchedulerWpfApp.Helper;

namespace SchedulerWpfApp.Data
{
    /// <summary>
    /// Database context class that provides access to the application's data.
    /// Uses Entity Framework Core with SQLite as the database provider.
    /// </summary>
    public class DataContext : DbContext
    {
        /// <summary>
        /// Default constructor for the DataContext.
        /// </summary>
        public DataContext()
        {
        }

        /// <summary>
        /// Constructor that accepts DbContextOptions for configuration.
        /// Used for dependency injection and testing scenarios.
        /// </summary>
        /// <param name="options">The options to configure the context.</param>
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
        }

        /// <summary>
        /// DbSet representing the Persons table in the database.
        /// </summary>
        public DbSet<Subject> Subjects { get; set; } = null!;
        public DbSet<Lecturer> Lecturers { get; set; } = null!;
        public DbSet<LecturerSubject> LecturerSubjects { get; set; } = null!;
        public DbSet<GroupClass> GroupName { get; set; }
        public DbSet<Schedule> Schedules { get; set; }
        public DbSet<LecturerRequest> LecturerRequests { get; set; } = null!;
        public DbSet<Room> Rooms { get; set; } = null!;
        public DbSet<Curriculum> Curriculums { get; set; } = null!;
        public DbSet<CurriculumSubject> CurriculumSubjects { get; set; } = null!;
        /// <summary>
        /// Configures the database connection if not already configured.
        /// Creates the necessary directories for the SQLite database file if they don't exist.
        /// </summary>
        /// <param name="optionsBuilder">The builder used to configure the context options.</param>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            try
            {
                // Use the helper class to configure SQLite properly
                SqliteConnectionHelper.ConfigureDbContext(optionsBuilder);
            }
            catch (Exception ex)
            {
                throw new Exception("Error configuring database connection", ex);
            }
        }

        /// <summary>
        /// Configure database settings like WAL mode using PRAGMA statements
        /// </summary>
        private async Task ConfigureDatabaseSettings()
        {
            try
            {
                // Get a configured connection and apply settings
                var dbPath = SqliteConnectionHelper.GetDatabasePath();
                using var connection = await SqliteConnectionHelper.CreateConfiguredConnectionAsync(dbPath);
                // Settings are already applied by the helper
            }
            catch (Exception ex)
            {
                // Log but don't fail if PRAGMA settings can't be applied
                System.Diagnostics.Debug.WriteLine($"Could not configure database settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Ensures database is created and configured properly
        /// </summary>
        public async Task EnsureDatabaseConfiguredAsync()
        {
            await Database.EnsureCreatedAsync();
            await ConfigureDatabaseSettings();
        }

        /// <summary>
        /// Gets the database path using the helper
        /// </summary>
        private string GetDatabasePath()
        {
            return SqliteConnectionHelper.GetDatabasePath();
        }

        /// <summary>
        /// Creates a backup using VACUUM INTO command through Entity Framework
        /// This is the most efficient backup method for SQLite databases
        /// </summary>
        /// <param name="backupPath">Path where backup will be created</param>
        /// <param name="progress">Optional progress reporting</param>
        /// <returns>True if backup was successful</returns>
        public async Task<bool> CreateBackupUsingVacuumIntoAsync(string backupPath, IProgress<(string message, double percentage)>? progress = null)
        {
            try
            {
                progress?.Report(("Preparing database for VACUUM INTO...", 0));
                System.Diagnostics.Debug.WriteLine($"Creating VACUUM INTO backup: {backupPath}");

                // Ensure we have a clean state
                if (ChangeTracker.HasChanges())
                {
                    await SaveChangesAsync();
                }

                progress?.Report(("Executing VACUUM INTO command...", 20));

                // Use raw SQL to execute VACUUM INTO
                // Note: VACUUM INTO requires a literal path, not a parameter
                var escapedBackupPath = backupPath.Replace("'", "''");
                var sql = $"VACUUM INTO '{escapedBackupPath}';";

#pragma warning disable EF1002 // Risk of vulnerability to SQL injection
                await Database.ExecuteSqlRawAsync(sql);
#pragma warning restore EF1002 // Risk of vulnerability to SQL injection

                progress?.Report(("Verifying backup...", 80));

                // Verify backup was created successfully
                if (!File.Exists(backupPath))
                {
                    throw new InvalidOperationException("Backup file was not created");
                }

                var backupSize = new FileInfo(backupPath).Length;
                if (backupSize == 0)
                {
                    throw new InvalidOperationException("Backup file is empty");
                }

                progress?.Report(("Backup completed successfully!", 100));
                System.Diagnostics.Debug.WriteLine($"VACUUM INTO backup completed. Size: {backupSize:N0} bytes");

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"VACUUM INTO backup failed: {ex.Message}");
                progress?.Report(($"Backup failed: {ex.Message}", 0));

                // Clean up failed backup
                try
                {
                    if (File.Exists(backupPath))
                    {
                        File.Delete(backupPath);
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
        /// Gets SQLite version information
        /// </summary>
        /// <returns>SQLite version string</returns>
        public async Task<string> GetSqliteVersionAsync()
        {
            try
            {
                var result = await Database.SqlQueryRaw<string>("SELECT sqlite_version();").FirstOrDefaultAsync();
                return result ?? "Unknown";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to get SQLite version: {ex.Message}");
                return "Unknown";
            }
        }

        /// <summary>
        /// Checks if the current SQLite version supports VACUUM INTO
        /// VACUUM INTO was introduced in SQLite 3.44.0
        /// </summary>
        /// <returns>True if VACUUM INTO is supported</returns>
        public async Task<bool> IsVacuumIntoSupportedAsync()
        {
            try
            {
                var versionString = await GetSqliteVersionAsync();

                if (Version.TryParse(versionString, out var version))
                {
                    var minimumVersion = new Version(3, 44, 0);
                    bool isSupported = version >= minimumVersion;

                    System.Diagnostics.Debug.WriteLine($"SQLite version: {versionString}, VACUUM INTO supported: {isSupported}");
                    return isSupported;
                }

                System.Diagnostics.Debug.WriteLine($"Could not parse SQLite version: {versionString}");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to check VACUUM INTO support: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Tests VACUUM INTO functionality safely
        /// Creates a small backup to verify the feature works
        /// </summary>
        /// <returns>True if VACUUM INTO works correctly</returns>
        public async Task<bool> TestVacuumIntoFunctionalityAsync()
        {
            try
            {
                var tempBackupPath = Path.GetTempFileName();

                try
                {
                    // Try to create a test backup
                    var escapedPath = tempBackupPath.Replace("'", "''");
                    var sql = $"VACUUM INTO '{escapedPath}';";

#pragma warning disable EF1002 // Risk of vulnerability to SQL injection
                    await Database.ExecuteSqlRawAsync(sql);
#pragma warning restore EF1002 // Risk of vulnerability to SQL injection

                    // Verify the backup was created and has content
                    bool success = File.Exists(tempBackupPath) && new FileInfo(tempBackupPath).Length > 0;

                    System.Diagnostics.Debug.WriteLine($"VACUUM INTO test result: {success}");
                    return success;
                }
                finally
                {
                    // Clean up test file
                    if (File.Exists(tempBackupPath))
                    {
                        File.Delete(tempBackupPath);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"VACUUM INTO test failed: {ex.Message}");
                return false;
            }
        }        /// <summary>
                 /// Disposes the DataContext and ensures proper cleanup of database connections
                 /// </summary>
        public override void Dispose()
        {
            try
            {
                // Clear change tracker to release any cached entities
                ChangeTracker.Clear();

                // Force close and dispose of database connection
                var connection = Database.GetDbConnection();
                if (connection.State != System.Data.ConnectionState.Closed)
                {
                    Database.CloseConnection();
                }

                // Execute SQLite cleanup commands to release locks
                try
                {
                    Database.ExecuteSqlRaw("PRAGMA wal_checkpoint(TRUNCATE);");
                    Database.ExecuteSqlRaw("PRAGMA optimize;");
                }
                catch
                {
                    // Ignore errors during cleanup
                }
            }
            catch (Exception)
            {
                // Ignore disposal errors
            }
            finally
            {
                base.Dispose();
            }
        }

        /// <summary>
        /// Async version of Dispose for proper cleanup
        /// </summary>
        public override async ValueTask DisposeAsync()
        {
            try
            {
                // Clear change tracker to release any cached entities
                ChangeTracker.Clear();

                // Force close and dispose of database connection
                var connection = Database.GetDbConnection();
                if (connection.State != System.Data.ConnectionState.Closed)
                {
                    await Database.CloseConnectionAsync();
                }

                // Execute SQLite cleanup commands to release locks
                try
                {
                    await Database.ExecuteSqlRawAsync("PRAGMA wal_checkpoint(TRUNCATE);");
                    await Database.ExecuteSqlRawAsync("PRAGMA optimize;");
                }
                catch
                {
                    // Ignore errors during cleanup
                }
            }
            catch (Exception)
            {
                // Ignore disposal errors
            }
            finally
            {
                await base.DisposeAsync();
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure the index for CurriculumCode and SubjectCode in the CurriculumSubject entity
            modelBuilder.Entity<CurriculumSubject>()
                   .HasIndex(c => new { c.CurriculumCode, c.SubjectCode })
                   .HasDatabaseName("IX_CurriculumCode_SubjectCode");

            // Configure the index of CurriculumCode in the GroupClass entity
            modelBuilder.Entity<GroupClass>()
                    .HasIndex(g => new { g.CurriculumCode })
                    .HasDatabaseName("IX_CurriculumCode");

            // Create a composite index for LecturerId and SubjectCode
            modelBuilder.Entity<LecturerSubject>()
                    .HasIndex(ls => new { ls.LecturerId, ls.SubjectCode })  // Composite index for LecturerId and SubjectCode
                    .HasDatabaseName("IX_LecturerId_SubjectCode");  // Name of the index

            // Configure the index for RoomName in the Room entity
            modelBuilder.Entity<Room>()
                    .HasIndex(r => r.RoomName)
                    .HasDatabaseName("IX_RoomName");



            modelBuilder.Entity<LecturerSubject>(entity =>
            {
                // Relation: LecturerSubject → Subject (Many-to-One)
                entity.HasOne(ls => ls.Subject)
                      .WithMany(s => s.LecturerSubjects)
                      .HasForeignKey(ls => ls.SubjectCode)
                      .OnDelete(DeleteBehavior.Restrict);
                // Relation: LecturerSubject → Lecturer (Many-to-One)
                entity.HasOne(ls => ls.Lecturer)
                      .WithMany(l => l.LecturerSubjects)
                      .HasForeignKey(ls => ls.LecturerId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<LecturerRequest>(LecturerRequest =>
            {
                // Relation: LecturerRequest → Lecturer (Many-to-One)
                LecturerRequest.HasOne(lr => lr.Lecturer)
                               .WithMany(l => l.LecturerRequests)
                               .HasForeignKey(lr => lr.LecturerId)
                               .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<CurriculumSubject>(entity =>
            {
                // Relation: CurriculumSubject → Subject (Many-to-One)
                entity.HasOne(cs => cs.Subject)
                            .WithMany(s => s.CurriculumSubjects)
                            .HasForeignKey(cs => cs.SubjectCode)
                            .OnDelete(DeleteBehavior.Restrict);
                // Relation: CurriculumSubject → Curriculum (Many-to-One)
                entity.HasOne(cs => cs.Curriculum)
                            .WithMany(c => c.CurriculumSubjects)
                            .HasForeignKey(cs => cs.CurriculumCode)
                            .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
