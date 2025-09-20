using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SchedulerWpfApp.Algorithm.DTO;
using SchedulerWpfApp.Model;
using SchedulerWpfApp.Repository;
using SchedulerWpfApp.ServiceRefactor.NotificationService;
using SchedulerWpfApp.ServiceRefactor.Auth;
using Syncfusion.XlsIO;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;

namespace SchedulerWpfApp.ServiceRefactor.ScheduleServices
{
    public class ScheduleServices : IScheduleServices
    {
        #region Fields
        private IUnitOfWork _unitOfWork;
        private readonly ILogger<ScheduleServices> _logger;
        private readonly INotificationService _notificationService;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly AuthState _authState;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the ScheduleServices class
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="unitOfWork"></param>
        /// <param name="notificationService"></param>
        /// <param name="configuration"></param>
        /// <param name="httpClient"></param>
        /// <param name="authState"></param>
        public ScheduleServices(ILogger<ScheduleServices> logger, IUnitOfWork unitOfWork, INotificationService notificationService, IConfiguration configuration, HttpClient httpClient, AuthState authState)
        {
            _logger = logger; // Injecting the logger to log errors and information
            _unitOfWork = unitOfWork; // Injecting the unit of work to manage database operations
            _notificationService = notificationService; // Injecting the notification service to handle notifications
            _configuration = configuration; // Injecting the configuration to access app settings
            _httpClient = httpClient; // Injecting the HTTP client for API calls
            _authState = authState; // Injecting the authentication state for API authorization
        }
        #endregion

        #region Methods
        /// <summary>
        /// Add a new schedules to the database
        /// </summary>
        /// <param name="schedules"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public async Task<bool> AddScheduleAsync(List<Schedule> schedules, IProgress<int> progress)
        {
            await _unitOfWork.BeginTransactionAsync();
            int index = 0;
            if (schedules == null)
                throw new ArgumentNullException(nameof(schedules));

            try
            {
                foreach (var schedule in schedules)
                {
                    await _unitOfWork.Repository<Schedule>().AddAsync(schedule);

                    await Task.Delay(10); // Simulate some delay for UI responsiveness

                    index++;

                    var percentCompleted = (int)((double)index / schedules.Count * 100);
                    progress?.Report(percentCompleted);
                }

                await _unitOfWork.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger?.LogError(ex, "Failed to add schedules due to an unexpected error.");
                return false;
            }
        }

        public async Task<bool> AddSlot(Schedule schedule)
        {
            if (schedule == null)
                throw new ArgumentNullException(nameof(schedule));
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _unitOfWork.ScheduleRepository.AddSlotAsync(schedule); // Sử dụng phương thức AddRange từ IUnitOfWork
                await _unitOfWork.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger?.LogError(ex, "Failed to add slot due to an unexpected error.");
                return false;
            }
        }

        /// <summary>
        /// Update schedule in database
        /// </summary>
        /// <param name="schedule"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public async Task<bool> UpdateScheduleAsync(Schedule schedule)
        {
            if (schedule == null)
                throw new ArgumentNullException(nameof(schedule));
            try
            {
                var existingScheduler = await _unitOfWork.ScheduleRepository.GetByIdAsync(schedule.ScheduleId);

                if (existingScheduler != null)
                {
                    existingScheduler.ScheduleId = schedule.ScheduleId;
                    existingScheduler.RoomId = schedule.RoomId;
                    existingScheduler.RoomName = schedule.RoomName;
                    existingScheduler.PartOfDay = schedule.PartOfDay;
                    existingScheduler.SlotTime = schedule.SlotTime;
                    existingScheduler.StatusSlot = schedule.StatusSlot;
                    existingScheduler.Date = schedule.Date;
                    existingScheduler.Major = schedule.Major;
                    existingScheduler.SubjectCode = schedule.SubjectCode;
                    existingScheduler.GroupName = schedule.GroupName;
                    existingScheduler.LecturerId = schedule.LecturerId;
                    existingScheduler.LecturerName = schedule.LecturerName;
                    existingScheduler.SlotTypeCode = schedule.SlotTypeCode;
                    existingScheduler.TypeSlot = schedule.TypeSlot;
                    existingScheduler.SessionNo = schedule.SessionNo;

                    await _unitOfWork.BeginTransactionAsync();
                    await _unitOfWork.Repository<Schedule>().UpdateAsync(existingScheduler);
                    await _unitOfWork.CommitAsync();
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to update schedule due to an unexpected error.");
                await _unitOfWork.RollbackAsync();
                return false;
            }
        }

        /// <summary>
        /// Get all scheduler
        /// </summary>
        /// <returns>List schedule</returns>
        public async Task<List<Schedule>> GetAllAsync()
        {
            try
            {
                return await _unitOfWork.ScheduleRepository.GetAllAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to add schedules due to an unexpected error.");
                throw new Exception("Lỗi khi lấy danh sách lịch học", ex);
            }
        }

        public async Task DeleteAllAsync(IProgress<int> progress)
        {
            await _unitOfWork.BeginTransactionAsync();
            int index = 0;
            try
            {
                var schedules = await _unitOfWork.ScheduleRepository.GetAllAsync();
                // Call delete and reset identity operations
                foreach (var schedule in schedules)
                {
                    await _unitOfWork.ScheduleRepository.DeleteAsync(schedule.ScheduleId);

                    await Task.Delay(10); // Simulate some delay for UI responsiveness

                    index++;
                    // Simulate progress reporting
                    var percentCompleted = (int)((double)index / schedules.Count * 100);
                    progress?.Report(percentCompleted);
                }
                await _unitOfWork.ScheduleRepository.ResetIdentitySchedulesAsync();  // Ensure ResetIdentity is part of the transaction

                await _unitOfWork.CommitAsync();
            }
            catch (DbUpdateException dbEx)
            {
                _logger?.LogError(dbEx, "Database error while deleting all schedules.");
                await _unitOfWork.RollbackAsync();
                throw new Exception("Lỗi khi xóa tất cả lịch học do lỗi cơ sở dữ liệu.", dbEx);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Unexpected error while deleting all schedules.");
                await _unitOfWork.RollbackAsync();
                throw new Exception("Lỗi khi xóa tất cả lịch học.", ex);
            }
        }

        public async Task DeleteAllData()
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();
                // Call delete and reset identity operations
                await _unitOfWork.ScheduleRepository.DeleteAllDataAsync();
                await _unitOfWork.ScheduleRepository.ResetIdentityAllTableAsync();  // Ensure ResetIdentity is part of the transaction
                await _unitOfWork.CommitAsync();
            }
            catch (DbUpdateException dbEx)
            {
                _logger?.LogError(dbEx, "Database error while deleting all data.");
                await _unitOfWork.RollbackAsync();
                throw new Exception("Lỗi khi xóa tất cả dữ liệu do lỗi cơ sở dữ liệu.", dbEx);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Unexpected error while deleting all data.");
                await _unitOfWork.RollbackAsync();
                throw new Exception("Lỗi khi xóa tất cả dữ liệu.", ex);
            }
        }

        public async Task DeleteScheduleAsync(int scheduleId)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                // Call delete and reset identity operations
                var existingSchedule = await _unitOfWork.ScheduleRepository.GetByIdAsync(scheduleId);
                if (existingSchedule == null)
                {
                    throw new Exception($"Lịch học với ID {scheduleId} không tồn tại.");
                }

                await _unitOfWork.ScheduleRepository.DeleteScheduleAsync(existingSchedule);
                await _unitOfWork.ScheduleRepository.ResetIdentitySchedulesAsync();  // Ensure ResetIdentity is part of the transaction

                await _unitOfWork.CommitAsync();
            }
            catch (DbUpdateException dbEx)
            {
                _logger?.LogError(dbEx, "Database error while deleting schedules.");
                await _unitOfWork.RollbackAsync();
                throw new Exception("Lỗi khi xóa lịch học do lỗi cơ sở dữ liệu.", dbEx);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Unexpected error while deleting schedules.");
                await _unitOfWork.RollbackAsync();
                throw new Exception("Lỗi khi xóa lịch học.", ex);
            }
        }
        public void ExportToExcel(List<Schedule> schedules, string filePath)
        {
            using ExcelEngine excelEngine = new();
            IApplication application = excelEngine.Excel;
            application.DefaultVersion = ExcelVersion.Xlsx;

            IWorkbook workbook = application.Workbooks.Create(1);
            IWorksheet sheet = workbook.Worksheets[0];

            // Header row
            string[] headers = new string[]
            {
                "ScheduleId", "GroupName", "SubjectCode", "Date", "Slot",
                "RoomNo", "SessionNo", "Lecturer", "SlotTypeCode", "StatusSlot",
                "TypeSlot"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                sheet[1, i + 1].Text = headers[i];
            }

            // Data rows
            int row = 2;
            foreach (var s in schedules)
            {
                sheet[row, 1].Number = s.ScheduleId;
                sheet[row, 2].Text = s.GroupName ?? "";
                sheet[row, 3].Text = s.SubjectCode ?? "";
                sheet[row, 4].Text = s.Date?.ToString("dd-MM-yyyy") ?? "";
                sheet[row, 5].Number = s.SlotTime ?? 0;
                sheet[row, 6].Text = s.RoomName ?? "";
                sheet[row, 7].Number = s.SessionNo ?? 0;
                sheet[row, 8].Text = s.LecturerName ?? "";
                sheet[row, 9].Text = s.SlotTypeCode ?? "";
                sheet[row, 10].Text = s.StatusSlot ?? "";
                sheet[row, 11].Text = s.TypeSlot ?? "";

                row++;
            }

            workbook.SaveAs(filePath);
            _notificationService.ShowSuccess("Xuất dữ liệu thành công!");
        }

        /// <summary>
        /// Upload schedules in batches to handle large datasets efficiently
        /// </summary>
        /// <param name="schedules">List of schedules to upload</param>
        /// <param name="batchSize">Number of schedules per batch (default: 1000)</param>
        /// <param name="overwriteExisting">Whether to overwrite existing schedules</param>
        /// <returns>True if all batches uploaded successfully, false otherwise</returns>
        public async Task<bool> UploadSchedulesInBatchesAsync(List<Schedule> schedules, int batchSize = 1000, bool overwriteExisting = false)
        {
            if (schedules == null || !schedules.Any())
            {
                _logger?.LogWarning("No schedules provided for batch upload.");
                _notificationService.ShowWarning("Không có lịch học nào để tải lên.");
                return false;
            }

            _logger?.LogInformation($"Starting batch upload for {schedules.Count} schedules with batch size {batchSize}");

            var totalBatches = (int)Math.Ceiling((double)schedules.Count / batchSize);
            var successfulBatches = 0;
            var failedBatches = 0;

            for (int i = 0; i < totalBatches; i++)
            {
                var batch = schedules.Skip(i * batchSize).Take(batchSize).ToList();
                var batchNumber = i + 1;

                _logger?.LogInformation($"Uploading batch {batchNumber}/{totalBatches} ({batch.Count} schedules)");
                _notificationService.ShowInfo($"Đang tải lên batch {batchNumber}/{totalBatches} ({batch.Count} lịch học)...");

                try
                {
                    var result = await UploadBatchAsync(batch, batchNumber, totalBatches, overwriteExisting);
                    if (result)
                    {
                        successfulBatches++;
                        _logger?.LogInformation($"Batch {batchNumber} uploaded successfully");
                    }
                    else
                    {
                        failedBatches++;
                        _logger?.LogError($"Batch {batchNumber} upload failed");

                        // Ask user if they want to continue with remaining batches
                        var continueUpload = MessageBox.Show(
                            $"Batch {batchNumber} tải lên thất bại. Bạn có muốn tiếp tục với các batch còn lại không?",
                            "Lỗi tải lên batch",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning) == MessageBoxResult.Yes;

                        if (!continueUpload)
                        {
                            _logger?.LogInformation("User chose to stop batch upload after failure");
                            break;
                        }
                    }

                    // Add a configurable delay between batches to avoid overwhelming the server
                    if (i < totalBatches - 1) // Don't delay after the last batch
                    {
                        var delayMs = _configuration.GetValue<int>("ApiConfiguration:DelayBetweenBatches", 2000);
                        _logger?.LogInformation($"Waiting {delayMs}ms before next batch...");
                        await Task.Delay(delayMs);
                    }
                }
                catch (Exception ex)
                {
                    failedBatches++;
                    _logger?.LogError(ex, $"Error uploading batch {batchNumber}");

                    // Ask user if they want to continue
                    var continueUpload = MessageBox.Show(
                        $"Có lỗi xảy ra khi tải batch {batchNumber}: {ex.Message}\nBạn có muốn tiếp tục với các batch còn lại không?",
                        "Lỗi tải lên batch",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Error) == MessageBoxResult.Yes;

                    if (!continueUpload)
                    {
                        break;
                    }
                }
            }

            // Show final results
            var totalProcessed = successfulBatches + failedBatches;
            if (successfulBatches == totalBatches)
            {
                _notificationService.ShowSuccess($"Tải lên thành công tất cả {totalBatches} batch!");
                return true;
            }
            else if (successfulBatches > 0)
            {
                _notificationService.ShowWarning($"Tải lên thành công {successfulBatches}/{totalProcessed} batch. " +
                                                $"Có {failedBatches} batch thất bại.");
                return false;
            }
            else
            {
                _notificationService.ShowError("Tất cả các batch đều tải lên thất bại.");
                return false;
            }
        }

        /// <summary>
        /// Upload a single batch of schedules with retry logic for rate limiting
        /// </summary>
        /// <param name="batch">Batch of schedules to upload</param>
        /// <param name="batchNumber">Current batch number</param>
        /// <param name="totalBatches">Total number of batches</param>
        /// <param name="overwriteExisting">Whether to overwrite existing schedules</param>
        /// <returns>True if batch uploaded successfully, false otherwise</returns>
        private async Task<bool> UploadBatchAsync(List<Schedule> batch, int batchNumber, int totalBatches, bool overwriteExisting = false)
        {
            var maxRetries = _configuration.GetValue<int>("ApiConfiguration:MaxRateLimitRetries", 5);
            var baseRetryDelay = _configuration.GetValue<int>("ApiConfiguration:RateLimitRetryDelay", 30000); // 30 seconds

            for (int retry = 0; retry <= maxRetries; retry++)
            {
                try
                {
                    // Check authentication status
                    if (!_authState.IsAuthenticated)
                    {
                        _logger?.LogWarning("User is not authenticated. Cannot upload batch.");
                        return false;
                    }

                    // Convert to DTOs for optimal JSON payload
                    var uploadDtos = batch.Select(s => new ScheduleUploadDto
                    {
                        ScheduleId = s.ScheduleId.ToString(),
                        GroupName = s.GroupName,
                        SubjectCode = s.SubjectCode,
                        Date = s.Date,
                        SlotTime = s.SlotTime?.ToString(),
                        RoomName = s.RoomName,
                        SessionNo = s.SessionNo,
                        LecturerName = s.LecturerName,
                        SlotTypeCode = s.SlotTypeCode,
                        StatusSlot = s.StatusSlot,
                        TypeSlot = s.TypeSlot,
                        RoomId = s.RoomId?.ToString(),
                        PartOfDay = s.PartOfDay,
                        Major = s.Major,
                        LecturerId = s.LecturerId,
                        LecturerAccount = s.LecturerAccount,
                        TermInYear = s.TermInYear
                    }).ToList();

                    // Create the bulk upload request wrapper
                    var uploadRequest = new BulkUploadRequest
                    {
                        Schedules = uploadDtos,
                        OverwriteExisting = overwriteExisting,
                        ValidateOnly = false
                    };

                    // Serialize to JSON
                    var jsonOptions = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        WriteIndented = false
                    };

                    var jsonPayload = JsonSerializer.Serialize(uploadRequest, jsonOptions);
                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                    // Get API configuration
                    var baseUrl = _configuration["ApiConfiguration:BaseUrl"] ?? "http://localhost:4000";
                    var timeoutSeconds = _configuration.GetValue<int>("ApiConfiguration:TimeoutSeconds", 300);

                    // Use shorter timeout for individual batches (batch size is smaller)
                    var batchTimeoutSeconds = Math.Min(timeoutSeconds, 300); // Max 5 minutes per batch

                    // Add Authorization header
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _authState.AccessToken);

                    // Build endpoint URL
                    var endpoint = $"{baseUrl}/schedules/upload";

                    if (retry > 0)
                    {
                        _logger?.LogInformation($"Retry {retry}/{maxRetries} for batch {batchNumber}/{totalBatches} ({uploadDtos.Count} schedules)");
                        _notificationService.ShowInfo($"Thử lại lần {retry} cho batch {batchNumber}/{totalBatches}...");
                    }
                    else
                    {
                        _logger?.LogInformation($"Uploading batch {batchNumber}/{totalBatches} ({uploadDtos.Count} schedules) to {endpoint}");
                    }

                    // Create cancellation token for this batch
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(batchTimeoutSeconds));

                    // Send POST request
                    var response = await _httpClient.PostAsync(endpoint, content, cts.Token);

                    // Clear Authorization header
                    _httpClient.DefaultRequestHeaders.Authorization = null;

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        _logger?.LogInformation($"Batch {batchNumber} upload successful. Response: {responseContent}");

                        // Parse and display statistics if available
                        try
                        {
                            var apiResponse = JsonSerializer.Deserialize<ApiResponse<BulkUploadResponse>>(responseContent, jsonOptions);
                            if (apiResponse?.Success == true && apiResponse.Data?.Statistics != null)
                            {
                                var stats = apiResponse.Data.Statistics;
                                _logger?.LogInformation($"Batch {batchNumber} statistics - Created: {stats.Created}, Updated: {stats.Updated}, Skipped: {stats.Skipped}, Errors: {stats.Errors}");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogWarning(ex, $"Could not parse response statistics for batch {batchNumber}");
                        }

                        return true;
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        _logger?.LogWarning($"Rate limit exceeded for batch {batchNumber}. Response: {errorContent}");

                        if (retry < maxRetries)
                        {
                            // Exponential backoff: base delay * 2^retry + jitter
                            var delayMs = baseRetryDelay * Math.Pow(2, retry) + Random.Shared.Next(0, 5000);
                            var delaySec = (int)(delayMs / 1000);

                            _logger?.LogInformation($"Waiting {delaySec} seconds before retrying batch {batchNumber}...");
                            _notificationService.ShowWarning($"Server bận. Đợi {delaySec} giây trước khi thử lại batch {batchNumber}...");

                            await Task.Delay((int)delayMs);
                            continue; // Retry this batch
                        }
                        else
                        {
                            _logger?.LogError($"Max retries exceeded for batch {batchNumber} due to rate limiting");
                            _notificationService.ShowError($"Đã vượt quá số lần thử lại cho batch {batchNumber}. Server quá bận.");
                            return false;
                        }
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        _logger?.LogError($"Batch {batchNumber} upload failed. Status: {response.StatusCode}, Response: {errorContent}");
                        return false;
                    }
                }
                catch (TaskCanceledException)
                {
                    _logger?.LogError($"Batch {batchNumber} upload timed out");
                    if (retry < maxRetries)
                    {
                        _logger?.LogInformation($"Retrying batch {batchNumber} due to timeout...");
                        await Task.Delay(5000); // Wait 5 seconds before retry
                        continue;
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, $"Error uploading batch {batchNumber}");
                    if (retry < maxRetries)
                    {
                        _logger?.LogInformation($"Retrying batch {batchNumber} due to error: {ex.Message}");
                        await Task.Delay(5000); // Wait 5 seconds before retry
                        continue;
                    }
                    return false;
                }
            }

            return false; // All retries exhausted
        }

        /// <summary>
        /// Upload all schedules to the server via API
        /// </summary>
        /// <returns>True if upload successful, false otherwise</returns>
        public async Task<bool> UploadAllSchedulesAsync()
        {
            List<Schedule>? schedules = null;

            try
            {
                _logger?.LogInformation("Starting schedule upload process...");

                // Check authentication status
                if (!_authState.IsAuthenticated)
                {
                    _logger?.LogWarning("User is not authenticated. Cannot upload schedules.");
                    _notificationService.ShowError("Bạn cần đăng nhập để tải lên lịch học.");
                    return false;
                }

                // Check if token is close to expiry (within 5 minutes)
                if (_authState.AccessTokenExpiryUtc <= DateTimeOffset.UtcNow.AddMinutes(5))
                {
                    _logger?.LogWarning("Access token is expired or close to expiry. Please refresh token before upload.");
                    _notificationService.ShowWarning("Phiên đăng nhập sắp hết hạn. Vui lòng đăng nhập lại.");
                    return false;
                }

                // Check server connectivity before proceeding
                var baseUrl = _configuration["ApiConfiguration:BaseUrl"] ?? "http://localhost:4000";
                if (!await CheckServerConnectivity(baseUrl))
                {
                    _logger?.LogError("Server connectivity check failed.");
                    _notificationService.ShowError($"Không thể kết nối đến server tại {baseUrl}. Vui lòng kiểm tra:\n" +
                                                 "1. Server có đang chạy không?\n" +
                                                 "2. Địa chỉ server có đúng không?\n" +
                                                 "3. Firewall có chặn kết nối không?");
                    return false;
                }

                // Get all schedules from database
                schedules = await GetAllAsync();
                if (schedules == null || !schedules.Any())
                {
                    _logger?.LogWarning("No schedules found for upload.");
                    _notificationService.ShowWarning("Không có lịch học nào để tải lên.");
                    return false;
                }

                _logger?.LogInformation($"Found {schedules.Count} schedules to upload.");

                // Use batch processing for large uploads to prevent timeouts
                var batchThreshold = _configuration.GetValue<int>("ApiConfiguration:BatchThreshold", 2000);
                if (schedules.Count > batchThreshold)
                {
                    _logger?.LogInformation($"Large upload detected ({schedules.Count} records). Using batch processing...");
                    _notificationService.ShowInfo($"Phát hiện tải lên lớn ({schedules.Count:N0} lịch học). Sẽ chia thành nhiều batch để tối ưu hiệu suất.");

                    // Ask user about overwriting existing schedules
                    var overwriteResult = MessageBox.Show(
                        "Dữ liệu lịch học có thể đã tồn tại trên server. Bạn có muốn ghi đè lên dữ liệu cũ không?\n\n" +
                        "- Chọn 'Có' để cập nhật/ghi đè dữ liệu cũ\n" +
                        "- Chọn 'Không' để chỉ thêm lịch học mới (bỏ qua lịch đã tồn tại)",
                        "Ghi đè dữ liệu cũ?",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    bool overwriteExisting = overwriteResult == MessageBoxResult.Yes;
                    _logger?.LogInformation($"User selected overwrite existing: {overwriteExisting}");

                    var batchSize = _configuration.GetValue<int>("ApiConfiguration:BatchSize", 1000);
                    return await UploadSchedulesInBatchesAsync(schedules, batchSize, overwriteExisting);
                }

                // Convert to DTOs for optimal JSON payload
                var uploadDtos = schedules.Select(s => new ScheduleUploadDto
                {
                    ScheduleId = s.ScheduleId.ToString(),
                    GroupName = s.GroupName,
                    SubjectCode = s.SubjectCode,
                    Date = s.Date,
                    SlotTime = s.SlotTime?.ToString(),
                    RoomName = s.RoomName,
                    SessionNo = s.SessionNo,
                    LecturerName = s.LecturerName,
                    SlotTypeCode = s.SlotTypeCode,
                    StatusSlot = s.StatusSlot,
                    TypeSlot = s.TypeSlot,
                    RoomId = s.RoomId?.ToString(),
                    PartOfDay = s.PartOfDay,
                    Major = s.Major,
                    LecturerId = s.LecturerId,
                    LecturerAccount = s.LecturerAccount,
                    TermInYear = s.TermInYear
                }).ToList();

                // Create the bulk upload request wrapper
                var uploadRequest = new BulkUploadRequest
                {
                    Schedules = uploadDtos,
                    OverwriteExisting = false,
                    ValidateOnly = false
                };

                // Serialize to JSON
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false // Compact JSON for better performance
                };

                var jsonPayload = JsonSerializer.Serialize(uploadRequest, jsonOptions);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                // Get API configuration (baseUrl already defined above)
                var timeoutSeconds = _configuration.GetValue<int>("ApiConfiguration:TimeoutSeconds", 30);

                // Add Authorization header with Bearer token
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _authState.AccessToken);

                // Build the endpoint URL (matching documentation)
                var endpoint = $"{baseUrl}/schedules/upload";

                _logger?.LogInformation($"Uploading {uploadDtos.Count} schedules to {endpoint} with authentication...");

                // Warn user about large uploads
                if (uploadDtos.Count > 1000)
                {
                    _logger?.LogInformation($"Large upload detected ({uploadDtos.Count} records). This may take several minutes...");
                    _notificationService.ShowInfo($"Đang tải lên {uploadDtos.Count} lịch học. Quá trình này có thể mất vài phút, vui lòng đợi...");
                }

                // Create cancellation token with timeout instead of modifying HttpClient.Timeout
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));

                // Send POST request with cancellation token
                var response = await _httpClient.PostAsync(endpoint, content, cts.Token);

                // Clear Authorization header after request to avoid affecting other requests
                _httpClient.DefaultRequestHeaders.Authorization = null;

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger?.LogInformation($"Upload successful. Response: {responseContent}");

                    // Try to parse response for additional details
                    try
                    {
                        var apiResponse = JsonSerializer.Deserialize<ApiResponse<BulkUploadResponse>>(responseContent, jsonOptions);
                        if (apiResponse?.Success == true && apiResponse.Data?.Statistics != null)
                        {
                            var stats = apiResponse.Data.Statistics;
                            _notificationService.ShowSuccess($"Tải lên thành công! Đã tạo: {stats.Created}, Cập nhật: {stats.Updated}, Lỗi: {stats.Errors}");
                        }
                        else
                        {
                            _notificationService.ShowSuccess($"Tải lên thành công {uploadDtos.Count} lịch học!");
                        }
                    }
                    catch
                    {
                        _notificationService.ShowSuccess($"Tải lên thành công {uploadDtos.Count} lịch học!");
                    }

                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger?.LogError($"Upload failed. Status: {response.StatusCode}, Response: {errorContent}");

                    // Handle authentication errors specifically
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        _logger?.LogWarning("Upload failed due to unauthorized access. Token may be expired.");
                        _notificationService.ShowError("Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.");
                        return false;
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        _logger?.LogWarning("Upload failed due to insufficient permissions.");
                        _notificationService.ShowError("Bạn không có quyền thực hiện chức năng này.");
                        return false;
                    }

                    // Try to parse error response for better user feedback
                    try
                    {
                        var apiErrorResponse = JsonSerializer.Deserialize<ApiResponse<object>>(errorContent, jsonOptions);
                        if (apiErrorResponse?.Error != null)
                        {
                            _notificationService.ShowError($"Lỗi tải lên: {apiErrorResponse.Error.Message ?? response.ReasonPhrase}");
                        }
                        else
                        {
                            // Try old format for backward compatibility
                            var errorDto = JsonSerializer.Deserialize<ScheduleUploadErrorDto>(errorContent, jsonOptions);
                            _notificationService.ShowError($"Lỗi tải lên: {errorDto?.Error ?? response.ReasonPhrase}");
                        }
                    }
                    catch
                    {
                        _notificationService.ShowError($"Lỗi tải lên: {response.StatusCode} - {response.ReasonPhrase}");
                    }

                    return false;
                }
            }
            catch (HttpRequestException httpEx)
            {
                _logger?.LogError(httpEx, "Network error during schedule upload.");

                // Get the base URL for the error message
                var baseUrl = _configuration["ApiConfiguration:BaseUrl"] ?? "http://localhost:4000";

                // Check if it's a server connection issue
                if (httpEx.Message.Contains("connection was forcibly closed") ||
                    httpEx.Message.Contains("No connection could be made") ||
                    httpEx.InnerException is System.Net.Sockets.SocketException)
                {
                    _notificationService.ShowError($"Không thể kết nối đến server tại {baseUrl}. Vui lòng kiểm tra:\n" +
                                                 "1. Server có đang chạy không?\n" +
                                                 "2. Địa chỉ server có đúng không?\n" +
                                                 "3. Firewall có chặn kết nối không?");
                }
                else
                {
                    _notificationService.ShowError("Lỗi kết nối mạng. Vui lòng kiểm tra kết nối internet và thử lại.");
                }
                return false;
            }
            catch (TaskCanceledException timeoutEx)
            {
                _logger?.LogError(timeoutEx, "Upload request timed out.");

                // Provide more detailed timeout information
                var configuredTimeout = _configuration.GetValue<int>("ApiConfiguration:TimeoutSeconds", 300);
                var uploadCount = schedules?.Count ?? 0;

                string timeoutMessage;
                if (uploadCount > 5000)
                {
                    timeoutMessage = $"Quá trình tải lên {uploadCount:N0} lịch học bị hết thời gian chờ ({configuredTimeout} giây). " +
                                   "Với số lượng lớn như vậy, hãy thử:\n" +
                                   "1. Chia nhỏ dữ liệu thành nhiều lần tải\n" +
                                   "2. Kiểm tra kết nối mạng\n" +
                                   "3. Liên hệ quản trị viên để tăng thời gian chờ server";
                }
                else if (uploadCount > 1000)
                {
                    timeoutMessage = $"Quá trình tải lên {uploadCount:N0} lịch học bị hết thời gian chờ ({configuredTimeout} giây). " +
                                   "Hãy thử lại hoặc kiểm tra kết nối mạng.";
                }
                else
                {
                    timeoutMessage = "Quá trình tải lên bị hết thời gian chờ. Vui lòng kiểm tra kết nối mạng và thử lại.";
                }

                _notificationService.ShowError(timeoutMessage);
                return false;
            }
            catch (JsonException jsonEx)
            {
                _logger?.LogError(jsonEx, "JSON serialization error during schedule upload.");
                _notificationService.ShowError("Lỗi xử lý dữ liệu. Vui lòng thử lại.");
                return false;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Unexpected error during schedule upload.");
                _notificationService.ShowError("Có lỗi không mong đợi xảy ra khi tải lên dữ liệu.");
                return false;
            }
        }

        /// <summary>
        /// Check if the server is reachable by attempting a simple HTTP request
        /// </summary>
        /// <param name="baseUrl">The base URL of the server to check</param>
        /// <returns>True if server is reachable, false otherwise</returns>
        private async Task<bool> CheckServerConnectivity(string baseUrl)
        {
            try
            {
                _logger?.LogInformation($"Checking server connectivity to {baseUrl}...");

                // Create a simple GET request to check if server is reachable
                // Use a lightweight endpoint or just check the base URL
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)); // Short timeout for connectivity check

                // Try to reach the server with a simple request
                var response = await _httpClient.GetAsync($"{baseUrl}/health", cts.Token);

                _logger?.LogInformation($"Server connectivity check result: {response.StatusCode}");
                return true; // If we get any response, server is reachable
            }
            catch (HttpRequestException httpEx)
            {
                _logger?.LogWarning(httpEx, $"Server connectivity check failed with HTTP error: {httpEx.Message}");
                return false;
            }
            catch (TaskCanceledException)
            {
                _logger?.LogWarning("Server connectivity check timed out.");
                return false;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, $"Server connectivity check failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Downloads schedules from the server and saves them to local database
        /// </summary>
        /// <returns>True if download successful, false otherwise</returns>
        public async Task<bool> DownloadSchedulesFromServerAsync()
        {
            try
            {
                if (!_authState.IsAuthenticated)
                {
                    _notificationService.ShowError("Vui lòng đăng nhập để tải dữ liệu từ server.");
                    return false;
                }

                var baseUrl = _configuration["ApiConfiguration:BaseUrl"] ?? "http://localhost:4000";

                // Check server connectivity first
                if (!await CheckServerConnectionAsync())
                {
                    _notificationService.ShowError("Không thể kết nối đến server.");
                    return false;
                }

                // Set authorization header
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _authState.AccessToken);

                var endpoint = $"{baseUrl}/schedules";

                using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
                var response = await _httpClient.GetAsync(endpoint, cts.Token);

                // Clear Authorization header
                _httpClient.DefaultRequestHeaders.Authorization = null;

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger?.LogInformation($"Download successful. Response length: {responseContent.Length}");

                    // Parse response - use dynamic parsing for flexibility
                    var jsonOptions = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    var apiResponse = JsonSerializer.Deserialize<ApiResponse<dynamic>>(responseContent, jsonOptions);

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        try
                        {
                            // Try to extract schedules array from response
                            var schedulesElement = ((JsonElement)apiResponse.Data).GetProperty("schedules");
                            var serverSchedules = JsonSerializer.Deserialize<List<ScheduleUploadDto>>(schedulesElement.GetRawText(), jsonOptions);

                            if (serverSchedules?.Any() == true)
                            {
                                // Convert server schedules to local format and save
                                var localSchedules = ConvertServerSchedulesToLocal(serverSchedules);

                                // Clear existing schedules and add new ones
                                await DeleteAllData();
                                await AddScheduleAsync(localSchedules, new Progress<int>());

                                _notificationService.ShowSuccess($"Tải về thành công {localSchedules.Count} lịch học từ server!");
                                return true;
                            }
                            else
                            {
                                _notificationService.ShowWarning("Server không có dữ liệu lịch học nào.");
                                return true; // Not an error, just no data
                            }
                        }
                        catch (Exception parseEx)
                        {
                            _logger?.LogError(parseEx, "Error parsing server response");
                            _notificationService.ShowWarning("Dữ liệu từ server có định dạng không hợp lệ.");
                            return false;
                        }
                    }
                    else
                    {
                        _notificationService.ShowWarning("Server không có dữ liệu lịch học nào.");
                        return true; // Not an error, just no data
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger?.LogError($"Download failed with status {response.StatusCode}: {errorContent}");
                    _notificationService.ShowError($"Tải dữ liệu thất bại. Mã lỗi: {response.StatusCode}");
                    return false;
                }
            }
            catch (HttpRequestException httpEx)
            {
                _logger?.LogError(httpEx, "HTTP error during download");
                var baseUrl = _configuration["ApiConfiguration:BaseUrl"] ?? "http://localhost:4000";

                if (httpEx.Message.Contains("connection was forcibly closed") ||
                    httpEx.Message.Contains("No connection could be made") ||
                    httpEx.InnerException is System.Net.Sockets.SocketException)
                {
                    _notificationService.ShowError($"Không thể kết nối đến server tại {baseUrl}. Vui lòng kiểm tra:\n" +
                                                 "1. Server có đang chạy không?\n" +
                                                 "2. Địa chỉ server có đúng không?\n" +
                                                 "3. Firewall có chặn kết nối không?");
                }
                else
                {
                    _notificationService.ShowError("Lỗi kết nối mạng. Vui lòng kiểm tra kết nối internet và thử lại.");
                }
                return false;
            }
            catch (TaskCanceledException timeoutEx)
            {
                _logger?.LogError(timeoutEx, "Download timeout");
                _notificationService.ShowError("Quá thời gian chờ. Vui lòng thử lại.");
                return false;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Unexpected error during download");
                _notificationService.ShowError($"Có lỗi không mong muốn: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Synchronizes data with server (upload and download)
        /// </summary>
        /// <returns>True if sync successful, false otherwise</returns>
        public async Task<bool> SyncWithServerAsync()
        {
            try
            {
                if (!_authState.IsAuthenticated)
                {
                    _notificationService.ShowError("Vui lòng đăng nhập để đồng bộ với server.");
                    return false;
                }

                _notificationService.ShowInfo("Bắt đầu đồng bộ dữ liệu với server...");

                // First upload local changes
                var uploadSuccess = await UploadAllSchedulesAsync();
                if (!uploadSuccess)
                {
                    _notificationService.ShowError("Đồng bộ thất bại: Không thể tải lên dữ liệu local.");
                    return false;
                }

                // Then download server data
                var downloadSuccess = await DownloadSchedulesFromServerAsync();
                if (!downloadSuccess)
                {
                    _notificationService.ShowError("Đồng bộ thất bại: Không thể tải về dữ liệu từ server.");
                    return false;
                }

                _notificationService.ShowSuccess("Đồng bộ dữ liệu với server thành công!");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during sync");
                _notificationService.ShowError($"Lỗi đồng bộ: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks server connection health
        /// </summary>
        /// <returns>True if server is accessible, false otherwise</returns>
        public async Task<bool> CheckServerConnectionAsync()
        {
            try
            {
                var baseUrl = _configuration["ApiConfiguration:BaseUrl"] ?? "http://localhost:4000";

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var response = await _httpClient.GetAsync($"{baseUrl}/health", cts.Token);

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Converts server schedule format to local schedule format
        /// </summary>
        /// <param name="serverSchedules">Schedules from server</param>
        /// <returns>List of local schedule objects</returns>
        private List<Schedule> ConvertServerSchedulesToLocal(List<ScheduleUploadDto> serverSchedules)
        {
            var localSchedules = new List<Schedule>();

            foreach (var serverSchedule in serverSchedules)
            {
                var localSchedule = new Schedule
                {
                    // Note: ScheduleId in local is int (auto-generated), so we don't set it from server
                    GroupName = serverSchedule.GroupName,
                    SubjectCode = serverSchedule.SubjectCode,
                    Date = serverSchedule.Date,
                    SlotTime = ParseIntFromString(serverSchedule.SlotTime), // Convert string to int
                    RoomName = serverSchedule.RoomName,
                    SessionNo = serverSchedule.SessionNo,
                    LecturerName = serverSchedule.LecturerName,
                    SlotTypeCode = serverSchedule.SlotTypeCode,
                    StatusSlot = serverSchedule.StatusSlot,
                    TypeSlot = serverSchedule.TypeSlot,
                    RoomId = ParseIntFromString(serverSchedule.RoomId), // Convert string to int
                    PartOfDay = serverSchedule.PartOfDay,
                    Major = serverSchedule.Major,
                    LecturerId = serverSchedule.LecturerId,
                    LecturerAccount = serverSchedule.LecturerAccount,
                    TermInYear = serverSchedule.TermInYear
                };

                localSchedules.Add(localSchedule);
            }

            return localSchedules;
        }

        /// <summary>
        /// Helper method to parse int from string
        /// </summary>
        /// <param name="value">String value to parse</param>
        /// <returns>Parsed integer or null if parsing fails</returns>
        private int? ParseIntFromString(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            if (int.TryParse(value, out int result))
                return result;

            return null;
        }

        #endregion
    }
}
