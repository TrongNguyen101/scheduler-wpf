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
}