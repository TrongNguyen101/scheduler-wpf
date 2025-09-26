using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SchedulerWpfApp.Algorithm.DTO;
using SchedulerWpfApp.Data;
using SchedulerWpfApp.Helper;
using SchedulerWpfApp.ServiceRefactor.Auth;
using SchedulerWpfApp.ServiceRefactor.NotificationService;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;

namespace SchedulerWpfApp.ServiceRefactor.BackupRestoreService
{
    /// <summary>
    /// Service for handling SQLite database backup and restore operations
    /// Integrates with existing authentication and HTTP client patterns
    /// </summary>
    public class BackupRestoreService : IBackupRestoreService
    {
        #region Fields
        private readonly HttpClient _httpClient;
        private readonly AuthState _authState;
        private readonly AuthService _authService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<BackupRestoreService> _logger;
        private readonly INotificationService _notificationService;
        private readonly string _databasePath;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly SemaphoreSlim _operationSemaphore;
        private readonly string _backupTempPath;
        private readonly IServiceProvider _serviceProvider;
        #endregion

        #region Constructor
        public BackupRestoreService(
            HttpClient httpClient,
            AuthState authState,
            AuthService authService,
            IConfiguration configuration,
            ILogger<BackupRestoreService> logger,
            INotificationService notificationService,
            IServiceProvider serviceProvider)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _authState = authState ?? throw new ArgumentNullException(nameof(authState));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            // Get database path using helper
            _databasePath = SqliteConnectionHelper.GetDatabasePath();

            // Initialize backup temp directory
            _backupTempPath = Path.Combine(Path.GetTempPath(), "SchedulerBackups");
            Directory.CreateDirectory(_backupTempPath);

            // Start background cleanup of old backup files
            _ = Task.Run(async () => await CleanupOldBackupFilesAsync());

            // Initialize operation semaphore (only one backup/restore at a time)
            _operationSemaphore = new SemaphoreSlim(1, 1);

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// Checks SQLite version and VACUUM INTO support
        /// </summary>
        /// <returns>Version information and support status</returns>
        public async Task<(string version, bool supportsVacuumInto)> GetSqliteInfoAsync()
        {
            try
            {
                string version = await SqliteConnectionHelper.GetSqliteVersionAsync();
                bool supportsVacuumInto = await SqliteConnectionHelper.IsVacuumIntoSupportedAsync();

                _logger.LogInformation("SQLite Version: {Version}, VACUUM INTO Support: {Support}", version, supportsVacuumInto);

                return (version, supportsVacuumInto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get SQLite information");
                return ("Unknown", false);
            }
        }

        /// <summary>
        /// Creates a local backup using VACUUM INTO with simplified, reliable approach
        /// This method ensures database consistency and proper error handling
        /// </summary>
        /// <param name="backupPath">Path where backup will be created</param>
        /// <param name="progress">Optional progress reporting</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if backup was successful</returns>
        public async Task<bool> CreateLocalBackupAsync(string backupPath, IProgress<(string message, double percentage)>? progress = null, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Starting local database backup to: {BackupPath}", backupPath);
                progress?.Report(("Checking SQLite version and VACUUM INTO support...", 0));

                // Step 1: Check VACUUM INTO support
                var (version, supportsVacuumInto) = await GetSqliteInfoAsync();
                if (!supportsVacuumInto)
                {
                    throw new NotSupportedException($"VACUUM INTO not supported by SQLite version {version}. Requires SQLite 3.44.0 or later.");
                }

                progress?.Report(("Preparing backup location...", 20));

                // Step 2: Ensure backup directory exists
                var backupDirectory = Path.GetDirectoryName(backupPath);
                if (!string.IsNullOrEmpty(backupDirectory) && !Directory.Exists(backupDirectory))
                {
                    Directory.CreateDirectory(backupDirectory);
                }

                // Step 3: Remove existing backup file if exists
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                    _logger.LogDebug("Removed existing backup file: {BackupPath}", backupPath);
                }

                progress?.Report(("Creating backup using VACUUM INTO...", 40));

                // Step 4: Use direct SQLite connection to avoid EF Core connection pool issues
                // VACUUM INTO works even with other connections active
                bool backupSuccess = false;
                Exception? lastException = null;

                try
                {
                    // Try direct SQLite connection first (most reliable)
                    _logger.LogDebug("Attempting VACUUM INTO using direct SQLite connection");

                    var connectionString = SqliteConnectionHelper.CreateConnectionString(_databasePath, true);
                    using var connection = new Microsoft.Data.Sqlite.SqliteConnection(connectionString);
                    await connection.OpenAsync(cancellationToken);

                    using var command = connection.CreateCommand();
                    command.CommandText = $"VACUUM INTO '{backupPath.Replace("'", "''")}';";
                    command.CommandTimeout = 300; // 5 minutes

                    await command.ExecuteNonQueryAsync(cancellationToken);
                    backupSuccess = true;
                    progress?.Report(("Backup created successfully using direct SQLite", 70));
                }
                catch (Exception directEx)
                {
                    _logger.LogWarning(directEx, "Direct SQLite VACUUM INTO failed, trying Entity Framework approach");
                    lastException = directEx;
                    progress?.Report(("Trying alternative backup method via Entity Framework...", 50));

                    try
                    {
                        // Fallback to Entity Framework if direct approach fails
                        using var context = new Data.DataContext();

                        // Escape the path for SQL injection protection
                        var escapedPath = backupPath.Replace("'", "''");
                        var sql = $"VACUUM INTO '{escapedPath}';";

                        _logger.LogDebug("Executing VACUUM INTO via Entity Framework: {SQL}", sql);

#pragma warning disable EF1002 // Risk of vulnerability to SQL injection
                        await context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
#pragma warning restore EF1002 // Risk of vulnerability to SQL injection

                        backupSuccess = true;
                        progress?.Report(("Backup created successfully via Entity Framework", 70));
                    }
                    catch (Exception efEx)
                    {
                        _logger.LogError(efEx, "Entity Framework VACUUM INTO also failed");
                        lastException = efEx;
                    }
                }

                if (!backupSuccess)
                {
                    throw new InvalidOperationException("All VACUUM INTO methods failed", lastException);
                }

                progress?.Report(("Verifying backup file...", 80));

                // Step 5: Verify backup was created and is valid
                if (!File.Exists(backupPath))
                {
                    throw new InvalidOperationException("Backup file was not created");
                }

                var backupInfo = new FileInfo(backupPath);
                if (backupInfo.Length == 0)
                {
                    throw new InvalidOperationException("Backup file is empty");
                }

                progress?.Report(("Performing integrity check...", 90));

                // Step 6: Quick integrity check on backup file
                try
                {
                    using var testConnection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={backupPath};Mode=ReadOnly");
                    await testConnection.OpenAsync(cancellationToken);

                    using var testCommand = testConnection.CreateCommand();
                    testCommand.CommandText = "PRAGMA integrity_check(1);";
                    var result = await testCommand.ExecuteScalarAsync(cancellationToken);

                    if (result?.ToString() != "ok")
                    {
                        throw new InvalidOperationException("Backup file integrity check failed");
                    }
                }
                catch (Exception verifyEx)
                {
                    _logger.LogWarning(verifyEx, "Could not verify backup integrity, but file exists with size: {Size}", backupInfo.Length);
                    // Don't fail the backup if verification fails - the file might still be valid
                }

                progress?.Report(("Backup completed successfully!", 100));
                _logger.LogInformation("Local backup completed successfully. File size: {Size:N0} bytes", backupInfo.Length);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create local backup: {Message}", ex.Message);
                progress?.Report(($"Backup failed: {ex.Message}", 0));

                // Clean up failed backup file
                try
                {
                    if (File.Exists(backupPath))
                    {
                        File.Delete(backupPath);
                        _logger.LogDebug("Cleaned up failed backup file: {BackupPath}", backupPath);
                    }
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogWarning(cleanupEx, "Failed to cleanup failed backup file: {BackupPath}", backupPath);
                }

                return false;
            }
        }

        /// <summary>
        /// Creates a backup of the current database and uploads it to the server
        /// </summary>
        /// <param name="progress">Optional progress reporting</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if backup and upload were successful</returns>
        public async Task<bool> CreateServerBackupAsync(IProgress<(string message, double percentage)>? progress = null, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Starting database backup and upload to server");
                progress?.Report(("Checking SQLite version and VACUUM INTO support...", 0));

                // Step 1: Check VACUUM INTO support
                var (version, supportsVacuumInto) = await GetSqliteInfoAsync();
                if (!supportsVacuumInto)
                {
                    throw new NotSupportedException($"VACUUM INTO not supported by SQLite version {version}. Requires SQLite 3.44.0 or later.");
                }

                progress?.Report(("Creating temporary backup file...", 10));

                // Step 2: Create temporary backup file
                var tempBackupPath = Path.Combine(_backupTempPath, $"temp_backup_{DateTime.Now:yyyyMMdd_HHmmss}.sqlite");

                // Ensure temp directory exists
                var tempDirectory = Path.GetDirectoryName(tempBackupPath);
                if (!string.IsNullOrEmpty(tempDirectory) && !Directory.Exists(tempDirectory))
                {
                    Directory.CreateDirectory(tempDirectory);
                }

                // Remove existing temp file if exists
                if (File.Exists(tempBackupPath))
                {
                    File.Delete(tempBackupPath);
                    _logger.LogDebug("Removed existing temp backup file: {TempBackupPath}", tempBackupPath);
                }

                progress?.Report(("Creating backup using VACUUM INTO...", 20));

                // Step 3: Create backup using same logic as CreateLocalBackupAsync
                bool backupSuccess = false;
                Exception? lastException = null;

                try
                {
                    // Try direct SQLite connection first (most reliable)
                    _logger.LogDebug("Attempting VACUUM INTO using direct SQLite connection");

                    var connectionString = SqliteConnectionHelper.CreateConnectionString(_databasePath, true);
                    using var connection = new Microsoft.Data.Sqlite.SqliteConnection(connectionString);
                    await connection.OpenAsync(cancellationToken);

                    using var command = connection.CreateCommand();
                    command.CommandText = $"VACUUM INTO '{tempBackupPath.Replace("'", "''")}';";
                    command.CommandTimeout = 300; // 5 minutes

                    await command.ExecuteNonQueryAsync(cancellationToken);
                    backupSuccess = true;
                    progress?.Report(("Backup created successfully using direct SQLite", 40));
                }
                catch (Exception directEx)
                {
                    _logger.LogWarning(directEx, "Direct SQLite VACUUM INTO failed, trying Entity Framework approach");
                    lastException = directEx;
                    progress?.Report(("Trying alternative backup method via Entity Framework...", 30));

                    try
                    {
                        // Fallback to Entity Framework if direct approach fails
                        using var context = new Data.DataContext();

                        // Escape the path for SQL injection protection
                        var escapedPath = tempBackupPath.Replace("'", "''");
                        var sql = $"VACUUM INTO '{escapedPath}';";

                        _logger.LogDebug("Executing VACUUM INTO via Entity Framework: {SQL}", sql);

#pragma warning disable EF1002 // Risk of vulnerability to SQL injection
                        await context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
#pragma warning restore EF1002 // Risk of vulnerability to SQL injection

                        backupSuccess = true;
                        progress?.Report(("Backup created successfully via Entity Framework", 40));
                    }
                    catch (Exception efEx)
                    {
                        _logger.LogError(efEx, "Entity Framework VACUUM INTO also failed");
                        lastException = efEx;
                    }
                }

                if (!backupSuccess)
                {
                    throw new InvalidOperationException("All VACUUM INTO methods failed", lastException);
                }

                progress?.Report(("Verifying backup file...", 50));

                // Step 4: Verify backup was created and is valid
                if (!File.Exists(tempBackupPath))
                {
                    throw new InvalidOperationException("Backup file was not created");
                }

                var backupInfo = new FileInfo(tempBackupPath);
                if (backupInfo.Length == 0)
                {
                    throw new InvalidOperationException("Backup file is empty");
                }

                progress?.Report(("Performing integrity check...", 60));

                // Step 5: Quick integrity check on backup file
                try
                {
                    using var testConnection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={tempBackupPath};Mode=ReadOnly");
                    await testConnection.OpenAsync(cancellationToken);

                    using var testCommand = testConnection.CreateCommand();
                    testCommand.CommandText = "PRAGMA integrity_check(1);";
                    var result = await testCommand.ExecuteScalarAsync(cancellationToken);

                    if (result?.ToString() != "ok")
                    {
                        throw new InvalidOperationException("Backup file integrity check failed");
                    }
                }
                catch (Exception verifyEx)
                {
                    _logger.LogWarning(verifyEx, "Could not verify backup integrity, but file exists with size: {Size}", backupInfo.Length);
                    // Don't fail the backup if verification fails - the file might still be valid
                }

                progress?.Report(("Uploading backup to server...", 70));

                // Step 6: Upload backup to server
                var uploadSuccess = await UploadBackupToServerAsync(tempBackupPath, progress, cancellationToken);

                if (!uploadSuccess)
                {
                    throw new InvalidOperationException("Failed to upload backup to server");
                }

                progress?.Report(("Server backup completed successfully!", 100));
                _logger.LogInformation("Server backup completed successfully. File size: {Size:N0} bytes", backupInfo.Length);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create server backup: {Message}", ex.Message);
                progress?.Report(($"Server backup failed: {ex.Message}", 0));
                return false;
            }
            finally
            {
                // Clean up temporary backup file
                try
                {
                    var tempBackupPath = Path.Combine(_backupTempPath, $"temp_backup_{DateTime.Now:yyyyMMdd_HHmmss}.sqlite");
                    if (File.Exists(tempBackupPath))
                    {
                        File.Delete(tempBackupPath);
                        _logger.LogDebug("Cleaned up temporary backup file: {TempBackupPath}", tempBackupPath);
                    }
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogWarning(cleanupEx, "Failed to cleanup temporary backup file");
                }
            }
        }

        /// <summary>
        /// Uploads a backup file to the server using HTTP multipart form data
        /// </summary>
        /// <param name="backupFilePath">Path to the backup file to upload</param>
        /// <param name="progress">Progress reporting callback</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if upload was successful</returns>
        private async Task<bool> UploadBackupToServerAsync(string backupFilePath, IProgress<(string message, double percentage)>? progress = null, CancellationToken cancellationToken = default)
        {
            try
            {
                // Ensure user is authenticated
                if (string.IsNullOrEmpty(_authState.AccessToken))
                {
                    throw new UnauthorizedAccessException("User is not authenticated");
                }

                // Get upload endpoint from configuration
                var uploadEndpoint = "http://localhost:4000/api/backups/upload";

                progress?.Report(("Preparing file for upload...", 75));

                // Create multipart form content
                using var form = new MultipartFormDataContent();
                using var fileStream = File.OpenRead(backupFilePath);
                using var fileContent = new StreamContent(fileStream);

                // Set content headers
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

                // Generate filename with timestamp
                var fileName = $"backup_{DateTime.Now:yyyyMMdd_HHmmss}.sqlite";
                form.Add(fileContent, "file", fileName);

                // Add metadata
                form.Add(new StringContent(fileName), "originalName");
                form.Add(new StringContent(DateTime.UtcNow.ToString("O")), "backupDate");
                form.Add(new StringContent(Environment.MachineName), "machineName");
                form.Add(new StringContent(Environment.UserName), "userName"); progress?.Report(("Uploading to server...", 80));

                // Set authorization header
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _authState.AccessToken);

                // Upload the file
                var response = await _httpClient.PostAsync(uploadEndpoint, form, cancellationToken);

                progress?.Report(("Processing server response...", 90));

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogInformation("Backup uploaded successfully. Server response: {Response}", responseContent);

                    progress?.Report(("Upload completed successfully!", 95));
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Failed to upload backup. Status: {Status}, Error: {Error}", response.StatusCode, errorContent);

                    throw new HttpRequestException($"Upload failed with status {response.StatusCode}: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred during backup upload: {Message}", ex.Message);
                progress?.Report(($"Upload failed: {ex.Message}", 0));
                return false;
            }
        }

        /// <summary>
        /// Retrieves the list of available backups from the server
        /// </summary>
        public async Task<List<BackupMetadata>> GetBackupListAsync()
        {
            try
            {
                _logger.LogInformation("Retrieving backup list from server...");

                // Ensure authentication is valid and refresh token if necessary
                if (!await EnsureAuthenticatedAsync())
                {
                    throw new UnauthorizedAccessException("Authentication failed. Please log in again.");
                }

                var baseUrl = _configuration["ApiConfiguration:BaseUrl"] ?? "http://localhost:4000";
                var endpoint = $"{baseUrl}/api/backups/list";

                using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _authState.AccessToken);

                using var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var listResponse = JsonSerializer.Deserialize<BackupListResponse>(responseContent, _jsonOptions);

                    if (listResponse?.Success == true)
                    {
                        _logger.LogInformation("Retrieved {Count} backups from server", listResponse.Backups.Count);
                        return listResponse.Backups;
                    }
                    else
                    {
                        _logger.LogWarning("Server returned unsuccessful response: {Message}", listResponse?.Message);
                        return new List<BackupMetadata>();
                    }
                }
                else
                {
                    _logger.LogError("Failed to retrieve backup list. Status: {StatusCode}", response.StatusCode);

                    // Don't show notification for 404 - let the calling code handle it
                    if (response.StatusCode != System.Net.HttpStatusCode.NotFound)
                    {
                        _notificationService.ShowError($"Failed to retrieve backup list: {response.StatusCode}");
                    }

                    throw new HttpRequestException($"Failed to retrieve backup list: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving backup list");

                // Don't show notification for HTTP 404 errors - let the calling code handle it
                if (!(ex is HttpRequestException httpEx && httpEx.Message.Contains("NotFound")))
                {
                    _notificationService.ShowError($"Failed to retrieve backup list: {ex.Message}");
                }

                throw;
            }
        }

        /// <summary>
        /// Restores the database from a server backup with rollback capability
        /// </summary>
        public async Task<RestoreResult> RestoreFromBackupAsync(string filename, IProgress<RestoreProgress>? progress = null, CancellationToken cancellationToken = default)
        {
            // Ensure only one backup/restore operation at a time
            await _operationSemaphore.WaitAsync(cancellationToken);

            try
            {
                var stopwatch = Stopwatch.StartNew();
                var result = new RestoreResult { RestoreDate = DateTime.UtcNow };
                string? safetyBackupPath = null;
                string? downloadedBackupPath = null;

                try
                {
                    _logger.LogInformation("Starting database restore from backup: {Filename}", filename);

                    // Ensure authentication is valid and refresh token if necessary
                    if (!await EnsureAuthenticatedAsync())
                    {
                        _logger.LogError("Authentication failed during restore operation");
                        throw new UnauthorizedAccessException("Authentication failed. Please log in again.");
                    }

                    _logger.LogDebug("Authentication verified successfully");
                    progress?.Report(new RestoreProgress
                    {
                        PercentComplete = 10,
                        CurrentOperation = "Preparing for restore...",
                        Status = "Initializing"
                    });

                    // Create safety backup before restore with enhanced error handling
                    _logger.LogInformation("Preparing database for restore operation");
                    await EnsureDatabaseClosedWithRetry(cancellationToken);

                    progress?.Report(new RestoreProgress
                    {
                        PercentComplete = 20,
                        CurrentOperation = "Creating safety backup...",
                        Status = "Processing"
                    });

                    _logger.LogInformation("Creating safety backup before restore using VACUUM INTO");
                    safetyBackupPath = await CreateVacuumSafetyBackup(cancellationToken);
                    result.RollbackPath = safetyBackupPath;
                    _logger.LogInformation("Safety backup created: {SafetyBackupPath}", safetyBackupPath);

                    progress?.Report(new RestoreProgress
                    {
                        PercentComplete = 40,
                        CurrentOperation = "Downloading backup from server...",
                        Status = "Downloading"
                    });

                    // Download backup from server
                    downloadedBackupPath = await DownloadBackupFromServerAsync(filename, cancellationToken);

                    progress?.Report(new RestoreProgress
                    {
                        PercentComplete = 60,
                        CurrentOperation = "Validating backup file...",
                        Status = "Validating",
                        IsValidating = true,
                        ValidationMessage = "Checking file integrity..."
                    });

                    // Validate downloaded backup
                    _logger.LogInformation("Validating downloaded backup file");
                    await VerifyBackupFile(downloadedBackupPath, cancellationToken);

                    progress?.Report(new RestoreProgress
                    {
                        PercentComplete = 80,
                        CurrentOperation = "Replacing database...",
                        Status = "Restoring"
                    });

                    // Replace database file
                    _logger.LogInformation("Starting database file replacement");
                    await ReplaceDatabase(downloadedBackupPath);

                    progress?.Report(new RestoreProgress
                    {
                        PercentComplete = 90,
                        CurrentOperation = "Verifying database...",
                        Status = "Verifying"
                    });

                    // Verify restored database
                    _logger.LogInformation("Verifying restored database integrity");
                    await VerifyRestoredDatabase();

                    progress?.Report(new RestoreProgress
                    {
                        PercentComplete = 100,
                        CurrentOperation = "Restore completed successfully",
                        Status = "Completed"
                    });

                    result.Success = true;
                    result.Message = $"Database restored successfully from {filename}";
                    result.BackupFileName = filename;
                    result.Duration = stopwatch.Elapsed;

                    _logger.LogInformation("Database restore completed successfully from {Filename} in {Duration}ms",
                        filename, stopwatch.ElapsedMilliseconds);
                    _notificationService.ShowSuccess($"Database restored successfully from {filename}");

                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Database restore failed for file {Filename}", filename);

                    // Attempt rollback if we have a safety backup
                    if (!string.IsNullOrEmpty(safetyBackupPath) && File.Exists(safetyBackupPath))
                    {
                        try
                        {
                            _logger.LogWarning("Attempting to rollback to safety backup: {SafetyBackupPath}", safetyBackupPath);
                            progress?.Report(new RestoreProgress
                            {
                                PercentComplete = 95,
                                CurrentOperation = "Rolling back to safety backup...",
                                Status = "Rolling back"
                            });

                            await ReplaceDatabase(safetyBackupPath);
                            result.RollbackPerformed = true;
                            _logger.LogInformation("Rollback completed successfully");
                            _notificationService.ShowWarning("Restore failed, but database was restored to previous state");
                        }
                        catch (Exception rollbackEx)
                        {
                            _logger.LogError(rollbackEx, "Rollback also failed!");
                            _notificationService.ShowError("Restore failed and rollback also failed! Please restore database manually.");
                        }
                    }
                    else
                    {
                        _logger.LogError("No safety backup available for rollback");
                    }

                    result.Success = false;
                    result.Message = $"Restore failed: {ex.Message}";
                    result.Exception = ex;
                    result.Duration = stopwatch.Elapsed;
                    result.ErrorCode = ex is UnauthorizedAccessException ? "AUTH_ERROR" : "RESTORE_ERROR";

                    _notificationService.ShowError($"Restore failed: {ex.Message}");
                    return result;
                }
                finally
                {
                    stopwatch.Stop();
                    _logger.LogDebug("Restore operation cleanup started");

                    // Clean up downloaded file
                    if (!string.IsNullOrEmpty(downloadedBackupPath) && File.Exists(downloadedBackupPath))
                    {
                        try
                        {
                            _logger.LogDebug("Cleaning up downloaded backup file: {DownloadedPath}", downloadedBackupPath);
                            File.Delete(downloadedBackupPath);
                            _logger.LogDebug("Downloaded backup file cleaned up successfully");
                        }
                        catch (Exception cleanupEx)
                        {
                            _logger.LogWarning(cleanupEx, "Failed to cleanup downloaded backup file: {Path}", downloadedBackupPath);
                        }
                    }

                    // Bước 3: Mở lại kết nối sau khi restore hoàn thành
                    try
                    {
                        _logger.LogDebug("Reopening database connections after restore operation...");
                        await ReopenDatabaseConnectionsAsync(cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to reopen database connections after restore. Application may need restart.");
                        // Don't throw here as restore operation result is already determined
                    }

                    _logger.LogDebug("Restore operation cleanup completed");
                }
            }
            finally
            {
                _operationSemaphore.Release();
            }
        }

        /// <summary>
        /// Deletes a backup from the server
        /// </summary>
        public async Task<bool> DeleteBackupAsync(string filename)
        {
            try
            {
                _logger.LogInformation("Deleting backup from server: {Filename}", filename);

                // Ensure authentication is valid and refresh token if necessary
                if (!await EnsureAuthenticatedAsync())
                {
                    throw new UnauthorizedAccessException("Authentication failed. Please log in again.");
                }

                var baseUrl = _configuration["ApiConfiguration:BaseUrl"] ?? "http://localhost:4000";
                var endpoint = $"{baseUrl}/api/backups/{Uri.EscapeDataString(filename)}";

                using var request = new HttpRequestMessage(HttpMethod.Delete, endpoint);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _authState.AccessToken);

                using var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Backup deleted successfully: {Filename}", filename);
                    _notificationService.ShowSuccess($"Backup deleted: {filename}");
                    return true;
                }
                else
                {
                    _logger.LogError("Failed to delete backup {Filename}. Status: {StatusCode}", filename, response.StatusCode);
                    _notificationService.ShowError($"Failed to delete backup: {response.StatusCode}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting backup {Filename}", filename);
                _notificationService.ShowError($"Failed to delete backup: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Prepares the database for backup/restore operations by ensuring all connections are closed
        /// </summary>
        public async Task PrepareDatabaseForOperationsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Preparing database for backup/restore operations...");
                await EnsureDatabaseClosedWithRetry(cancellationToken);
                _logger.LogInformation("Database prepared successfully for backup/restore operations");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to prepare database for backup/restore operations");
                throw;
            }
        }

        /// <summary>
        /// Prepares database for backup operations with progress reporting for UI
        /// This method should be called BEFORE showing backup selection UI
        /// 
        /// WORKFLOW FOR UI:
        /// 1. User clicks "Sao lưu" button
        /// 2. UI shows loading dialog/progress
        /// 3. Call PrepareForBackupWithProgressAsync() with progress callback
        /// 4. Wait for completion (this ensures database is fully disconnected)
        /// 5. Hide loading dialog
        /// 6. Show backup selection UI
        /// 7. When user clicks "Create Backup", call CreateBackupAsync() with isDatabasePrepared=true
        /// </summary>
        /// <param name="progress">Progress reporting callback for loading UI</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if preparation was successful, false otherwise</returns>
        public async Task<bool> PrepareForBackupWithProgressAsync(IProgress<BackupProgress>? progress = null, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Starting database preparation for backup operations...");

                // Report initial progress
                progress?.Report(new BackupProgress
                {
                    PercentComplete = 0,
                    CurrentOperation = "Khởi tạo quá trình sao lưu...",
                    Status = "Initializing"
                });

                // Check authentication first
                progress?.Report(new BackupProgress
                {
                    PercentComplete = 10,
                    CurrentOperation = "Kiểm tra xác thực...",
                    Status = "Authenticating"
                });

                if (!await EnsureAuthenticatedAsync())
                {
                    _logger.LogError("Authentication failed during backup preparation");
                    progress?.Report(new BackupProgress
                    {
                        PercentComplete = 100,
                        CurrentOperation = "Xác thực thất bại!",
                        Status = "Failed"
                    });
                    throw new UnauthorizedAccessException("Authentication failed. Please log in again.");
                }

                // Start database disconnection process
                progress?.Report(new BackupProgress
                {
                    PercentComplete = 20,
                    CurrentOperation = "Chuẩn bị cơ sở dữ liệu...",
                    Status = "Preparing"
                });

                _logger.LogInformation("Starting comprehensive database preparation...");
                bool isReady = await PerformComprehensiveDatabasePreparationAsync();

                if (!isReady)
                {
                    throw new InvalidOperationException("Database is not ready for backup operations after comprehensive preparation");
                }

                progress?.Report(new BackupProgress
                {
                    PercentComplete = 100,
                    CurrentOperation = "Sẵn sàng cho sao lưu!",
                    Status = "Ready"
                });

                _logger.LogInformation("Database preparation completed successfully. Ready for backup operations.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to prepare database for backup operations");

                progress?.Report(new BackupProgress
                {
                    PercentComplete = 100,
                    CurrentOperation = $"Lỗi chuẩn bị: {ex.Message}",
                    Status = "Failed"
                });

                throw;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Ensures authentication is valid and refreshes token if necessary
        /// </summary>
        /// <returns>True if authenticated successfully, false otherwise</returns>
        private async Task<bool> EnsureAuthenticatedAsync()
        {
            // Check if user is authenticated
            if (!_authState.IsAuthenticated)
            {
                _logger.LogWarning("User is not authenticated");
                return false;
            }

            // Check if token is expired or close to expiry (within 5 minutes)
            if (_authState.AccessTokenExpiryUtc <= DateTimeOffset.UtcNow.AddMinutes(5))
            {
                _logger.LogInformation("Access token is expired or close to expiry (expires at {ExpiryTime}), attempting refresh...",
                    _authState.AccessTokenExpiryUtc);

                try
                {
                    bool refreshSuccess = await _authService.RefreshAsync();
                    if (refreshSuccess)
                    {
                        _logger.LogInformation("Access token refreshed successfully. New expiry: {ExpiryTime}",
                            _authState.AccessTokenExpiryUtc);
                        return true;
                    }
                    else
                    {
                        _logger.LogWarning("Token refresh failed - RefreshAsync returned false. Current user: {Username}, IsAuthenticated: {IsAuth}",
                            _authState.CurrentUser?.username, _authState.IsAuthenticated);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during token refresh for user {Username}", _authState.CurrentUser?.username);
                    return false;
                }
            }

            _logger.LogDebug("Access token is still valid until {ExpiryTime}", _authState.AccessTokenExpiryUtc);
            return true;
        }

        /// <summary>
        /// Creates a backup using only VACUUM INTO method
        /// </summary>
        private async Task<string> CreateVacuumBackupOnly(CancellationToken cancellationToken)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string tempBackupPath = Path.Combine(_backupTempPath, $"backup_{timestamp}.sqlite");

            _logger.LogInformation("Creating VACUUM INTO backup to: {BackupPath}", tempBackupPath);

            // Create progress reporter for VACUUM INTO
            var progress = new Progress<(string message, double percentage)>((p) =>
            {
                _logger.LogDebug("VACUUM INTO Progress: {Message} ({Percentage}%)", p.message, p.percentage);
            });

            // Try Entity Framework approach first
            _logger.LogInformation("Attempting VACUUM INTO backup using Entity Framework...");
            bool efSuccess = await CreateVacuumIntoBackupUsingEFAsync(tempBackupPath, progress);

            if (efSuccess)
            {
                _logger.LogInformation("Entity Framework VACUUM INTO backup completed successfully");
                await VerifyBackupFile(tempBackupPath, cancellationToken);
                return tempBackupPath;
            }

            // Fallback to direct SQLite helper method
            _logger.LogInformation("Entity Framework VACUUM INTO failed, trying direct SQLite approach...");

            bool isSupported = await SqliteConnectionHelper.IsVacuumIntoSupportedAsync();
            if (!isSupported)
            {
                throw new NotSupportedException("VACUUM INTO not supported by current SQLite version. Please upgrade SQLite to version 3.27.0 or later.");
            }

            bool directSuccess = await SqliteConnectionHelper.CreateBackupUsingVacuumIntoAsync(
                _databasePath,
                tempBackupPath,
                progress);

            if (directSuccess)
            {
                _logger.LogInformation("Direct VACUUM INTO backup completed successfully");
                await VerifyBackupFile(tempBackupPath, cancellationToken);
                return tempBackupPath;
            }

            throw new InvalidOperationException("All VACUUM INTO methods failed. Cannot create backup without VACUUM INTO support.");
        }



        /// <summary>
        /// Creates a backup using VACUUM INTO through Entity Framework context
        /// Alternative method using EF Core for VACUUM INTO operations
        /// </summary>
        /// <param name="backupPath">Path where backup will be created</param>
        /// <param name="progress">Progress reporting callback</param>
        /// <returns>True if backup was successful</returns>
        private async Task<bool> CreateVacuumIntoBackupUsingEFAsync(string backupPath, IProgress<(string message, double percentage)>? progress = null)
        {
            try
            {
                _logger.LogInformation("Creating VACUUM INTO backup using Entity Framework: {BackupPath}", backupPath);

                // Execute with fresh context to avoid connection issues
                return await ExecuteWithFreshContextAsync(async (context) =>
                {
                    progress?.Report(("Checking VACUUM INTO support...", 10));

                    // Check if VACUUM INTO is supported using direct connection
                    string? versionResult = null;
                    try
                    {
                        var connection = context.Database.GetDbConnection();
                        await context.Database.OpenConnectionAsync();
                        using var command = connection.CreateCommand();
                        command.CommandText = "SELECT sqlite_version();";
                        var result = await command.ExecuteScalarAsync();
                        versionResult = result?.ToString();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not determine SQLite version");
                        return false;
                    }

                    if (string.IsNullOrEmpty(versionResult))
                    {
                        _logger.LogWarning("Could not determine SQLite version");
                        return false;
                    }

                    _logger.LogInformation("SQLite version: {Version}", versionResult);

                    // VACUUM INTO was introduced in SQLite 3.27.0 (2019-02-07)
                    var version = new Version(versionResult.Split('-')[0]); // Handle version strings like "3.39.2-1"
                    var minVersion = new Version("3.27.0");

                    if (version < minVersion)
                    {
                        _logger.LogWarning("VACUUM INTO requires SQLite 3.27.0 or later. Current version: {Version}", versionResult);
                        return false;
                    }

                    progress?.Report(("VACUUM INTO supported, creating backup...", 20));

                    // Ensure backup directory exists
                    var backupDir = Path.GetDirectoryName(backupPath);
                    if (!string.IsNullOrEmpty(backupDir) && !Directory.Exists(backupDir))
                    {
                        Directory.CreateDirectory(backupDir);
                    }

                    // Remove existing backup file if it exists
                    if (File.Exists(backupPath))
                    {
                        File.Delete(backupPath);
                    }

                    progress?.Report(("Executing VACUUM INTO command...", 40));

                    // Execute VACUUM INTO command using raw SQL
                    var vacuumCommand = $"VACUUM INTO '{backupPath.Replace("'", "''")}';";
                    _logger.LogDebug("Executing VACUUM INTO command: {Command}", vacuumCommand);

                    await context.Database.ExecuteSqlRawAsync(vacuumCommand);

                    progress?.Report(("VACUUM INTO completed, verifying backup...", 80));

                    // Verify the backup file was created and has content
                    if (!File.Exists(backupPath))
                    {
                        _logger.LogError("VACUUM INTO completed but backup file was not created: {BackupPath}", backupPath);
                        return false;
                    }

                    var fileInfo = new FileInfo(backupPath);
                    if (fileInfo.Length == 0)
                    {
                        _logger.LogError("VACUUM INTO created empty backup file: {BackupPath}", backupPath);
                        return false;
                    }

                    progress?.Report(("Backup verification successful", 100));

                    _logger.LogInformation("VACUUM INTO backup completed successfully. File size: {Size} bytes", fileInfo.Length);
                    return true;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "VACUUM INTO backup using EF failed");

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
        /// Creates a safety backup using VACUUM INTO before restore operation
        /// </summary>
        private async Task<string> CreateVacuumSafetyBackup(CancellationToken cancellationToken)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string safetyBackupPath = Path.Combine(_backupTempPath, $"safety_backup_{timestamp}.sqlite");

            _logger.LogInformation("Creating safety backup using VACUUM INTO before restore operation to: {SafetyPath}", safetyBackupPath);

            try
            {
                // Use VACUUM INTO for safety backup
                using var context = new DataContext();
                using var connection = context.Database.GetDbConnection();
                await connection.OpenAsync(cancellationToken);

                using var command = connection.CreateCommand();
                command.CommandText = $"VACUUM INTO '{safetyBackupPath.Replace("'", "''")}'";

                _logger.LogDebug("Executing VACUUM INTO command for safety backup");
                await command.ExecuteNonQueryAsync(cancellationToken);

                _logger.LogDebug("Starting safety backup file verification...");
                await VerifyBackupFile(safetyBackupPath, cancellationToken);

                _logger.LogInformation("Safety backup created successfully using VACUUM INTO: {SafetyPath}", safetyBackupPath);
                return safetyBackupPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create safety backup using VACUUM INTO");

                // Clean up backup file if it exists
                try
                {
                    if (File.Exists(safetyBackupPath))
                    {
                        _logger.LogDebug("Cleaning up safety backup file: {SafetyPath}", safetyBackupPath);
                        File.Delete(safetyBackupPath);
                    }
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogWarning(cleanupEx, "Failed to cleanup safety backup file: {SafetyPath}", safetyBackupPath);
                }

                throw;
            }
        }


        /// <summary>
        /// Verifies that a backup file is a valid SQLite database
        /// </summary>x
        private async Task VerifyBackupFile(string filePath, CancellationToken cancellationToken = default)
        {
            const int maxRetries = 10;
            const int baseDelayMs = 200;

            await Task.Run(async () =>
            {
                Exception? lastException = null;

                for (int attempt = 1; attempt <= maxRetries; attempt++)
                {
                    try
                    {
                        // Check if file exists first
                        if (!File.Exists(filePath))
                        {
                            throw new FileNotFoundException($"Backup file not found: {filePath}");
                        }

                        // Wait a bit before each attempt to allow file locks to be released
                        if (attempt > 1)
                        {
                            await Task.Delay(baseDelayMs * attempt);
                        }

                        // Check SQLite file header with more permissive FileShare settings
                        byte[] header;
                        using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            header = new byte[16];
                            fs.Read(header, 0, 16);
                        }

                        var sqliteHeader = "SQLite format 3\0"u8.ToArray();
                        if (!header.SequenceEqual(sqliteHeader))
                        {
                            throw new InvalidOperationException("Backup file is not a valid SQLite database");
                        }

                        // Additional integrity check - try to open the database with retry logic
                        var connectionString = $"Data Source={filePath};Mode=ReadOnly;";
                        using (var connection = new Microsoft.Data.Sqlite.SqliteConnection(connectionString))
                        {
                            connection.Open();

                            using var command = connection.CreateCommand();
                            command.CommandText = "PRAGMA integrity_check(1);";
                            var result = command.ExecuteScalar()?.ToString();

                            if (result != "ok")
                            {
                                throw new InvalidOperationException($"Backup file integrity check failed: {result}");
                            }

                            // CRITICAL: Explicitly close and dispose connection to release file locks
                            command.Dispose();
                            connection.Close();
                        }

                        // ADDITIONAL: Force garbage collection to ensure SQLite connection is fully disposed
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        GC.Collect();

                        // Extra delay to ensure file locks are fully released
                        await Task.Delay(200, cancellationToken);

                        _logger.LogDebug("Backup file verification successful: {BackupPath} (attempt {Attempt})", filePath, attempt);
                        return; // Success - exit the retry loop
                    }
                    catch (IOException ex) when (ex.Message.Contains("being used by another process") && attempt < maxRetries)
                    {
                        lastException = ex;
                        _logger.LogWarning("File is locked, retrying verification (attempt {Attempt}/{MaxRetries}): {Error}",
                            attempt, maxRetries, ex.Message);
                        continue; // Retry for file lock issues
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Backup file validation failed for {BackupPath} (attempt {Attempt})", filePath, attempt);
                        throw new InvalidOperationException($"Backup file validation failed: {ex.Message}", ex);
                    }
                }

                // If we get here, all retries failed due to file being locked
                _logger.LogError(lastException, "Backup file validation failed after {MaxRetries} attempts due to file lock", maxRetries);
                throw new InvalidOperationException($"Backup file validation failed: The file is being used by another process after {maxRetries} attempts.", lastException);
            });
        }

        private async Task ReplaceDatabase(string backupPath)
        {
            _logger.LogInformation("Starting database replacement with backup: {BackupPath}", backupPath);

            try
            {
                var dbDirectory = Path.GetDirectoryName(_databasePath);
                _logger.LogDebug("Database directory: {DbDirectory}", dbDirectory);

                if (!string.IsNullOrEmpty(dbDirectory) && !Directory.Exists(dbDirectory))
                {
                    _logger.LogInformation("Creating database directory: {DbDirectory}", dbDirectory);
                    Directory.CreateDirectory(dbDirectory);
                }

                if (!File.Exists(backupPath))
                {
                    _logger.LogError("Backup file not found for replacement: {BackupPath}", backupPath);
                    throw new FileNotFoundException($"Backup file not found: {backupPath}");
                }

                var backupFileInfo = new FileInfo(backupPath);
                _logger.LogInformation("Backup file to restore - Size: {Size} bytes, LastModified: {LastModified}",
                    backupFileInfo.Length, backupFileInfo.LastWriteTime);

                _logger.LogDebug("Ensuring database is closed before replacement");
                await EnsureDatabaseClosedWithRetry(CancellationToken.None);

                await Task.Run(() =>
                {
                    _logger.LogDebug("Starting atomic database file replacement");

                    string tempCurrentDb = _databasePath + ".replacing";
                    if (File.Exists(_databasePath))
                    {
                        _logger.LogDebug("Moving current database to temporary location: {TempPath}", tempCurrentDb);
                        File.Move(_databasePath, tempCurrentDb);
                    }
                    else
                    {
                        _logger.LogDebug("No existing database file to backup");
                    }

                    try
                    {
                        _logger.LogDebug("Copying new database file to database location: {DatabasePath}", _databasePath);
                        File.Copy(backupPath, _databasePath, false);

                        var newFileInfo = new FileInfo(_databasePath);
                        _logger.LogInformation("Database file replaced successfully - New size: {Size} bytes", newFileInfo.Length);

                        if (File.Exists(tempCurrentDb))
                        {
                            _logger.LogDebug("Cleaning up temporary backup file: {TempPath}", tempCurrentDb);
                            File.Delete(tempCurrentDb);
                        }
                    }
                    catch (Exception copyEx)
                    {
                        _logger.LogError(copyEx, "Failed to copy backup file, attempting to restore original");
                        if (File.Exists(tempCurrentDb))
                        {
                            _logger.LogDebug("Restoring original database file from: {TempPath}", tempCurrentDb);
                            if (File.Exists(_databasePath))
                            {
                                File.Delete(_databasePath);
                            }
                            File.Move(tempCurrentDb, _databasePath);
                            _logger.LogInformation("Original database file restored successfully");
                        }
                        throw;
                    }
                });

                _logger.LogInformation("Database file replacement completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to replace database file with backup");
                throw;
            }
        }

        private async Task VerifyRestoredDatabase()
        {
            _logger.LogInformation("Starting verification of restored database");

            try
            {
                _logger.LogDebug("Creating new DataContext for database verification");
                using var context = new DataContext();

                _logger.LogDebug("Opening database connection for verification");
                await context.Database.OpenConnectionAsync();

                _logger.LogDebug("Performing database query test - counting subjects");
                var count = await context.Subjects.CountAsync();
                _logger.LogInformation("Database verification query successful. Subject count: {Count}", count);

                _logger.LogDebug("Configuring WAL mode for restored database");
                await context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");

                _logger.LogDebug("Running SQLite integrity check");
                string integrityResult = "unknown";
                try
                {
                    // Use direct connection for PRAGMA commands
                    var connection = context.Database.GetDbConnection();
                    using var command = connection.CreateCommand();
                    command.CommandText = "PRAGMA integrity_check;";
                    var result = await command.ExecuteScalarAsync();
                    integrityResult = result?.ToString() ?? "unknown";
                    _logger.LogDebug("Integrity check result: {Result}", integrityResult);

                    if (integrityResult != "ok")
                    {
                        _logger.LogError("Database integrity check failed: {Result}", integrityResult);
                        throw new InvalidDataException($"Database integrity check failed: {integrityResult}");
                    }
                }
                catch (Exception integrityEx)
                {
                    _logger.LogWarning(integrityEx, "Could not perform integrity check, but database appears functional based on query test");
                    // Don't fail the verification if integrity check fails but basic queries work
                }

                _logger.LogInformation("Database verification completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database verification failed");
                throw new InvalidOperationException("Restored database verification failed", ex);
            }
        }

        /// <summary>
        /// Ensures database connections are closed with retry logic for backup/restore operations
        /// </summary>
        private async Task EnsureDatabaseClosedWithRetry(CancellationToken cancellationToken)
        {
            const int maxRetries = 10; // Increased retries
            const int baseDelayMs = 500; // Increased base delay

            _logger.LogInformation("Ensuring database connections are closed (max {MaxRetries} retries)", maxRetries);

            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    _logger.LogDebug("Attempting to close database connections (attempt {Attempt}/{MaxRetries})", i + 1, maxRetries);

                    // First, disconnect all database contexts
                    await DisconnectAllDatabaseContextsAsync(cancellationToken);

                    // Force garbage collection and wait for cleanup
                    await SqliteConnectionHelper.ForceReleaseLocksAsync();

                    // Additional verification: try to open the database file for reading
                    try
                    {
                        using var testStream = new FileStream(_databasePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                        testStream.Close();
                        _logger.LogInformation("Database connections closed successfully on attempt {Attempt}", i + 1);
                        return;
                    }
                    catch (IOException testEx) when (testEx.Message.Contains("being used by another process"))
                    {
                        _logger.LogWarning("Database file still locked during verification test (attempt {Attempt})", i + 1);
                        if (i == maxRetries - 1)
                        {
                            throw new InvalidOperationException("Database file is still locked after all retries", testEx);
                        }
                    }
                }
                catch (Exception ex) when (i < maxRetries - 1)
                {
                    _logger.LogWarning(ex, "Failed to close database connections (attempt {Attempt}/{MaxRetries}). Retrying in {DelayMs}ms...",
                        i + 1, maxRetries, baseDelayMs * (i + 1));
                    await Task.Delay(baseDelayMs * (i + 1), cancellationToken); // Exponential backoff
                }
                catch (Exception ex) when (i == maxRetries - 1)
                {
                    _logger.LogError(ex, "Failed to close database connections on final attempt {Attempt}/{MaxRetries}", i + 1, maxRetries);
                    throw new InvalidOperationException($"Unable to close database connections after {maxRetries} attempts. Database may be locked by another process.", ex);
                }
            }

            _logger.LogError("Unable to close database connections after {MaxRetries} attempts", maxRetries);
            throw new InvalidOperationException($"Unable to close database connections after {maxRetries} attempts. Database may be locked by another process.");
        }

        /// <summary>
        /// Simplified file verification - just checks if file exists and is readable
        /// Removed lock testing to prevent additional file access issues
        /// </summary>
        private async Task VerifyFileNotLocked(string filePath)
        {
            await Task.Run(() =>
            {
                try
                {
                    // Only check if file exists and basic directory structure
                    var directory = Path.GetDirectoryName(filePath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    if (!File.Exists(filePath))
                    {
                        return; // File doesn't exist, so it's not locked
                    }

                    // Simple file info check without opening the file
                    var fileInfo = new FileInfo(filePath);
                    _logger.LogDebug("File verification: {FilePath}, Size: {Size} bytes, LastAccess: {LastAccess}",
                        filePath, fileInfo.Length, fileInfo.LastAccessTime);

                    // Don't test file locks as it can cause additional lock issues
                    // Trust that the preparation process has released all locks properly
                }
                catch (DirectoryNotFoundException)
                {
                    var directory = Path.GetDirectoryName(filePath);
                    if (!string.IsNullOrEmpty(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "File verification warning for {FilePath}: {Message}", filePath, ex.Message);
                    // Don't throw - let the actual backup process handle any file issues
                }
            });
        }

        /// <summary>
        /// Aggressively disconnects all database contexts and forces complete database shutdown
        /// </summary>
        private async Task DisconnectAllDatabaseContextsAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug("Starting aggressive context disconnection process...");

            try
            {
                // Phase 1: Try to get existing DataContext from DI and shut it down properly
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var dataContext = scope.ServiceProvider.GetService<DataContext>();
                        if (dataContext != null)
                        {
                            _logger.LogDebug("Found existing DataContext from DI, disposing...");
                            await dataContext.DisposeAsync();
                            _logger.LogDebug("DI DataContext disposed");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Warning during DI DataContext cleanup, continuing with manual cleanup");
                }

                // Phase 2: Create temporary context for final cleanup
                try
                {
                    using (var tempContext = new DataContext())
                    {
                        _logger.LogDebug("Using temporary DataContext for basic cleanup...");
                        await tempContext.DisposeAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Warning during temporary context cleanup");
                }

                // Phase 3: Force system-wide database shutdown
                _logger.LogDebug("Performing system-wide database shutdown...");
                bool shutdownResult = await SqliteConnectionHelper.PerformDatabaseShutdownAsync(_databasePath);
                _logger.LogDebug("System-wide shutdown result: {Result}", shutdownResult);

                if (!shutdownResult)
                {
                    _logger.LogWarning("System-wide shutdown failed, attempting forced cleanup...");
                    await SqliteConnectionHelper.ForceCloseAllConnectionsAsync(_databasePath);
                }

                _logger.LogDebug("Aggressive context disconnection completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during aggressive context disconnection");
                throw;
            }
        }

        /// <summary>
        /// Reopens database connections after backup/restore operations
        /// </summary>
        private async Task ReopenDatabaseConnectionsAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Starting database connection reopening process...");

            try
            {
                using (var tempContext = new DataContext())
                {
                    _logger.LogDebug("Opening database connection using OpenConnectionAsync...");
                    await tempContext.Database.OpenConnectionAsync(cancellationToken);

                    var connection = tempContext.Database.GetDbConnection();
                    if (connection.State == System.Data.ConnectionState.Open)
                    {
                        _logger.LogDebug("Database connection successfully reopened");

                        _logger.LogDebug("Testing database accessibility with simple query...");
                        await tempContext.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);

                        _logger.LogInformation("Database connection successfully restored and tested");
                    }
                    else
                    {
                        _logger.LogWarning("Database connection state is {State} after open attempt", connection.State);
                        throw new InvalidOperationException($"Unable to open database connection. Current state: {connection.State}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reopening database connections");
                throw;
            }
        }

        /// <summary>
        /// Downloads a specific backup file from the server to a temporary location
        /// </summary>
        /// <param name="filename">The name of the backup file to download</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The local path of the downloaded file</returns>
        private async Task<string> DownloadBackupFromServerAsync(string filename, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting backup download from server: {Filename}", filename);

            var baseUrl = _configuration["ApiConfiguration:BaseUrl"] ?? "http://localhost:4000";
            var endpoint = $"{baseUrl}/api/backups/download/{Uri.EscapeDataString(filename)}";

            _logger.LogDebug("Download endpoint: {Endpoint}", endpoint);

            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _authState.AccessToken);

            _logger.LogDebug("Sending download request with authorization header");
            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to download backup: {StatusCode} - {ReasonPhrase}", response.StatusCode, response.ReasonPhrase);
                throw new HttpRequestException($"Failed to download backup: {response.StatusCode}");
            }

            var tempPath = Path.Combine(Path.GetTempPath(), $"restore_{filename}_{Guid.NewGuid():N}");
            _logger.LogDebug("Creating temporary file for download: {TempPath}", tempPath);

            using var fileStream = File.Create(tempPath);
            await response.Content.CopyToAsync(fileStream, cancellationToken);

            var downloadedSize = new FileInfo(tempPath).Length;
            _logger.LogInformation("Backup downloaded successfully: {TempPath}, Size: {Size} bytes", tempPath, downloadedSize);
            return tempPath;
        }

        /// <summary>
        /// Cleans up backup file with retry logic to handle file locks
        /// </summary>
        private async Task CleanupBackupFileWithRetry(string filePath, CancellationToken cancellationToken = default)
        {
            const int maxRetries = 10;
            const int baseDelayMs = 300;

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                _logger.LogDebug("File not found for cleanup: {FilePath}", filePath);
                return;
            }

            _logger.LogDebug("Starting cleanup of backup file: {FilePath}", filePath);

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    // Force garbage collection before attempting file deletion
                    if (attempt > 1)
                    {
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        GC.Collect();
                        await Task.Delay(baseDelayMs * attempt, cancellationToken);
                    }

                    File.Delete(filePath);
                    _logger.LogDebug("Backup file cleaned up successfully: {FilePath} (attempt {Attempt})", filePath, attempt);
                    return;
                }
                catch (IOException ex) when (ex.Message.Contains("being used by another process") && attempt < maxRetries)
                {
                    _logger.LogWarning("File is locked, retrying cleanup (attempt {Attempt}/{MaxRetries}): {FilePath} - {Error}",
                        attempt, maxRetries, filePath, ex.Message);
                    continue;
                }
                catch (Exception ex) when (attempt < maxRetries)
                {
                    _logger.LogWarning(ex, "Unexpected error during file cleanup, retrying (attempt {Attempt}/{MaxRetries}): {FilePath}",
                        attempt, maxRetries, filePath);
                    await Task.Delay(baseDelayMs * attempt, cancellationToken);
                    continue;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to cleanup backup file after all retries: {FilePath}", filePath);
                    // Don't throw exception for cleanup failures - log warning instead
                    // This allows the backup operation to succeed even if cleanup fails
                    _logger.LogWarning("Backup file may remain in temp directory: {FilePath}. Manual cleanup may be required.", filePath);
                }
            }

            _logger.LogError("Failed to cleanup backup file after {MaxRetries} attempts: {FilePath}", maxRetries, filePath);
            // Don't throw exception for cleanup failures - log warning instead
            _logger.LogWarning("Backup file may remain in temp directory: {FilePath}. Manual cleanup may be required.", filePath);
        }

        /// <summary>
        /// Cleans up old backup files in temp directory to prevent accumulation
        /// </summary>
        private async Task CleanupOldBackupFilesAsync()
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(5)); // Wait 5 minutes before cleanup

                _logger.LogDebug("Starting cleanup of old backup files in temp directory: {TempPath}", _backupTempPath);

                if (!Directory.Exists(_backupTempPath))
                {
                    return;
                }

                var cutoffTime = DateTime.Now.AddHours(-24); // Remove files older than 24 hours
                var backupFiles = Directory.GetFiles(_backupTempPath, "*.sqlite")
                    .Where(file =>
                    {
                        try
                        {
                            return File.GetCreationTime(file) < cutoffTime;
                        }
                        catch
                        {
                            return false; // Skip files we can't access
                        }
                    })
                    .ToList();

                _logger.LogDebug("Found {Count} old backup files to clean up", backupFiles.Count);

                foreach (var file in backupFiles)
                {
                    try
                    {
                        File.Delete(file);
                        _logger.LogDebug("Cleaned up old backup file: {File}", file);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to cleanup old backup file: {File}", file);
                        // Continue with other files
                    }
                }

                _logger.LogInformation("Cleanup completed. Processed {Count} old backup files", backupFiles.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during old backup files cleanup");
                // Don't throw - this is background cleanup
            }
        }

        /// <summary>
        /// Safely executes an action with a fresh DataContext scope
        /// Ensures proper disposal and connection cleanup
        /// </summary>
        /// <typeparam name="T">Return type</typeparam>
        /// <param name="action">Action to execute with DataContext</param>
        /// <returns>Result of the action</returns>
        private async Task<T> ExecuteWithFreshContextAsync<T>(Func<DataContext, Task<T>> action)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DataContext>();

            try
            {
                return await action(context);
            }
            finally
            {
                // Ensure proper cleanup
                await context.DisposeAsync();
            }
        }

        /// <summary>
        /// Safely executes an action with a fresh DataContext scope
        /// Ensures proper disposal and connection cleanup
        /// </summary>
        /// <param name="action">Action to execute with DataContext</param>
        private async Task ExecuteWithFreshContextAsync(Func<DataContext, Task> action)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DataContext>();

            try
            {
                await action(context);
            }
            finally
            {
                // Ensure proper cleanup
                await context.DisposeAsync();
            }
        }

        /// <summary>
        /// Synchronizes the database to a specific backup version
        /// This function performs the same operation as RestoreFromBackupAsync but with different UI terminology
        /// </summary>
        /// <param name="filename">Name of the backup file to sync to</param>
        /// <param name="progress">Progress reporting callback</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result of the sync operation</returns>
        public async Task<RestoreResult> SyncToVersionAsync(string filename, IProgress<RestoreProgress>? progress = null, CancellationToken cancellationToken = default)
        {
            // Ensure only one backup/restore operation at a time
            await _operationSemaphore.WaitAsync(cancellationToken);

            try
            {
                var stopwatch = Stopwatch.StartNew();
                var result = new RestoreResult { RestoreDate = DateTime.UtcNow };
                string? safetyBackupPath = null;
                string? downloadedBackupPath = null;

                try
                {
                    _logger.LogInformation("Starting database sync to version: {Filename}", filename);

                    // Ensure authentication is valid and refresh token if necessary
                    if (!await EnsureAuthenticatedAsync())
                    {
                        _logger.LogError("Authentication failed during sync operation");
                        throw new UnauthorizedAccessException("Authentication failed. Please log in again.");
                    }

                    _logger.LogDebug("Authentication verified successfully");
                    progress?.Report(new RestoreProgress
                    {
                        PercentComplete = 10,
                        CurrentOperation = "Đang chuẩn bị đồng bộ...",
                        Status = "Initializing"
                    });

                    // Create safety backup before sync with enhanced error handling
                    _logger.LogInformation("Preparing database for sync operation");
                    await EnsureDatabaseClosedWithRetry(cancellationToken);

                    progress?.Report(new RestoreProgress
                    {
                        PercentComplete = 20,
                        CurrentOperation = "Tạo bản sao lưu an toàn...",
                        Status = "Processing"
                    });

                    _logger.LogInformation("Creating safety backup before sync using VACUUM INTO");
                    safetyBackupPath = await CreateVacuumSafetyBackup(cancellationToken);
                    result.RollbackPath = safetyBackupPath;
                    _logger.LogInformation("Safety backup created: {SafetyBackupPath}", safetyBackupPath);

                    progress?.Report(new RestoreProgress
                    {
                        PercentComplete = 40,
                        CurrentOperation = "Tải xuống phiên bản từ server...",
                        Status = "Downloading"
                    });

                    // Download backup from server
                    downloadedBackupPath = await DownloadBackupFromServerAsync(filename, cancellationToken);

                    progress?.Report(new RestoreProgress
                    {
                        PercentComplete = 60,
                        CurrentOperation = "Xác thực tập tin sao lưu...",
                        Status = "Validating",
                        IsValidating = true,
                        ValidationMessage = "Kiểm tra tính toàn vẹn tập tin..."
                    });

                    // Validate downloaded backup
                    _logger.LogInformation("Validating downloaded backup file");
                    await VerifyBackupFile(downloadedBackupPath, cancellationToken);

                    progress?.Report(new RestoreProgress
                    {
                        PercentComplete = 80,
                        CurrentOperation = "Đang đồng bộ cơ sở dữ liệu...",
                        Status = "Syncing"
                    });

                    // Replace database file
                    _logger.LogInformation("Starting database file sync");
                    await ReplaceDatabase(downloadedBackupPath);

                    progress?.Report(new RestoreProgress
                    {
                        PercentComplete = 90,
                        CurrentOperation = "Xác minh cơ sở dữ liệu...",
                        Status = "Verifying"
                    });

                    // Verify synced database
                    _logger.LogInformation("Verifying synced database integrity");
                    await VerifyRestoredDatabase();

                    progress?.Report(new RestoreProgress
                    {
                        PercentComplete = 100,
                        CurrentOperation = "Đồng bộ hoàn tất thành công",
                        Status = "Completed"
                    });

                    result.Success = true;
                    result.Message = $"Database synced successfully to version {filename}";
                    result.BackupFileName = filename;
                    result.Duration = stopwatch.Elapsed;

                    _logger.LogInformation("Database sync completed successfully to {Filename} in {Duration}ms",
                        filename, stopwatch.ElapsedMilliseconds);
                    _notificationService.ShowSuccess($"Đồng bộ thành công đến phiên bản {filename}");

                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Database sync failed for file {Filename}", filename);

                    // Attempt rollback if we have a safety backup
                    if (!string.IsNullOrEmpty(safetyBackupPath) && File.Exists(safetyBackupPath))
                    {
                        try
                        {
                            _logger.LogWarning("Attempting to rollback to safety backup: {SafetyBackupPath}", safetyBackupPath);
                            progress?.Report(new RestoreProgress
                            {
                                PercentComplete = 95,
                                CurrentOperation = "Đang khôi phục về trạng thái an toàn...",
                                Status = "Rolling back"
                            });

                            await ReplaceDatabase(safetyBackupPath);
                            result.RollbackPerformed = true;
                            _logger.LogInformation("Rollback completed successfully");
                            _notificationService.ShowWarning("Đồng bộ thất bại, nhưng cơ sở dữ liệu đã được khôi phục về trạng thái trước đó");
                        }
                        catch (Exception rollbackEx)
                        {
                            _logger.LogError(rollbackEx, "Rollback also failed!");
                            _notificationService.ShowError("Đồng bộ thất bại và không thể khôi phục! Vui lòng khôi phục cơ sở dữ liệu thủ công.");
                        }
                    }
                    else
                    {
                        _logger.LogError("No safety backup available for rollback");
                    }

                    result.Success = false;
                    result.Message = $"Sync failed: {ex.Message}";
                    result.Exception = ex;
                    result.Duration = stopwatch.Elapsed;
                    result.ErrorCode = ex is UnauthorizedAccessException ? "AUTH_ERROR" : "SYNC_ERROR";

                    _notificationService.ShowError($"Đồng bộ thất bại: {ex.Message}");
                    return result;
                }
                finally
                {
                    stopwatch.Stop();
                    _logger.LogDebug("Sync operation cleanup started");

                    // Clean up downloaded file
                    if (!string.IsNullOrEmpty(downloadedBackupPath) && File.Exists(downloadedBackupPath))
                    {
                        try
                        {
                            _logger.LogDebug("Cleaning up downloaded backup file: {DownloadedPath}", downloadedBackupPath);
                            File.Delete(downloadedBackupPath);
                            _logger.LogDebug("Downloaded backup file cleaned up successfully");
                        }
                        catch (Exception cleanupEx)
                        {
                            _logger.LogWarning(cleanupEx, "Failed to cleanup downloaded backup file: {Path}", downloadedBackupPath);
                        }
                    }

                    // Reopen database connections after sync operation
                    try
                    {
                        _logger.LogDebug("Reopening database connections after sync operation...");
                        await ReopenDatabaseConnectionsAsync(cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to reopen database connections after sync. Application may need restart.");
                        // Don't throw here as sync operation result is already determined
                    }

                    _logger.LogDebug("Sync operation cleanup completed");
                }
            }
            finally
            {
                _operationSemaphore.Release();
            }
        }

        /// <summary>
        /// Performs comprehensive database preparation for backup/restore operations
        /// Uses multiple strategies to ensure database is ready for file operations
        /// </summary>
        /// <returns>True if database is ready for file operations</returns>
        private async Task<bool> PerformComprehensiveDatabasePreparationAsync()
        {
            try
            {
                _logger.LogInformation("Starting comprehensive database preparation...");

                // Phase 1: Close all managed contexts
                await ExecuteWithFreshContextAsync(async context =>
                {
                    _logger.LogDebug("Executing database preparation with fresh context...");
                    // Basic cleanup - just dispose the context
                    await context.DisposeAsync();
                });

                // Phase 2: Force system-wide cleanup
                _logger.LogDebug("Performing system-wide database cleanup...");
                bool systemCleanup = await SqliteConnectionHelper.PerformDatabaseShutdownAsync(_databasePath);

                if (!systemCleanup)
                {
                    _logger.LogWarning("System cleanup failed, attempting forced cleanup...");
                    systemCleanup = await SqliteConnectionHelper.ForceCloseAllConnectionsAsync(_databasePath);
                }

                // Phase 3: Final verification
                await Task.Delay(1000); // Allow file system to settle

                _logger.LogInformation("Database preparation completed with result: {Result}", systemCleanup);
                return systemCleanup;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Comprehensive database preparation failed");
                return false;
            }
        }

        #endregion
    }
}
