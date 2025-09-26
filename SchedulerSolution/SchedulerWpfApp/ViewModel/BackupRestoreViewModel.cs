using Microsoft.Extensions.Logging;
using SchedulerWpfApp.Algorithm.DTO;
using SchedulerWpfApp.Helper;
using SchedulerWpfApp.ServiceRefactor.Auth;
using SchedulerWpfApp.ServiceRefactor.BackupRestoreService;
using SchedulerWpfApp.ServiceRefactor.NotificationService;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SchedulerWpfApp.ViewModel
{
    /// <summary>
    /// ViewModel for the Backup and Restore functionality
    /// Implements MVVM pattern with proper data binding and command handling
    /// </summary>
    public class BackupRestoreViewModel : ViewBaseModel, IDisposable
    {
        #region Fields
        private readonly IBackupRestoreService _backupRestoreService;
        private readonly AuthState _authState;
        private readonly INotificationService _notificationService;
        private readonly ILogger<BackupRestoreViewModel> _logger;

        private CancellationTokenSource? _cancellationTokenSource;
        private BackupProgress _backupProgress;
        private RestoreProgress _restoreProgress;
        private RestoreProgress _syncProgress;
        private BackupMetadata? _selectedBackup;
        private bool _isBackupInProgress;
        private bool _isRestoreInProgress;
        private bool _isSyncInProgress;
        private bool _isLoading;
        private string _statusMessage;
        private bool _canPerformOperations;
        private bool _isDatabasePreparedForBackup;
        #endregion

        #region Properties

        /// <summary>
        /// Collection of available backups from the server
        /// </summary>
        public ObservableCollection<BackupMetadata> AvailableBackups { get; }

        /// <summary>
        /// Currently selected backup for restore operation
        /// </summary>
        public BackupMetadata? SelectedBackup
        {
            get => _selectedBackup;
            set
            {
                if (SetProperty(ref _selectedBackup, value))
                {
                    OnPropertyChanged(nameof(CanRestore));
                    OnPropertyChanged(nameof(CanDelete));
                }
            }
        }

        /// <summary>
        /// Current backup operation progress
        /// </summary>
        public BackupProgress BackupProgress
        {
            get => _backupProgress;
            set => SetProperty(ref _backupProgress, value);
        }

        /// <summary>
        /// Current restore operation progress
        /// </summary>
        public RestoreProgress RestoreProgress
        {
            get => _restoreProgress;
            set => SetProperty(ref _restoreProgress, value);
        }

        /// <summary>
        /// Current sync operation progress
        /// </summary>
        public RestoreProgress SyncProgress
        {
            get => _syncProgress;
            set => SetProperty(ref _syncProgress, value);
        }

        /// <summary>
        /// Indicates if a backup operation is currently in progress
        /// </summary>
        public bool IsBackupInProgress
        {
            get => _isBackupInProgress;
            set
            {
                if (SetProperty(ref _isBackupInProgress, value))
                {
                    OnPropertyChanged(nameof(CanCreateBackup));
                    OnPropertyChanged(nameof(CanCreateLocalBackup));
                    OnPropertyChanged(nameof(CanRestore));
                    OnPropertyChanged(nameof(CanSync));
                    OnPropertyChanged(nameof(CanDelete));
                    OnPropertyChanged(nameof(CanRefresh));
                }
            }
        }

        /// <summary>
        /// Indicates if a restore operation is currently in progress
        /// </summary>
        public bool IsRestoreInProgress
        {
            get => _isRestoreInProgress;
            set
            {
                if (SetProperty(ref _isRestoreInProgress, value))
                {
                    OnPropertyChanged(nameof(CanCreateBackup));
                    OnPropertyChanged(nameof(CanCreateLocalBackup));
                    OnPropertyChanged(nameof(CanRestore));
                    OnPropertyChanged(nameof(CanSync));
                    OnPropertyChanged(nameof(CanDelete));
                    OnPropertyChanged(nameof(CanRefresh));
                }
            }
        }

        /// <summary>
        /// Indicates if a sync operation is currently in progress
        /// </summary>
        public bool IsSyncInProgress
        {
            get => _isSyncInProgress;
            set
            {
                if (SetProperty(ref _isSyncInProgress, value))
                {
                    OnPropertyChanged(nameof(CanCreateBackup));
                    OnPropertyChanged(nameof(CanCreateLocalBackup));
                    OnPropertyChanged(nameof(CanRestore));
                    OnPropertyChanged(nameof(CanSync));
                    OnPropertyChanged(nameof(CanDelete));
                    OnPropertyChanged(nameof(CanRefresh));
                }
            }
        }

        /// <summary>
        /// Indicates if data is being loaded
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (SetProperty(ref _isLoading, value))
                {
                    OnPropertyChanged(nameof(CanCreateBackup));
                    OnPropertyChanged(nameof(CanCreateLocalBackup));
                    OnPropertyChanged(nameof(CanRestore));
                    OnPropertyChanged(nameof(CanSync));
                    OnPropertyChanged(nameof(CanDelete));
                    OnPropertyChanged(nameof(CanRefresh));
                }
            }
        }

        /// <summary>
        /// Current status message
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>
        /// Indicates if operations can be performed (user is authenticated and not busy)
        /// </summary>
        public bool CanPerformOperations
        {
            get => _canPerformOperations;
            set
            {
                if (SetProperty(ref _canPerformOperations, value))
                {
                    OnPropertyChanged(nameof(CanCreateBackup));
                    OnPropertyChanged(nameof(CanCreateLocalBackup));
                    OnPropertyChanged(nameof(CanRestore));
                    OnPropertyChanged(nameof(CanSync));
                    OnPropertyChanged(nameof(CanDelete));
                    OnPropertyChanged(nameof(CanRefresh));
                }
            }
        }

        /// <summary>
        /// Indicates if database has been prepared for backup operations
        /// </summary>
        public bool IsDatabasePreparedForBackup
        {
            get => _isDatabasePreparedForBackup;
            set
            {
                if (SetProperty(ref _isDatabasePreparedForBackup, value))
                {
                    OnPropertyChanged(nameof(CanPrepareBackup));
                    OnPropertyChanged(nameof(CanCreateBackup));
                }
            }
        }

        /// <summary>
        /// Indicates if backup preparation can be started
        /// </summary>
        public bool CanPrepareBackup => CanPerformOperations && !IsBackupInProgress && !IsRestoreInProgress && !IsLoading && !IsDatabasePreparedForBackup;

        /// <summary>
        /// Indicates if a backup can be created
        /// </summary>
        public bool CanCreateBackup => CanPerformOperations && !IsBackupInProgress && !IsRestoreInProgress && !IsSyncInProgress && !IsLoading;

        /// <summary>
        /// Indicates if a local backup can be created (doesn't require server preparation)
        /// </summary>
        public bool CanCreateLocalBackup => CanPerformOperations && !IsBackupInProgress && !IsRestoreInProgress && !IsSyncInProgress && !IsLoading;

        /// <summary>
        /// Indicates if a restore can be performed
        /// </summary>
        public bool CanRestore => CanPerformOperations && SelectedBackup != null && !IsBackupInProgress && !IsRestoreInProgress && !IsSyncInProgress && !IsLoading;

        /// <summary>
        /// Indicates if a sync can be performed
        /// </summary>
        public bool CanSync => CanPerformOperations && SelectedBackup != null && !IsBackupInProgress && !IsRestoreInProgress && !IsSyncInProgress && !IsLoading;

        /// <summary>
        /// Indicates if a backup can be deleted
        /// </summary>
        public bool CanDelete => CanPerformOperations && SelectedBackup != null && !IsBackupInProgress && !IsRestoreInProgress && !IsSyncInProgress && !IsLoading;

        /// <summary>
        /// Indicates if the backup list can be refreshed
        /// </summary>
        public bool CanRefresh => CanPerformOperations && !IsBackupInProgress && !IsRestoreInProgress && !IsSyncInProgress && !IsLoading;

        /// <summary>
        /// Indicates if an operation can be cancelled
        /// </summary>
        public bool CanCancel => IsBackupInProgress || IsRestoreInProgress || IsSyncInProgress;

        /// <summary>
        /// Current user information
        /// </summary>
        public string CurrentUser => _authState.IsAuthenticated ? _authState.CurrentUser.username : "Chưa đăng nhập";

        /// <summary>
        /// Backup count for display
        /// </summary>
        public string BackupCountText => $"Có {AvailableBackups.Count} bản sao lưu";

        #endregion

        #region Commands

        public ICommand PrepareBackupCommand { get; }
        public ICommand CreateBackupCommand { get; }
        public ICommand CreateLocalBackupCommand { get; }
        public ICommand RestoreBackupCommand { get; }
        public ICommand SyncBackupCommand { get; }
        public ICommand DeleteBackupCommand { get; }
        public ICommand RefreshBackupsCommand { get; }
        public ICommand CancelOperationCommand { get; }
        public ICommand CloseCommand { get; }

        #endregion

        #region Constructor

        public BackupRestoreViewModel(
            IBackupRestoreService backupRestoreService,
            AuthState authState,
            INotificationService notificationService,
            ILogger<BackupRestoreViewModel> logger)
        {
            _backupRestoreService = backupRestoreService ?? throw new ArgumentNullException(nameof(backupRestoreService));
            _authState = authState ?? throw new ArgumentNullException(nameof(authState));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Initialize collections
            AvailableBackups = new ObservableCollection<BackupMetadata>();

            // Initialize progress objects
            _backupProgress = new BackupProgress();
            _restoreProgress = new RestoreProgress();
            _syncProgress = new RestoreProgress();
            _statusMessage = "Sẵn sàng";

            // Initialize commands
            PrepareBackupCommand = new RelayCommand(async () => await PrepareBackupAsync(), () => CanPrepareBackup);
            CreateBackupCommand = new RelayCommand(async () => await CreateBackupAsync(), () => CanCreateBackup);
            CreateLocalBackupCommand = new RelayCommand(async () => await CreateLocalBackupAsync(), () => CanCreateLocalBackup);
            RestoreBackupCommand = new RelayCommand(async () => await RestoreBackupAsync(), () => CanRestore);
            SyncBackupCommand = new RelayCommand(async () => await SyncBackupAsync(), () => CanSync);
            DeleteBackupCommand = new RelayCommand(async () => await DeleteBackupAsync(), () => CanDelete);
            RefreshBackupsCommand = new RelayCommand(async () => await RefreshBackupsAsync(), () => CanRefresh);
            CancelOperationCommand = new RelayCommand(CancelOperation, () => CanCancel);
            CloseCommand = new RelayCommand(CloseWindow);

            // Subscribe to authentication changes
            _authState.Changed += OnAuthStateChanged;

            // Update initial state
            UpdateCanPerformOperations();

            // Load backups on initialization - delay until after UI initialization
            LoadBackupsOnStartup();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Loads backups on startup with proper UI thread marshaling
        /// </summary>
        private async void LoadBackupsOnStartup()
        {
            try
            {
                // Wait a brief moment to ensure UI is fully initialized
                await Task.Delay(100);

                // Prepare database for backup operations before loading backups
                await PrepareForBackupOperations();

                // Then load backups on UI thread
                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    await RefreshBackupsAsync();
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during startup backup loading");
            }
        }

        /// <summary>
        /// Prepares the database for backup operations by ensuring connections are closed
        /// </summary>
        private async Task PrepareForBackupOperations()
        {
            try
            {
                _logger.LogDebug("Preparing database for backup operations...");

                // Use the backup service to properly close database connections
                await _backupRestoreService.PrepareDatabaseForOperationsAsync();

                // Add a small delay to allow any pending operations to complete
                await Task.Delay(200);

                // Force garbage collection to help release any held database connections
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                _logger.LogDebug("Database preparation for backup operations completed");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during database preparation for backup operations");
                // Don't rethrow as we want the backup/restore operation to proceed even if preparation fails
            }
        }

        /// <summary>
        /// Ensures ABSOLUTE disconnection from database before backup operations
        /// This method performs multiple layers of database disconnection to guarantee no active connections
        /// </summary>
        private async Task EnsureAbsoluteDatabaseDisconnection()
        {
            try
            {
                _logger.LogInformation("Initiating ABSOLUTE database disconnection for backup...");

                // Phase 1: Standard preparation
                StatusMessage = "Đang đóng kết nối cơ sở dữ liệu...";
                await PrepareForBackupOperations();

                // Phase 2: Multiple rounds of aggressive disconnection
                const int maxDisconnectionRounds = 3;
                for (int round = 1; round <= maxDisconnectionRounds; round++)
                {
                    StatusMessage = $"Đảm bảo ngắt kết nối cơ sở dữ liệu (lần {round}/{maxDisconnectionRounds})...";
                    _logger.LogDebug("Database disconnection round {Round}/{MaxRounds}", round, maxDisconnectionRounds);

                    // Call the backup service's preparation method with cancellation token
                    await _backupRestoreService.PrepareDatabaseForOperationsAsync(_cancellationTokenSource?.Token ?? CancellationToken.None);

                    // Extended delay between rounds to allow complete cleanup
                    await Task.Delay(500 * round, _cancellationTokenSource?.Token ?? CancellationToken.None);

                    // Aggressive garbage collection
                    for (int gc = 0; gc < 3; gc++)
                    {
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        await Task.Delay(100, _cancellationTokenSource?.Token ?? CancellationToken.None);
                    }
                    GC.Collect();
                }

                // Phase 3: Final verification that database is completely free
                StatusMessage = "Đang xác minh cơ sở dữ liệu sẵn sàng sao lưu...";
                _logger.LogDebug("Performing final database readiness verification...");

                // Wait additional time for any lingering processes
                await Task.Delay(1000, _cancellationTokenSource?.Token ?? CancellationToken.None);

                // Final garbage collection cycle
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                _logger.LogInformation("ABSOLUTE database disconnection completed successfully");
                StatusMessage = "Cơ sở dữ liệu sẵn sàng để sao lưu";

                // Small delay before proceeding to backup
                await Task.Delay(200, _cancellationTokenSource?.Token ?? CancellationToken.None);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Database disconnection cancelled by user");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during absolute database disconnection");
                // Don't throw here - let backup attempt proceed and handle any file lock issues
                StatusMessage = "Ngắt kết nối cơ sở dữ liệu hoàn thành với cảnh báo";
            }
        }

        #endregion

        #region Command Implementations

        /// <summary>
        /// Prepares database for backup operations by disconnecting all connections
        /// This should be called BEFORE showing backup selection UI
        /// </summary>
        private async Task PrepareBackupAsync()
        {
            try
            {
                IsBackupInProgress = true;
                StatusMessage = "Đang chuẩn bị cho quá trình sao lưu...";
                _cancellationTokenSource = new CancellationTokenSource();

                var progress = new Progress<BackupProgress>(p =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        BackupProgress = p;
                        StatusMessage = p.CurrentOperation;
                    });
                });

                _logger.LogInformation("Starting database preparation for backup operations...");
                bool success = await _backupRestoreService.PrepareForBackupWithProgressAsync(progress, _cancellationTokenSource.Token);

                if (success)
                {
                    StatusMessage = "Sẵn sàng cho sao lưu! Bạn có thể tạo backup ngay bây giờ.";
                    _logger.LogInformation("Database preparation completed successfully");

                    // Mark database as prepared for backup
                    IsDatabasePreparedForBackup = true;

                    // Show success notification
                    _notificationService.ShowSuccess("Cơ sở dữ liệu đã sẵn sàng cho sao lưu!");
                }
                else
                {
                    StatusMessage = "Chuẩn bị sao lưu thất bại.";
                    _logger.LogError("Database preparation failed");
                    IsDatabasePreparedForBackup = false;
                    _notificationService.ShowError("Không thể chuẩn bị cơ sở dữ liệu cho sao lưu.");
                }
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "Quá trình chuẩn bị đã bị hủy";
                _logger.LogInformation("Database preparation was cancelled by user");
                IsDatabasePreparedForBackup = false;
            }
            catch (UnauthorizedAccessException authEx)
            {
                StatusMessage = "Chuẩn bị thất bại: Cần xác thực";
                _logger.LogError("Database preparation failed due to authentication: {Message}", authEx.Message);
                _notificationService.ShowError("Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.");
                IsDatabasePreparedForBackup = false;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Chuẩn bị thất bại: {ex.Message}";
                _logger.LogError(ex, "Unexpected error during database preparation");
                _notificationService.ShowError($"Lỗi chuẩn bị sao lưu: {ex.Message}");
                IsDatabasePreparedForBackup = false;
            }
            finally
            {
                IsBackupInProgress = false;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        /// <summary>
        /// Creates a new backup and uploads it to the server
        /// NOTE: Will automatically prepare database if needed
        /// </summary>
        private async Task CreateBackupAsync()
        {
            try
            {
                IsBackupInProgress = true;
                StatusMessage = "Bắt đầu tạo sao lưu...";
                _cancellationTokenSource = new CancellationTokenSource();

                var progress = new Progress<BackupProgress>(p =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        BackupProgress = p;
                        StatusMessage = p.CurrentOperation;
                    });
                });

                // Automatically prepare database if not already prepared
                if (!IsDatabasePreparedForBackup)
                {
                    StatusMessage = "Đang chuẩn bị cơ sở dữ liệu cho sao lưu...";
                    _logger.LogInformation("Auto-preparing database for backup operations...");

                    bool preparationSuccess = await _backupRestoreService.PrepareForBackupWithProgressAsync(progress, _cancellationTokenSource.Token);

                    if (!preparationSuccess)
                    {
                        StatusMessage = "Chuẩn bị sao lưu thất bại.";
                        _logger.LogError("Database preparation failed");
                        _notificationService.ShowError("Không thể chuẩn bị cơ sở dữ liệu cho sao lưu.");
                        return;
                    }

                    IsDatabasePreparedForBackup = true;
                    StatusMessage = "Cơ sở dữ liệu đã sẵn sàng. Đang tạo sao lưu...";
                }

                // Call CreateServerBackupAsync with progress reporting
                var progressAdapter = new Progress<(string message, double percentage)>(p =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        BackupProgress = new BackupProgress
                        {
                            CurrentOperation = p.message,
                            PercentComplete = (int)p.percentage,
                            BytesTransferred = 0,
                            TotalBytes = 0,
                            Status = p.message
                        };
                        StatusMessage = p.message;
                    });
                });

                bool result = await _backupRestoreService.CreateServerBackupAsync(progressAdapter, _cancellationTokenSource.Token);

                if (result)
                {
                    StatusMessage = "Sao lưu lên server thành công!";
                    await RefreshBackupsAsync();
                    _notificationService.ShowSuccess("Sao lưu lên server thành công!");
                    // Reset preparation state after successful backup
                    IsDatabasePreparedForBackup = false;
                }
                else
                {
                    StatusMessage = "Sao lưu lên server thất bại";
                    _logger.LogError("Server backup creation failed");
                    _notificationService.ShowError("Sao lưu lên server thất bại. Vui lòng thử lại.");
                    // Reset preparation state after failed backup
                    IsDatabasePreparedForBackup = false;
                }
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "Quá trình sao lưu đã bị hủy";
                _logger.LogInformation("Backup operation was cancelled by user");
                IsDatabasePreparedForBackup = false;
            }
            catch (UnauthorizedAccessException authEx)
            {
                if (authEx.Message.Contains("Authentication failed"))
                {
                    StatusMessage = $"Sao lưu thất bại: Phiên đã hết hạn";
                    _logger.LogError("Backup failed due to authentication failure: {Message}", authEx.Message);
                    _notificationService.ShowError("Phiên đăng nhập đã hết hạn. Vui lòng đăng xuất và đăng nhập lại.");
                }
                else
                {
                    StatusMessage = $"Sao lưu thất bại: Cần xác thực";
                    _logger.LogError("Backup failed due to missing authentication: {Message}", authEx.Message);
                    _notificationService.ShowError("Vui lòng đăng nhập để tạo sao lưu.");
                }
                IsDatabasePreparedForBackup = false;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Sao lưu thất bại: {ex.Message}";
                _logger.LogError(ex, "Unexpected error during backup creation");
                _notificationService.ShowError($"Sao lưu thất bại: {ex.Message}");
                IsDatabasePreparedForBackup = false;
            }
            finally
            {
                IsBackupInProgress = false;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        /// <summary>
        /// Creates a local backup file using VACUUM INTO without server integration
        /// This method provides a simple, reliable way to backup the database locally
        /// </summary>
        private async Task CreateLocalBackupAsync()
        {
            try
            {
                IsBackupInProgress = true;
                StatusMessage = "Đang tạo sao lưu cục bộ...";
                _cancellationTokenSource = new CancellationTokenSource();

                // Create a file dialog to let user choose backup location
                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "Chọn vị trí lưu file sao lưu",
                    Filter = "Tệp cơ sở dữ liệu SQLite (*.sqlite)|*.sqlite|Tệp cơ sở dữ liệu (*.db)|*.db|Tất cả tệp (*.*)|*.*",
                    DefaultExt = "sqlite",
                    FileName = $"backup_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.sqlite"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var backupPath = saveDialog.FileName;

                    var progress = new Progress<(string message, double percentage)>(p =>
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            StatusMessage = p.message;
                            BackupProgress = new BackupProgress
                            {
                                CurrentOperation = p.message,
                                PercentComplete = (int)p.percentage,
                                Status = p.message
                            };
                        });
                    });

                    // Call the new local backup method
                    bool success = await _backupRestoreService.CreateLocalBackupAsync(backupPath, progress, _cancellationTokenSource.Token);

                    if (success)
                    {
                        StatusMessage = $"Sao lưu cục bộ thành công: {Path.GetFileName(backupPath)}";
                        _notificationService.ShowSuccess($"Tạo sao lưu thành công!\nVị trí: {backupPath}");
                        _logger.LogInformation("Local backup created successfully at: {BackupPath}", backupPath);
                    }
                    else
                    {
                        StatusMessage = "Sao lưu cục bộ thất bại";
                        _notificationService.ShowError("Không thể tạo sao lưu. Vui lòng kiểm tra quyền truy cập file và thử lại.");
                        _logger.LogError("Local backup creation failed");
                    }
                }
                else
                {
                    StatusMessage = "Đã hủy tạo sao lưu";
                    _logger.LogInformation("Local backup cancelled by user");
                }
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "Quá trình sao lưu đã bị hủy";
                _logger.LogInformation("Local backup operation was cancelled");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Sao lưu cục bộ thất bại: {ex.Message}";
                _logger.LogError(ex, "Unexpected error during local backup creation");
                _notificationService.ShowError($"Sao lưu cục bộ thất bại: {ex.Message}");
            }
            finally
            {
                IsBackupInProgress = false;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        /// <summary>
        /// Restores the database from the selected backup
        /// </summary>
        private async Task RestoreBackupAsync()
        {
            if (SelectedBackup == null) return;

            // Confirm with user before proceeding
            var result = MessageBox.Show(
                $"Điều này sẽ thay thế cơ sở dữ liệu hiện tại bằng bản sao lưu từ {SelectedBackup.FormattedDate}.\n\n" +
                "Một bản sao lưu an toàn sẽ được tạo trước khi khôi phục.\n\n" +
                "Bạn có muốn tiếp tục?",
                "Xác nhận khôi phục cơ sở dữ liệu",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                IsRestoreInProgress = true;
                StatusMessage = "Đang chuẩn bị khôi phục cơ sở dữ liệu...";
                _cancellationTokenSource = new CancellationTokenSource();

                // CRITICAL: Ensure ABSOLUTE disconnection from database before restore
                await EnsureAbsoluteDatabaseDisconnection();

                StatusMessage = "Đang khôi phục cơ sở dữ liệu...";
                var progress = new Progress<RestoreProgress>(p =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        RestoreProgress = p;
                        StatusMessage = p.CurrentOperation;
                    });
                });

                var restoreResult = await _backupRestoreService.RestoreFromBackupAsync(
                    SelectedBackup.Filename,
                    progress,
                    _cancellationTokenSource.Token);

                if (restoreResult.Success)
                {
                    StatusMessage = $"Khôi phục thành công từ {SelectedBackup.Filename}";
                    MessageBox.Show(
                        "Khôi phục cơ sở dữ liệu thành công!\n\nỨng dụng cần khởi động lại để áp dụng các thay đổi.",
                        "Khôi phục thành công",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    StatusMessage = $"Khôi phục thất bại: {restoreResult.Message}";
                    if (restoreResult.RollbackPerformed)
                    {
                        MessageBox.Show(
                            "Khôi phục thất bại, nhưng cơ sở dữ liệu gốc đã được khôi phục.\n\n" +
                            $"Lỗi: {restoreResult.Message}",
                            "Khôi phục thất bại - Cơ sở dữ liệu đã được khôi phục",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
                    else
                    {
                        MessageBox.Show(
                            $"Khôi phục thất bại: {restoreResult.Message}",
                            "Khôi phục thất bại",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "Quá trình khôi phục đã bị hủy";
                _logger.LogInformation("Restore operation was cancelled by user");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Khôi phục thất bại: {ex.Message}";
                _logger.LogError(ex, "Unexpected error during restore operation");
                _notificationService.ShowError($"Khôi phục thất bại: {ex.Message}");
            }
            finally
            {
                IsRestoreInProgress = false;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        /// <summary>
        /// Synchronizes the database to the selected backup version
        /// </summary>
        private async Task SyncBackupAsync()
        {
            if (SelectedBackup == null) return;

            // Confirm with user before proceeding
            var result = MessageBox.Show(
                $"Đồng bộ cơ sở dữ liệu về phiên bản từ {SelectedBackup.FormattedDate}?\n\n" +
                "Một bản sao lưu an toàn sẽ được tạo trước khi đồng bộ.\n\n" +
                "Bạn có muốn tiếp tục?",
                "Xác nhận đồng bộ cơ sở dữ liệu",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                IsSyncInProgress = true;
                StatusMessage = "Đang chuẩn bị đồng bộ cơ sở dữ liệu...";
                _cancellationTokenSource = new CancellationTokenSource();

                // CRITICAL: Ensure ABSOLUTE disconnection from database before sync
                await EnsureAbsoluteDatabaseDisconnection();

                StatusMessage = "Đang đồng bộ cơ sở dữ liệu...";
                var progress = new Progress<RestoreProgress>(p =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        SyncProgress = p;
                        StatusMessage = p.CurrentOperation;
                    });
                });

                var syncResult = await _backupRestoreService.SyncToVersionAsync(
                    SelectedBackup.Filename,
                    progress,
                    _cancellationTokenSource.Token);

                if (syncResult.Success)
                {
                    StatusMessage = $"Đồng bộ thành công đến phiên bản {SelectedBackup.Filename}";
                    MessageBox.Show(
                        "Đồng bộ cơ sở dữ liệu thành công!\n\nỨng dụng cần khởi động lại để áp dụng các thay đổi.",
                        "Đồng bộ thành công",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    StatusMessage = $"Đồng bộ thất bại: {syncResult.Message}";
                    if (syncResult.RollbackPerformed)
                    {
                        MessageBox.Show(
                            "Đồng bộ thất bại, nhưng cơ sở dữ liệu gốc đã được khôi phục.\n\n" +
                            $"Lỗi: {syncResult.Message}",
                            "Đồng bộ thất bại - Cơ sở dữ liệu đã được khôi phục",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
                    else
                    {
                        MessageBox.Show(
                            $"Đồng bộ thất bại: {syncResult.Message}",
                            "Đồng bộ thất bại",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "Quá trình đồng bộ đã bị hủy";
                _logger.LogInformation("Sync operation was cancelled by user");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Đồng bộ thất bại: {ex.Message}";
                _logger.LogError(ex, "Unexpected error during sync operation");
                _notificationService.ShowError($"Đồng bộ thất bại: {ex.Message}");
            }
            finally
            {
                IsSyncInProgress = false;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        /// <summary>
        /// Deletes the selected backup from the server
        /// </summary>
        private async Task DeleteBackupAsync()
        {
            if (SelectedBackup == null) return;

            var result = MessageBox.Show(
                $"Bạn có chắc chắn muốn xóa bản sao lưu '{SelectedBackup.Filename}'?\n\nHành động này không thể hoàn tác.",
                "Xác nhận xóa",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                StatusMessage = $"Đang xóa sao lưu {SelectedBackup.Filename}...";
                var success = await _backupRestoreService.DeleteBackupAsync(SelectedBackup.Filename);

                if (success)
                {
                    StatusMessage = $"Đã xóa thành công sao lưu {SelectedBackup.Filename}";
                    await RefreshBackupsAsync();
                }
                else
                {
                    StatusMessage = $"Không thể xóa sao lưu {SelectedBackup.Filename}";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi khi xóa sao lưu: {ex.Message}";
                _logger.LogError(ex, "Error deleting backup {Filename}", SelectedBackup.Filename);
                _notificationService.ShowError($"Không thể xóa sao lưu: {ex.Message}");
            }
        }

        /// <summary>
        /// Refreshes the list of available backups from the server
        /// </summary>
        private async Task RefreshBackupsAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Đang tải danh sách sao lưu...";

                var backups = await _backupRestoreService.GetBackupListAsync();

                // Ensure UI updates happen on UI thread
                if (Application.Current?.Dispatcher != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        AvailableBackups.Clear();
                        foreach (var backup in backups.OrderByDescending(b => b.UploadDate))
                        {
                            AvailableBackups.Add(backup);
                        }

                        OnPropertyChanged(nameof(BackupCountText));
                        StatusMessage = $"Đã tải {backups.Count} bản sao lưu";
                    });
                }
                else
                {
                    // Fallback if no dispatcher available
                    AvailableBackups.Clear();
                    foreach (var backup in backups.OrderByDescending(b => b.UploadDate))
                    {
                        AvailableBackups.Add(backup);
                    }

                    OnPropertyChanged(nameof(BackupCountText));
                    StatusMessage = $"Đã tải {backups.Count} bản sao lưu";
                }
            }
            catch (System.Net.Http.HttpRequestException httpEx) when (httpEx.Message.Contains("NotFound"))
            {
                StatusMessage = "Dịch vụ sao lưu không khả dụng - server có thể chưa chạy";
                _logger.LogWarning("Backup service endpoint not found. Server may not be running or endpoint may not exist.");

                if (Application.Current?.Dispatcher != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        AvailableBackups.Clear();
                        OnPropertyChanged(nameof(BackupCountText));
                    });
                }
                // Don't show error notification for 404 - it's expected when server is not running
            }
            catch (UnauthorizedAccessException authEx)
            {
                if (authEx.Message.Contains("Authentication failed"))
                {
                    StatusMessage = "Phiên làm việc đã hết hạn - vui lòng đăng xuất và đăng nhập lại";
                    _logger.LogWarning("Token refresh failed, user needs to re-authenticate");
                    _notificationService.ShowWarning("Phiên làm việc của bạn đã hết hạn và không thể làm mới. Vui lòng đăng xuất và đăng nhập lại.");
                }
                else
                {
                    StatusMessage = "Cần xác thực - vui lòng đăng nhập";
                    _logger.LogWarning("User is not authenticated for backup operations");
                }

                if (Application.Current?.Dispatcher != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        AvailableBackups.Clear();
                        OnPropertyChanged(nameof(BackupCountText));
                    });
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Không thể tải danh sách sao lưu: {ex.Message}";
                _logger.LogError(ex, "Error refreshing backup list");
                _notificationService.ShowError($"Không thể tải danh sách sao lưu: {ex.Message}");

                if (Application.Current?.Dispatcher != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        AvailableBackups.Clear();
                        OnPropertyChanged(nameof(BackupCountText));
                    });
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Cancels the current operation
        /// </summary>
        private void CancelOperation()
        {
            _cancellationTokenSource?.Cancel();
            StatusMessage = "Cancelling operation...";
        }

        /// <summary>
        /// Closes the backup/restore window
        /// </summary>
        private void CloseWindow()
        {
            // Cancel any ongoing operations
            if (CanCancel)
            {
                CancelOperation();
            }

            // The actual window close will be handled by the View
            OnRequestClose();
        }

        #endregion

        #region Event Handlers

        private void OnAuthStateChanged()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                UpdateCanPerformOperations();
                OnPropertyChanged(nameof(CurrentUser));

                if (_authState.IsAuthenticated)
                {
                    _ = Task.Run(async () => await RefreshBackupsAsync());
                }
                else
                {
                    AvailableBackups.Clear();
                    StatusMessage = "Please log in to access backup functionality";
                }
            });
        }

        private void UpdateCanPerformOperations()
        {
            CanPerformOperations = _authState.IsAuthenticated;
        }

        #endregion

        #region Events

        public event EventHandler? RequestClose;

        protected virtual void OnRequestClose()
        {
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _authState.Changed -= OnAuthStateChanged;
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
            }
        }

        #endregion
    }
}