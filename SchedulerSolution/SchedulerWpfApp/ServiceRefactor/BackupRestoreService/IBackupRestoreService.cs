using SchedulerWpfApp.Algorithm.DTO;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SchedulerWpfApp.ServiceRefactor.BackupRestoreService
{
    /// <summary>
    /// Interface for backup and restore operations
    /// </summary>
    public interface IBackupRestoreService
    {
        /// <summary>
        /// Creates a backup of the current database and uploads it to the server
        /// </summary>
        /// <param name="progress">Progress reporting callback</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <param name="isDatabasePrepared">Indicates if database has already been prepared for backup</param>
        /// <returns>Result of the backup operation</returns>
        // Task<BackupResult> CreateBackupAsync(IProgress<BackupProgress>? progress = null, CancellationToken cancellationToken = default, bool isDatabasePrepared = false);

        /// <summary>
        /// Creates a backup of the current database and uploads it to the server
        /// </summary>
        /// <param name="progress">Optional progress reporting</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if backup and upload were successful</returns>
        Task<bool> CreateServerBackupAsync(IProgress<(string message, double percentage)>? progress = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prepares database for backup operations with progress reporting
        /// This method should be called BEFORE showing backup selection UI
        /// </summary>
        /// <param name="progress">Progress reporting callback</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if preparation was successful</returns>
        Task<bool> PrepareForBackupWithProgressAsync(IProgress<BackupProgress>? progress = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves the list of available backups from the server
        /// </summary>
        /// <returns>List of available backup metadata</returns>
        Task<List<BackupMetadata>> GetBackupListAsync();

        /// <summary>
        /// Restores the database from a server backup with rollback capability
        /// </summary>
        /// <param name="filename">Name of the backup file to restore</param>
        /// <param name="progress">Progress reporting callback</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result of the restore operation</returns>
        Task<RestoreResult> RestoreFromBackupAsync(string filename, IProgress<RestoreProgress>? progress = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a backup from the server
        /// </summary>
        /// <param name="filename">Name of the backup file to delete</param>
        /// <returns>True if deletion was successful</returns>
        Task<bool> DeleteBackupAsync(string filename);

        /// <summary>
        /// Prepares the database for backup/restore operations by ensuring all connections are closed
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the preparation operation</returns>
        Task PrepareDatabaseForOperationsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a local backup using VACUUM INTO with simplified error handling
        /// This method focuses on reliable local backup without server integration
        /// </summary>
        /// <param name="backupPath">Path where backup will be created</param>
        /// <param name="progress">Optional progress reporting</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if backup was successful</returns>
        Task<bool> CreateLocalBackupAsync(string backupPath, IProgress<(string message, double percentage)>? progress = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets SQLite version and VACUUM INTO support information
        /// </summary>
        /// <returns>Version and support status</returns>
        Task<(string version, bool supportsVacuumInto)> GetSqliteInfoAsync();

        /// <summary>
        /// Synchronizes the database to a specific backup version
        /// This function performs the same operation as RestoreFromBackupAsync but with different UI terminology
        /// </summary>
        /// <param name="filename">Name of the backup file to sync to</param>
        /// <param name="progress">Progress reporting callback</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result of the sync operation</returns>
        Task<RestoreResult> SyncToVersionAsync(string filename, IProgress<RestoreProgress>? progress = null, CancellationToken cancellationToken = default);
    }
}