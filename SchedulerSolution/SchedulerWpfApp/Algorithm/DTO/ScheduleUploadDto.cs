using System.Text.Json.Serialization;

namespace SchedulerWpfApp.Algorithm.DTO
{
    /// <summary>
    /// Data Transfer Object for uploading schedule data to server
    /// Contains only the essential fields needed for upload, optimized for JSON serialization
    /// </summary>
    public class ScheduleUploadDto
    {
        [JsonPropertyName("scheduleId")]
        public string? ScheduleId { get; set; }

        [JsonPropertyName("groupName")]
        public string? GroupName { get; set; }

        [JsonPropertyName("subjectCode")]
        public string? SubjectCode { get; set; }

        [JsonPropertyName("date")]
        public DateTime? Date { get; set; }

        [JsonPropertyName("slotTime")]
        public string? SlotTime { get; set; }

        [JsonPropertyName("roomName")]
        public string? RoomName { get; set; }

        [JsonPropertyName("sessionNo")]
        public int? SessionNo { get; set; }

        [JsonPropertyName("lecturerName")]
        public string? LecturerName { get; set; }

        [JsonPropertyName("slotTypeCode")]
        public string? SlotTypeCode { get; set; }

        [JsonPropertyName("statusSlot")]
        public string? StatusSlot { get; set; }

        [JsonPropertyName("typeSlot")]
        public string? TypeSlot { get; set; }

        [JsonPropertyName("roomId")]
        public string? RoomId { get; set; }

        [JsonPropertyName("partOfDay")]
        public string? PartOfDay { get; set; }

        [JsonPropertyName("major")]
        public string? Major { get; set; }

        [JsonPropertyName("lecturerId")]
        public string? LecturerId { get; set; }

        [JsonPropertyName("lecturerAccount")]
        public string? LecturerAccount { get; set; }

        [JsonPropertyName("termInYear")]
        public string? TermInYear { get; set; }
    }

    /// <summary>
    /// Response DTO for upload operation
    /// </summary>
    public class ScheduleUploadResponseDto
    {
        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("uploadedAt")]
        public DateTime UploadedAt { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }
    }

    /// <summary>
    /// Error response DTO for upload operation
    /// </summary>
    public class ScheduleUploadErrorDto
    {
        [JsonPropertyName("error")]
        public string? Error { get; set; }

        [JsonPropertyName("details")]
        public string? Details { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }
    }

    /// <summary>
    /// Bulk upload request wrapper as expected by the server API
    /// </summary>
    public class BulkUploadRequest
    {
        [JsonPropertyName("schedules")]
        public List<ScheduleUploadDto> Schedules { get; set; } = new();

        [JsonPropertyName("overwriteExisting")]
        public bool OverwriteExisting { get; set; } = false;

        [JsonPropertyName("validateOnly")]
        public bool ValidateOnly { get; set; } = false;
    }

    /// <summary>
    /// Bulk upload response as returned by the server API
    /// </summary>
    public class BulkUploadResponse
    {
        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("uploadBatch")]
        public string? UploadBatch { get; set; }

        [JsonPropertyName("statistics")]
        public UploadStatistics? Statistics { get; set; }

        [JsonPropertyName("errors")]
        public List<UploadError>? Errors { get; set; }
    }

    /// <summary>
    /// Upload statistics as returned by the server
    /// </summary>
    public class UploadStatistics
    {
        [JsonPropertyName("totalRecords")]
        public int TotalRecords { get; set; }

        [JsonPropertyName("created")]
        public int Created { get; set; }

        [JsonPropertyName("updated")]
        public int Updated { get; set; }

        [JsonPropertyName("skipped")]
        public int Skipped { get; set; }

        [JsonPropertyName("errors")]
        public int Errors { get; set; }
    }

    /// <summary>
    /// Individual upload error details
    /// </summary>
    public class UploadError
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("scheduleId")]
        public string? ScheduleId { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }

    /// <summary>
    /// Generic API response wrapper
    /// </summary>
    public class ApiResponse<T>
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("data")]
        public T? Data { get; set; }

        [JsonPropertyName("error")]
        public ApiError? Error { get; set; }

        [JsonPropertyName("meta")]
        public object? Meta { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// API error details
    /// </summary>
    public class ApiError
    {
        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("details")]
        public object? Details { get; set; }
    }

    // ========================================
    // Backup & Restore DTOs
    // ========================================

    /// <summary>
    /// Metadata for a backup file
    /// </summary>
    public class BackupMetadata
    {
        [JsonPropertyName("filename")]
        public string Filename { get; set; } = string.Empty;

        [JsonPropertyName("originalName")]
        public string? OriginalName { get; set; }

        [JsonPropertyName("uploadDate")]
        public DateTime UploadDate { get; set; }

        [JsonPropertyName("fileSize")]
        public long FileSize { get; set; }

        [JsonPropertyName("checksum")]
        public string Checksum { get; set; } = string.Empty;

        [JsonPropertyName("userId")]
        public string UserId { get; set; } = string.Empty;

        [JsonPropertyName("username")]
        public string? Username { get; set; }

        /// <summary>
        /// Formatted file size for display
        /// </summary>
        public string FormattedSize => FormatFileSize(FileSize);

        /// <summary>
        /// Formatted upload date for display
        /// </summary>
        public string FormattedDate => UploadDate.ToString("yyyy-MM-dd HH:mm:ss");

        private static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }

    /// <summary>
    /// Progress information for backup operations
    /// </summary>
    public class BackupProgress
    {
        public int PercentComplete { get; set; }
        public string CurrentOperation { get; set; } = string.Empty;
        public long BytesTransferred { get; set; }
        public long TotalBytes { get; set; }
        public string Status { get; set; } = string.Empty;
        public TimeSpan? ElapsedTime { get; set; }
        public TimeSpan? EstimatedRemaining { get; set; }
    }

    /// <summary>
    /// Progress information for restore operations
    /// </summary>
    public class RestoreProgress
    {
        public int PercentComplete { get; set; }
        public string CurrentOperation { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsValidating { get; set; }
        public string? ValidationMessage { get; set; }
        public TimeSpan? ElapsedTime { get; set; }
    }

    /// <summary>
    /// Result of a backup operation
    /// </summary>
    public class BackupResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? Filename { get; set; }
        public string? Checksum { get; set; }
        public long FileSize { get; set; }
        public DateTime BackupDate { get; set; }
        public string? ErrorCode { get; set; }
        public Exception? Exception { get; set; }
        public TimeSpan Duration { get; set; }
    }

    /// <summary>
    /// Result of a restore operation
    /// </summary>
    public class RestoreResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public DateTime? RestoreDate { get; set; }
        public string? BackupFileName { get; set; }
        public bool RollbackPerformed { get; set; }
        public string? ErrorCode { get; set; }
        public Exception? Exception { get; set; }
        public TimeSpan Duration { get; set; }
        public string? RollbackPath { get; set; }
    }

    /// <summary>
    /// Response from server listing available backups
    /// </summary>
    public class BackupListResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("backups")]
        public List<BackupMetadata> Backups { get; set; } = new List<BackupMetadata>();

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("errorCode")]
        public string? ErrorCode { get; set; }

        [JsonPropertyName("totalCount")]
        public int TotalCount { get; set; }
    }

    /// <summary>
    /// Response from server after backup upload
    /// </summary>
    public class BackupUploadResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("metadata")]
        public BackupMetadata? Metadata { get; set; }

        [JsonPropertyName("errorCode")]
        public string? ErrorCode { get; set; }
    }

    /// <summary>
    /// Result of an upload operation
    /// </summary>
    public class UploadResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public BackupMetadata? Metadata { get; set; }
        public string? ErrorCode { get; set; }
        public Exception? Exception { get; set; }
        public TimeSpan Duration { get; set; }
    }

    /// <summary>
    /// Response from server after file upload
    /// </summary>
    public class UploadResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("metadata")]
        public BackupMetadata? Metadata { get; set; }

        [JsonPropertyName("errorCode")]
        public string? ErrorCode { get; set; }
    }
}