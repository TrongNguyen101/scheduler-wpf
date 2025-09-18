# WPF Client Integration Guide

## Schedule Management System Integration

This document provides comprehensive guidance for integrating your WPF client application with the enhanced Express.js schedule management server.

---

## Table of Contents

1. [Server Configuration & Setup](#server-configuration--setup)
2. [Authentication Integration](#authentication-integration)
3. [Schedule Data Models](#schedule-data-models)
4. [API Endpoints Reference](#api-endpoints-reference)
5. [WPF Client Implementation Examples](#wpf-client-implementation-examples)
6. [Error Handling & Validation](#error-handling--validation)
7. [Performance Considerations](#performance-considerations)
8. [Testing & Validation](#testing--validation)
9. [Troubleshooting Guide](#troubleshooting-guide)

---

## Server Configuration & Setup

### Prerequisites

1. **Node.js** (version 16+ recommended)
2. **MongoDB** (local installation or MongoDB Atlas)
3. **Network Access** to port 4000 (configurable)

### Quick Server Setup

```bash
# Navigate to auth-service directory
cd auth-service

# Install dependencies
npm install

# Configure environment
cp .env.example .env
# Edit .env with your MongoDB connection string

# Start the server
npm run dev
```

### Environment Configuration (.env)

```env
# Server Configuration
PORT=4000

# MongoDB Configuration - REQUIRED
MONGO_URI=mongodb://localhost:27017/scheduler-db
# Or MongoDB Atlas: mongodb+srv://username:password@cluster.mongodb.net/scheduler-db

# JWT Secrets - Change in production
JWT_ACCESS_SECRET=your-secure-access-secret
JWT_REFRESH_SECRET=your-secure-refresh-secret

# JWT Token Expiration
JWT_ACCESS_EXPIRES=900          # 15 minutes
JWT_REFRESH_EXPIRES=1209600     # 14 days

# CORS - Set to your WPF app's needs
CORS_ORIGIN=*

# Environment
NODE_ENV=development
```

### Server Health Check

Verify server is running:

```bash
curl http://localhost:4000/health
```

Expected response:

```json
{
  "ok": true,
  "timestamp": "2024-09-20T10:30:00.000Z",
  "uptime": 3600,
  "memory": {...},
  "version": "1.0.0"
}
```

---

## Authentication Integration

### Authentication Flow

```mermaid
sequenceDiagram
    participant WPF as WPF Client
    participant API as Auth Server
    participant DB as MongoDB

    WPF->>API: POST /auth/login {username, password}
    API->>DB: Verify credentials
    DB-->>API: User data + roles
    API-->>WPF: {accessToken, refreshToken, user}

    Note over WPF: Store tokens securely

    WPF->>API: API Request with Bearer token
    API->>API: Validate token
    API-->>WPF: Protected resource

    Note over WPF: When access token expires

    WPF->>API: POST /auth/refresh {refreshToken}
    API-->>WPF: {accessToken}
```

### WPF Authentication Implementation

#### 1. Authentication Models (C#)

```csharp
// Models/AuthModels.cs
public class LoginRequest
{
    public string Username { get; set; }
    public string Password { get; set; }
}

public class LoginResponse
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public UserInfo User { get; set; }
}

public class UserInfo
{
    public string Id { get; set; }
    public string Username { get; set; }
    public List<string> Roles { get; set; }
}

public class RefreshRequest
{
    public string RefreshToken { get; set; }
}

public class RefreshResponse
{
    public string AccessToken { get; set; }
}

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T Data { get; set; }
    public ApiError Error { get; set; }
    public object Meta { get; set; }
    public DateTime Timestamp { get; set; }
}

public class ApiError
{
    public string Code { get; set; }
    public string Message { get; set; }
    public object Details { get; set; }
}
```

#### 2. HTTP Client Service (C#)

```csharp
// Services/ApiService.cs
using System.Net.Http;
using System.Text;
using System.Text.Json;

public class ApiService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl = "http://localhost:4000";
    private string _accessToken;
    private string _refreshToken;

    public ApiService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "WPF-Scheduler-Client/1.0");
    }

    public async Task<ApiResponse<LoginResponse>> LoginAsync(string username, string password)
    {
        var request = new LoginRequest { Username = username, Password = password };
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync($"{_baseUrl}/auth/login", content);
            var responseJson = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var loginResponse = JsonSerializer.Deserialize<LoginResponse>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                _accessToken = loginResponse.AccessToken;
                _refreshToken = loginResponse.RefreshToken;

                // Set default authorization header
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);

                return new ApiResponse<LoginResponse>
                {
                    Success = true,
                    Data = loginResponse
                };
            }
            else
            {
                var errorResponse = JsonSerializer.Deserialize<ApiResponse<object>>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return new ApiResponse<LoginResponse>
                {
                    Success = false,
                    Error = errorResponse.Error
                };
            }
        }
        catch (Exception ex)
        {
            return new ApiResponse<LoginResponse>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "CONNECTION_ERROR",
                    Message = "Unable to connect to server",
                    Details = ex.Message
                }
            };
        }
    }

    public async Task<bool> RefreshTokenAsync()
    {
        if (string.IsNullOrEmpty(_refreshToken))
            return false;

        var request = new RefreshRequest { RefreshToken = _refreshToken };
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync($"{_baseUrl}/auth/refresh", content);
            var responseJson = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var refreshResponse = JsonSerializer.Deserialize<RefreshResponse>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                _accessToken = refreshResponse.AccessToken;
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);

                return true;
            }
        }
        catch (Exception ex)
        {
            // Log error
            Console.WriteLine($"Token refresh failed: {ex.Message}");
        }

        return false;
    }

    public async Task LogoutAsync()
    {
        if (!string.IsNullOrEmpty(_refreshToken))
        {
            var request = new { RefreshToken = _refreshToken };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                await _httpClient.PostAsync($"{_baseUrl}/auth/logout", content);
            }
            catch
            {
                // Ignore logout errors
            }
        }

        _accessToken = null;
        _refreshToken = null;
        _httpClient.DefaultRequestHeaders.Authorization = null;
    }

    // Generic API call method with automatic token refresh
    public async Task<ApiResponse<T>> CallApiAsync<T>(HttpMethod method, string endpoint, object data = null)
    {
        HttpContent content = null;
        if (data != null)
        {
            var json = JsonSerializer.Serialize(data);
            content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        var request = new HttpRequestMessage(method, $"{_baseUrl}{endpoint}")
        {
            Content = content
        };

        try
        {
            var response = await _httpClient.SendAsync(request);
            var responseJson = await response.Content.ReadAsStringAsync();

            // Handle token expiration
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                var refreshSuccess = await RefreshTokenAsync();
                if (refreshSuccess)
                {
                    // Retry with new token
                    request.Headers.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
                    response = await _httpClient.SendAsync(request);
                    responseJson = await response.Content.ReadAsStringAsync();
                }
            }

            if (response.IsSuccessStatusCode)
            {
                if (typeof(T) == typeof(string))
                {
                    return new ApiResponse<T>
                    {
                        Success = true,
                        Data = (T)(object)responseJson
                    };
                }

                var apiResponse = JsonSerializer.Deserialize<ApiResponse<T>>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return apiResponse;
            }
            else
            {
                var errorResponse = JsonSerializer.Deserialize<ApiResponse<object>>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return new ApiResponse<T>
                {
                    Success = false,
                    Error = errorResponse.Error
                };
            }
        }
        catch (Exception ex)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "REQUEST_ERROR",
                    Message = "Request failed",
                    Details = ex.Message
                }
            };
        }
    }
}
```

---

## Schedule Data Models

### Server-Side Schedule Schema

The server expects schedule data in this format:

```javascript
{
  "scheduleId": "string",      // Unique identifier
  "groupName": "string",       // Class/group name (required)
  "subjectCode": "string",     // Subject code (required)
  "date": "2024-09-20T00:00:00.000Z", // ISO date string (required)
  "slotTime": "string",        // Time slot like "07:30-09:00" (required)
  "roomName": "string",        // Room name (required)
  "sessionNo": 1,              // Session number (required, integer)
  "lecturerName": "string",    // Lecturer name (required)
  "slotTypeCode": "string",    // Type like "THEORY", "LAB" (required)
  "statusSlot": "string",      // Status like "ACTIVE", "CANCELLED" (required)
  "typeSlot": "string",        // Type like "NORMAL", "MAKEUP" (required)
  "roomId": "string",          // Room identifier (required)
  "partOfDay": "string",       // "MORNING", "AFTERNOON", "EVENING" (required)
  "major": "string",           // Major/department (required)
  "lecturerId": "string",      // Lecturer identifier (required)
  "lecturerAccount": "string", // Optional lecturer account
  "termInYear": "string"       // Optional term like "2024-FALL"
}
```

### WPF Schedule Models (C#)

```csharp
// Models/ScheduleModels.cs
public class ScheduleUploadDto
{
    public string ScheduleId { get; set; }
    public string GroupName { get; set; }
    public string SubjectCode { get; set; }
    public DateTime Date { get; set; }
    public string SlotTime { get; set; }
    public string RoomName { get; set; }
    public int SessionNo { get; set; }
    public string LecturerName { get; set; }
    public string SlotTypeCode { get; set; }
    public string StatusSlot { get; set; }
    public string TypeSlot { get; set; }
    public string RoomId { get; set; }
    public string PartOfDay { get; set; }
    public string Major { get; set; }
    public string LecturerId { get; set; }
    public string LecturerAccount { get; set; }
    public string TermInYear { get; set; }
}

public class BulkUploadRequest
{
    public List<ScheduleUploadDto> Schedules { get; set; }
    public bool OverwriteExisting { get; set; } = false;
    public bool ValidateOnly { get; set; } = false;
}

public class BulkUploadResponse
{
    public string Message { get; set; }
    public string UploadBatch { get; set; }
    public UploadStatistics Statistics { get; set; }
    public List<UploadError> Errors { get; set; }
}

public class UploadStatistics
{
    public int TotalRecords { get; set; }
    public int Created { get; set; }
    public int Updated { get; set; }
    public int Skipped { get; set; }
    public int Errors { get; set; }
}

public class UploadError
{
    public int Index { get; set; }
    public string ScheduleId { get; set; }
    public string Error { get; set; }
}

public class ScheduleQueryParams
{
    public int Page { get; set; } = 1;
    public int Limit { get; set; } = 50;
    public string LecturerId { get; set; }
    public string SubjectCode { get; set; }
    public string GroupName { get; set; }
    public string RoomId { get; set; }
    public string Major { get; set; }
    public string TermInYear { get; set; }
    public string StatusSlot { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string SortBy { get; set; } = "date";
    public string SortOrder { get; set; } = "asc";
}

public class ScheduleQueryResponse
{
    public List<ScheduleUploadDto> Data { get; set; }
    public PaginationMeta Meta { get; set; }
}

public class PaginationMeta
{
    public PaginationInfo Pagination { get; set; }
    public object Filters { get; set; }
}

public class PaginationInfo
{
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int TotalRecords { get; set; }
    public int RecordsPerPage { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}
```

---

## API Endpoints Reference

### Authentication Endpoints

#### 1. Login

```http
POST /auth/login
Content-Type: application/json

{
  "username": "admin",
  "password": "admin123"
}
```

**Success Response (200):**

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": "u_admin",
    "username": "admin",
    "roles": ["Admin"]
  }
}
```

#### 2. Refresh Token

```http
POST /auth/refresh
Content-Type: application/json

{
  "refreshToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

#### 3. Get User Info

```http
GET /auth/me
Authorization: Bearer {accessToken}
```

#### 4. Logout

```http
POST /auth/logout
Content-Type: application/json

{
  "refreshToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

### Schedule Management Endpoints

#### 1. Bulk Upload Schedules

```http
POST /schedules/upload
Authorization: Bearer {accessToken}
Content-Type: application/json

{
  "schedules": [
    {
      "scheduleId": "SCH001",
      "groupName": "SE1801",
      "subjectCode": "PRN231",
      "date": "2024-09-20T00:00:00.000Z",
      "slotTime": "07:30-09:00",
      "roomName": "DE-301",
      "sessionNo": 1,
      "lecturerName": "Nguyen Van A",
      "slotTypeCode": "THEORY",
      "statusSlot": "ACTIVE",
      "typeSlot": "NORMAL",
      "roomId": "R301",
      "partOfDay": "MORNING",
      "major": "Software Engineering",
      "lecturerId": "GV001",
      "lecturerAccount": "nguyenvana",
      "termInYear": "2024-FALL"
    }
  ],
  "overwriteExisting": false,
  "validateOnly": false
}
```

**Success Response (201):**

```json
{
  "success": true,
  "data": {
    "message": "Bulk upload completed",
    "uploadBatch": "550e8400-e29b-41d4-a716-446655440000",
    "statistics": {
      "totalRecords": 1,
      "created": 1,
      "updated": 0,
      "skipped": 0,
      "errors": 0
    },
    "errors": []
  },
  "timestamp": "2024-09-20T10:30:00.000Z"
}
```

#### 2. Query Schedules

```http
GET /schedules?page=1&limit=50&lecturerId=GV001&startDate=2024-09-01&endDate=2024-09-30
Authorization: Bearer {accessToken}
```

**Success Response (200):**

```json
{
  "success": true,
  "data": [
    {
      "scheduleId": "SCH001",
      "groupName": "SE1801"
      // ... schedule data
    }
  ],
  "meta": {
    "pagination": {
      "currentPage": 1,
      "totalPages": 5,
      "totalRecords": 247,
      "recordsPerPage": 50,
      "hasNextPage": true,
      "hasPreviousPage": false
    }
  },
  "timestamp": "2024-09-20T10:30:00.000Z"
}
```

#### 3. Get Schedule by ID

```http
GET /schedules/{scheduleId}
Authorization: Bearer {accessToken}
```

#### 4. Update Schedule

```http
PUT /schedules/{scheduleId}
Authorization: Bearer {accessToken}
Content-Type: application/json

{
  "statusSlot": "CANCELLED",
  "roomName": "DE-302"
}
```

#### 5. Delete Schedule

```http
DELETE /schedules/{scheduleId}
Authorization: Bearer {accessToken}
```

#### 6. Get Schedule Statistics

```http
GET /schedules/stats?termInYear=2024-FALL&major=Software Engineering
Authorization: Bearer {accessToken}
```

#### 7. Bulk Delete Schedules

```http
POST /schedules/bulk-delete
Authorization: Bearer {accessToken}
Content-Type: application/json

{
  "filters": {
    "termInYear": "2024-FALL",
    "statusSlot": "CANCELLED"
  }
}
```

---

## WPF Client Implementation Examples

### Schedule Service Implementation

```csharp
// Services/ScheduleService.cs
public class ScheduleService
{
    private readonly ApiService _apiService;

    public ScheduleService(ApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<ApiResponse<BulkUploadResponse>> BulkUploadSchedulesAsync(
        List<ScheduleUploadDto> schedules,
        bool overwriteExisting = false,
        bool validateOnly = false)
    {
        var request = new BulkUploadRequest
        {
            Schedules = schedules,
            OverwriteExisting = overwriteExisting,
            ValidateOnly = validateOnly
        };

        return await _apiService.CallApiAsync<BulkUploadResponse>(
            HttpMethod.Post,
            "/schedules/upload",
            request);
    }

    public async Task<ApiResponse<ScheduleQueryResponse>> GetSchedulesAsync(ScheduleQueryParams queryParams)
    {
        var queryString = BuildQueryString(queryParams);
        return await _apiService.CallApiAsync<ScheduleQueryResponse>(
            HttpMethod.Get,
            $"/schedules{queryString}");
    }

    public async Task<ApiResponse<ScheduleUploadDto>> GetScheduleByIdAsync(string scheduleId)
    {
        return await _apiService.CallApiAsync<ScheduleUploadDto>(
            HttpMethod.Get,
            $"/schedules/{scheduleId}");
    }

    public async Task<ApiResponse<ScheduleUploadDto>> UpdateScheduleAsync(string scheduleId, object updateData)
    {
        return await _apiService.CallApiAsync<ScheduleUploadDto>(
            HttpMethod.Put,
            $"/schedules/{scheduleId}",
            updateData);
    }

    public async Task<ApiResponse<object>> DeleteScheduleAsync(string scheduleId)
    {
        return await _apiService.CallApiAsync<object>(
            HttpMethod.Delete,
            $"/schedules/{scheduleId}");
    }

    private string BuildQueryString(ScheduleQueryParams queryParams)
    {
        var parameters = new List<string>();

        if (queryParams.Page > 1)
            parameters.Add($"page={queryParams.Page}");

        if (queryParams.Limit != 50)
            parameters.Add($"limit={queryParams.Limit}");

        if (!string.IsNullOrEmpty(queryParams.LecturerId))
            parameters.Add($"lecturerId={Uri.EscapeDataString(queryParams.LecturerId)}");

        if (!string.IsNullOrEmpty(queryParams.SubjectCode))
            parameters.Add($"subjectCode={Uri.EscapeDataString(queryParams.SubjectCode)}");

        if (!string.IsNullOrEmpty(queryParams.GroupName))
            parameters.Add($"groupName={Uri.EscapeDataString(queryParams.GroupName)}");

        if (!string.IsNullOrEmpty(queryParams.RoomId))
            parameters.Add($"roomId={Uri.EscapeDataString(queryParams.RoomId)}");

        if (!string.IsNullOrEmpty(queryParams.Major))
            parameters.Add($"major={Uri.EscapeDataString(queryParams.Major)}");

        if (!string.IsNullOrEmpty(queryParams.TermInYear))
            parameters.Add($"termInYear={Uri.EscapeDataString(queryParams.TermInYear)}");

        if (!string.IsNullOrEmpty(queryParams.StatusSlot))
            parameters.Add($"statusSlot={Uri.EscapeDataString(queryParams.StatusSlot)}");

        if (queryParams.StartDate.HasValue)
            parameters.Add($"startDate={queryParams.StartDate.Value:yyyy-MM-dd}");

        if (queryParams.EndDate.HasValue)
            parameters.Add($"endDate={queryParams.EndDate.Value:yyyy-MM-dd}");

        if (queryParams.SortBy != "date")
            parameters.Add($"sortBy={queryParams.SortBy}");

        if (queryParams.SortOrder != "asc")
            parameters.Add($"sortOrder={queryParams.SortOrder}");

        return parameters.Count > 0 ? "?" + string.Join("&", parameters) : "";
    }
}
```

### WPF ViewModel Example

```csharp
// ViewModels/ScheduleManagementViewModel.cs
public class ScheduleManagementViewModel : INotifyPropertyChanged
{
    private readonly ScheduleService _scheduleService;
    private readonly ApiService _apiService;

    public ObservableCollection<ScheduleUploadDto> Schedules { get; set; }
    public string StatusMessage { get; set; }
    public bool IsLoading { get; set; }

    public ICommand LoadSchedulesCommand { get; set; }
    public ICommand UploadSchedulesCommand { get; set; }
    public ICommand DeleteScheduleCommand { get; set; }

    public ScheduleManagementViewModel()
    {
        _apiService = new ApiService();
        _scheduleService = new ScheduleService(_apiService);

        Schedules = new ObservableCollection<ScheduleUploadDto>();

        LoadSchedulesCommand = new RelayCommand(async () => await LoadSchedulesAsync());
        UploadSchedulesCommand = new RelayCommand<List<ScheduleUploadDto>>(async (schedules) =>
            await UploadSchedulesAsync(schedules));
        DeleteScheduleCommand = new RelayCommand<string>(async (scheduleId) =>
            await DeleteScheduleAsync(scheduleId));
    }

    public async Task<bool> LoginAsync(string username, string password)
    {
        IsLoading = true;
        StatusMessage = "Đang đăng nhập...";

        var response = await _apiService.LoginAsync(username, password);

        IsLoading = false;

        if (response.Success)
        {
            StatusMessage = $"Đăng nhập thành công. Chào mừng {response.Data.User.Username}!";
            return true;
        }
        else
        {
            StatusMessage = $"Đăng nhập thất bại: {response.Error.Message}";
            return false;
        }
    }

    private async Task LoadSchedulesAsync()
    {
        IsLoading = true;
        StatusMessage = "Đang tải danh sách lịch học...";

        var queryParams = new ScheduleQueryParams
        {
            Page = 1,
            Limit = 100,
            SortBy = "date",
            SortOrder = "asc"
        };

        var response = await _scheduleService.GetSchedulesAsync(queryParams);

        if (response.Success)
        {
            Schedules.Clear();
            foreach (var schedule in response.Data.Data)
            {
                Schedules.Add(schedule);
            }

            StatusMessage = $"Đã tải {Schedules.Count} lịch học. " +
                          $"Trang {response.Data.Meta.Pagination.CurrentPage}/{response.Data.Meta.Pagination.TotalPages}";
        }
        else
        {
            StatusMessage = $"Lỗi tải dữ liệu: {response.Error.Message}";
        }

        IsLoading = false;
    }

    private async Task UploadSchedulesAsync(List<ScheduleUploadDto> schedulesToUpload)
    {
        if (schedulesToUpload == null || !schedulesToUpload.Any())
        {
            StatusMessage = "Không có dữ liệu để upload.";
            return;
        }

        IsLoading = true;
        StatusMessage = $"Đang upload {schedulesToUpload.Count} lịch học...";

        // First, validate the data
        var validateResponse = await _scheduleService.BulkUploadSchedulesAsync(
            schedulesToUpload,
            overwriteExisting: false,
            validateOnly: true);

        if (!validateResponse.Success)
        {
            StatusMessage = $"Lỗi validation: {validateResponse.Error.Message}";
            IsLoading = false;
            return;
        }

        // Then, upload the data
        var uploadResponse = await _scheduleService.BulkUploadSchedulesAsync(
            schedulesToUpload,
            overwriteExisting: false,
            validateOnly: false);

        if (uploadResponse.Success)
        {
            var stats = uploadResponse.Data.Statistics;
            StatusMessage = $"Upload thành công! " +
                          $"Đã tạo: {stats.Created}, " +
                          $"Cập nhật: {stats.Updated}, " +
                          $"Bỏ qua: {stats.Skipped}, " +
                          $"Lỗi: {stats.Errors}";

            if (stats.Errors > 0)
            {
                // Show error details
                var errorDetails = string.Join("\n",
                    uploadResponse.Data.Errors.Take(5).Select(e =>
                        $"Dòng {e.Index + 1}: {e.Error}"));
                StatusMessage += $"\n\nChi tiết lỗi:\n{errorDetails}";
            }

            // Reload schedules
            await LoadSchedulesAsync();
        }
        else
        {
            StatusMessage = $"Upload thất bại: {uploadResponse.Error.Message}";
        }

        IsLoading = false;
    }

    private async Task DeleteScheduleAsync(string scheduleId)
    {
        IsLoading = true;
        StatusMessage = $"Đang xóa lịch học {scheduleId}...";

        var response = await _scheduleService.DeleteScheduleAsync(scheduleId);

        if (response.Success)
        {
            StatusMessage = $"Đã xóa lịch học {scheduleId} thành công.";

            // Remove from local collection
            var scheduleToRemove = Schedules.FirstOrDefault(s => s.ScheduleId == scheduleId);
            if (scheduleToRemove != null)
            {
                Schedules.Remove(scheduleToRemove);
            }
        }
        else
        {
            StatusMessage = $"Lỗi xóa lịch học: {response.Error.Message}";
        }

        IsLoading = false;
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
```

---

## Error Handling & Validation

### Common Error Responses

#### Authentication Errors

```json
{
  "success": false,
  "error": {
    "code": "UNAUTHORIZED",
    "message": "Invalid authentication token"
  },
  "timestamp": "2024-09-20T10:30:00.000Z"
}
```

#### Validation Errors

```json
{
  "success": false,
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Invalid request data",
    "details": [
      {
        "field": "schedules.0.date",
        "message": "\"date\" must be a valid date",
        "value": "invalid-date"
      },
      {
        "field": "schedules.0.sessionNo",
        "message": "\"sessionNo\" must be greater than or equal to 1",
        "value": 0
      }
    ]
  },
  "timestamp": "2024-09-20T10:30:00.000Z"
}
```

#### Rate Limit Errors

```json
{
  "success": false,
  "error": {
    "code": "RATE_LIMIT_EXCEEDED",
    "message": "Too many bulk operations. Please try again later."
  },
  "timestamp": "2024-09-20T10:30:00.000Z"
}
```

### WPF Error Handling Implementation

```csharp
// Services/ErrorHandler.cs
public static class ErrorHandler
{
    public static string GetFriendlyMessage(ApiError error)
    {
        return error.Code switch
        {
            "UNAUTHORIZED" => "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.",
            "FORBIDDEN" => "Bạn không có quyền thực hiện thao tác này.",
            "VALIDATION_ERROR" => "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.",
            "RATE_LIMIT_EXCEEDED" => "Bạn đang thực hiện quá nhiều thao tác. Vui lòng chờ một chút.",
            "CONNECTION_ERROR" => "Không thể kết nối đến server. Vui lòng kiểm tra kết nối mạng.",
            "DUPLICATE_ERROR" => "Dữ liệu đã tồn tại trong hệ thống.",
            "NOT_FOUND" => "Không tìm thấy dữ liệu yêu cầu.",
            _ => error.Message ?? "Đã xảy ra lỗi không xác định."
        };
    }

    public static string GetValidationDetails(ApiError error)
    {
        if (error.Code != "VALIDATION_ERROR" || error.Details == null)
            return string.Empty;

        try
        {
            var details = JsonSerializer.Deserialize<List<ValidationError>>(
                error.Details.ToString(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return string.Join("\n", details.Select(d => $"• {d.Field}: {d.Message}"));
        }
        catch
        {
            return error.Details.ToString();
        }
    }
}

public class ValidationError
{
    public string Field { get; set; }
    public string Message { get; set; }
    public object Value { get; set; }
}
```

---

## Performance Considerations

### 1. Batch Size Recommendations

- **Optimal batch size**: 500-1000 records per upload
- **Maximum batch size**: 10,000 records (server limit)
- **For large datasets**: Split into smaller batches

```csharp
// Example: Split large datasets
public async Task<List<BulkUploadResponse>> UploadLargeDatasetAsync(
    List<ScheduleUploadDto> allSchedules,
    int batchSize = 1000)
{
    var responses = new List<BulkUploadResponse>();

    for (int i = 0; i < allSchedules.Count; i += batchSize)
    {
        var batch = allSchedules.Skip(i).Take(batchSize).ToList();

        var response = await _scheduleService.BulkUploadSchedulesAsync(batch);

        if (response.Success)
        {
            responses.Add(response.Data);
        }
        else
        {
            // Handle batch failure
            throw new Exception($"Batch {i / batchSize + 1} failed: {response.Error.Message}");
        }

        // Add delay between batches to avoid rate limiting
        await Task.Delay(1000);
    }

    return responses;
}
```

### 2. Pagination for Large Queries

```csharp
public async Task<List<ScheduleUploadDto>> GetAllSchedulesAsync(ScheduleQueryParams baseParams)
{
    var allSchedules = new List<ScheduleUploadDto>();
    var currentPage = 1;
    var hasMore = true;

    while (hasMore)
    {
        var queryParams = new ScheduleQueryParams
        {
            Page = currentPage,
            Limit = 1000, // Maximum per page
            // Copy other parameters from baseParams
            LecturerId = baseParams.LecturerId,
            Major = baseParams.Major,
            // ... other filters
        };

        var response = await _scheduleService.GetSchedulesAsync(queryParams);

        if (response.Success)
        {
            allSchedules.AddRange(response.Data.Data);
            hasMore = response.Data.Meta.Pagination.HasNextPage;
            currentPage++;
        }
        else
        {
            throw new Exception($"Failed to fetch page {currentPage}: {response.Error.Message}");
        }
    }

    return allSchedules;
}
```

### 3. Connection Management

```csharp
// Services/ConnectionManager.cs
public class ConnectionManager
{
    private readonly ApiService _apiService;
    private readonly Timer _healthCheckTimer;

    public ConnectionManager(ApiService apiService)
    {
        _apiService = apiService;

        // Check server health every 5 minutes
        _healthCheckTimer = new Timer(CheckServerHealth, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));
    }

    private async void CheckServerHealth(object state)
    {
        try
        {
            var response = await _apiService.CallApiAsync<object>(HttpMethod.Get, "/health");

            if (!response.Success)
            {
                // Handle server unavailability
                OnServerUnavailable?.Invoke();
            }
        }
        catch
        {
            OnServerUnavailable?.Invoke();
        }
    }

    public event Action OnServerUnavailable;
}
```

---

## Testing & Validation

### 1. Unit Test Example

```csharp
// Tests/ScheduleServiceTests.cs
[TestClass]
public class ScheduleServiceTests
{
    private ApiService _apiService;
    private ScheduleService _scheduleService;

    [TestInitialize]
    public void Setup()
    {
        _apiService = new ApiService();
        _scheduleService = new ScheduleService(_apiService);
    }

    [TestMethod]
    public async Task BulkUploadSchedules_ValidData_ReturnsSuccess()
    {
        // Arrange
        await _apiService.LoginAsync("admin", "admin123");

        var schedules = new List<ScheduleUploadDto>
        {
            new ScheduleUploadDto
            {
                ScheduleId = "TEST001",
                GroupName = "SE1801",
                SubjectCode = "PRN231",
                Date = DateTime.Today.AddDays(1),
                SlotTime = "07:30-09:00",
                RoomName = "DE-301",
                SessionNo = 1,
                LecturerName = "Test Lecturer",
                SlotTypeCode = "THEORY",
                StatusSlot = "ACTIVE",
                TypeSlot = "NORMAL",
                RoomId = "R301",
                PartOfDay = "MORNING",
                Major = "Software Engineering",
                LecturerId = "GV001"
            }
        };

        // Act
        var response = await _scheduleService.BulkUploadSchedulesAsync(schedules);

        // Assert
        Assert.IsTrue(response.Success);
        Assert.AreEqual(1, response.Data.Statistics.Created);
        Assert.AreEqual(0, response.Data.Statistics.Errors);
    }

    [TestMethod]
    public async Task BulkUploadSchedules_InvalidData_ReturnsValidationError()
    {
        // Arrange
        await _apiService.LoginAsync("admin", "admin123");

        var schedules = new List<ScheduleUploadDto>
        {
            new ScheduleUploadDto
            {
                // Missing required fields
                ScheduleId = "TEST002"
            }
        };

        // Act
        var response = await _scheduleService.BulkUploadSchedulesAsync(schedules);

        // Assert
        Assert.IsFalse(response.Success);
        Assert.AreEqual("VALIDATION_ERROR", response.Error.Code);
    }
}
```

### 2. Integration Test Checklist

**Before running tests, ensure:**

- [ ] Server is running on `http://localhost:4000`
- [ ] MongoDB is accessible and running
- [ ] Test database is clean (use separate DB for testing)
- [ ] Network connectivity is stable

**Test scenarios to validate:**

1. **Authentication Flow**

   - [ ] Login with valid credentials
   - [ ] Login with invalid credentials
   - [ ] Token refresh functionality
   - [ ] Logout functionality

2. **Schedule Management**

   - [ ] Bulk upload with valid data
   - [ ] Bulk upload with validation errors
   - [ ] Query schedules with various filters
   - [ ] Update individual schedule
   - [ ] Delete individual schedule
   - [ ] Bulk delete functionality

3. **Error Handling**

   - [ ] Unauthorized requests (expired token)
   - [ ] Rate limiting (rapid requests)
   - [ ] Server unavailability
   - [ ] Network timeout

4. **Performance**
   - [ ] Upload 1000 schedules
   - [ ] Upload 5000 schedules
   - [ ] Query large datasets with pagination
   - [ ] Concurrent operations

### 3. Sample Test Data Generator

```csharp
// Utils/TestDataGenerator.cs
public static class TestDataGenerator
{
    public static List<ScheduleUploadDto> GenerateSchedules(int count)
    {
        var schedules = new List<ScheduleUploadDto>();
        var random = new Random();

        var subjects = new[] { "PRN231", "SWD392", "SWP391", "DBI202" };
        var groups = new[] { "SE1801", "SE1802", "SE1803", "IA1701" };
        var lecturers = new[] { "Nguyen Van A", "Tran Thi B", "Le Van C" };
        var rooms = new[] { "DE-301", "DE-302", "BE-201", "AL-205" };
        var slots = new[] { "07:30-09:00", "09:10-10:40", "10:50-12:20", "13:00-14:30" };

        for (int i = 0; i < count; i++)
        {
            schedules.Add(new ScheduleUploadDto
            {
                ScheduleId = $"SCH{i + 1:D6}",
                GroupName = groups[random.Next(groups.Length)],
                SubjectCode = subjects[random.Next(subjects.Length)],
                Date = DateTime.Today.AddDays(random.Next(1, 30)),
                SlotTime = slots[random.Next(slots.Length)],
                RoomName = rooms[random.Next(rooms.Length)],
                SessionNo = random.Next(1, 15),
                LecturerName = lecturers[random.Next(lecturers.Length)],
                SlotTypeCode = random.Next(2) == 0 ? "THEORY" : "LAB",
                StatusSlot = "ACTIVE",
                TypeSlot = "NORMAL",
                RoomId = $"R{random.Next(101, 999)}",
                PartOfDay = random.Next(3) switch
                {
                    0 => "MORNING",
                    1 => "AFTERNOON",
                    _ => "EVENING"
                },
                Major = "Software Engineering",
                LecturerId = $"GV{random.Next(1, 100):D3}",
                LecturerAccount = $"lecturer{random.Next(1, 100)}",
                TermInYear = "2024-FALL"
            });
        }

        return schedules;
    }
}
```

---

## Troubleshooting Guide

### Common Issues & Solutions

#### 1. Server Connection Issues

**Problem**: `Failed to connect to localhost port 4000`

**Solutions**:

```bash
# Check if server is running
curl http://localhost:4000/health

# Check server logs
cd auth-service
npm run dev

# Check port availability
netstat -an | grep :4000

# Try different port
# Edit .env file: PORT=4001
```

#### 2. Authentication Issues

**Problem**: `401 Unauthorized` responses

**Solutions**:

- Verify login credentials
- Check token expiration
- Implement token refresh logic
- Verify Authorization header format: `Bearer {token}`

```csharp
// Debug authentication
public async Task DebugAuthAsync()
{
    var loginResponse = await _apiService.LoginAsync("admin", "admin123");
    Console.WriteLine($"Login success: {loginResponse.Success}");

    if (loginResponse.Success)
    {
        Console.WriteLine($"Access token: {loginResponse.Data.AccessToken.Substring(0, 20)}...");

        var meResponse = await _apiService.CallApiAsync<object>(HttpMethod.Get, "/auth/me");
        Console.WriteLine($"User info success: {meResponse.Success}");
    }
}
```

#### 3. Validation Errors

**Problem**: Bulk upload fails with validation errors

**Solutions**:

```csharp
// Validate data before upload
public List<string> ValidateSchedule(ScheduleUploadDto schedule)
{
    var errors = new List<string>();

    if (string.IsNullOrEmpty(schedule.ScheduleId))
        errors.Add("ScheduleId is required");

    if (string.IsNullOrEmpty(schedule.GroupName))
        errors.Add("GroupName is required");

    if (schedule.Date < DateTime.Today)
        errors.Add("Date cannot be in the past");

    if (schedule.SessionNo < 1)
        errors.Add("SessionNo must be greater than 0");

    // Add more validation rules...

    return errors;
}
```

#### 4. Performance Issues

**Problem**: Slow upload for large datasets

**Solutions**:

- Reduce batch size (500-1000 records)
- Add delays between batches
- Use pagination for queries
- Monitor server resources

```csharp
// Performance monitoring
public class PerformanceMonitor
{
    public async Task<TimeSpan> MeasureUploadTime(List<ScheduleUploadDto> schedules)
    {
        var stopwatch = Stopwatch.StartNew();

        var response = await _scheduleService.BulkUploadSchedulesAsync(schedules);

        stopwatch.Stop();

        Console.WriteLine($"Upload took: {stopwatch.Elapsed}");
        Console.WriteLine($"Records per second: {schedules.Count / stopwatch.Elapsed.TotalSeconds:F2}");

        return stopwatch.Elapsed;
    }
}
```

#### 5. Data Consistency Issues

**Problem**: Duplicate or inconsistent data

**Solutions**:

- Use `validateOnly: true` before actual upload
- Implement data validation in WPF client
- Use `overwriteExisting: true` for updates
- Check for duplicates before upload

```csharp
// Data consistency check
public async Task<bool> CheckDataConsistencyAsync(List<ScheduleUploadDto> schedules)
{
    // Check for duplicates in the list
    var duplicateIds = schedules.GroupBy(s => s.ScheduleId)
        .Where(g => g.Count() > 1)
        .Select(g => g.Key)
        .ToList();

    if (duplicateIds.Any())
    {
        Console.WriteLine($"Found duplicate IDs: {string.Join(", ", duplicateIds)}");
        return false;
    }

    // Validate with server
    var validateResponse = await _scheduleService.BulkUploadSchedulesAsync(
        schedules,
        validateOnly: true);

    return validateResponse.Success;
}
```

### Server-Side Debugging

#### Enable Debug Logging

```javascript
// Add to server.js
process.env.DEBUG = "auth-service:*";

// Or set LOG_LEVEL=debug in .env
```

#### Monitor MongoDB Operations

```bash
# MongoDB logs
mongod --logpath /var/log/mongodb/mongod.log --logappend

# Or for Docker
docker logs mongodb-container
```

#### Check Server Health

```bash
# Basic health check
curl http://localhost:4000/health

# Detailed server status
curl http://localhost:4000/health | jq
```

---

## Security Considerations

### 1. Token Security

- Store tokens securely (not in plain text)
- Implement token refresh before expiration
- Clear tokens on logout
- Never log tokens

```csharp
// Secure token storage example
public class SecureTokenStorage
{
    private static readonly string TokenKey = "SchedulerAccessToken";
    private static readonly string RefreshKey = "SchedulerRefreshToken";

    public static void StoreTokens(string accessToken, string refreshToken)
    {
        // Use ProtectedData for Windows applications
        var accessBytes = ProtectedData.Protect(
            Encoding.UTF8.GetBytes(accessToken),
            null,
            DataProtectionScope.CurrentUser);

        var refreshBytes = ProtectedData.Protect(
            Encoding.UTF8.GetBytes(refreshToken),
            null,
            DataProtectionScope.CurrentUser);

        Settings.Default[TokenKey] = Convert.ToBase64String(accessBytes);
        Settings.Default[RefreshKey] = Convert.ToBase64String(refreshBytes);
        Settings.Default.Save();
    }

    public static (string accessToken, string refreshToken) GetTokens()
    {
        try
        {
            var accessBase64 = Settings.Default[TokenKey]?.ToString();
            var refreshBase64 = Settings.Default[RefreshKey]?.ToString();

            if (string.IsNullOrEmpty(accessBase64) || string.IsNullOrEmpty(refreshBase64))
                return (null, null);

            var accessBytes = ProtectedData.Unprotect(
                Convert.FromBase64String(accessBase64),
                null,
                DataProtectionScope.CurrentUser);

            var refreshBytes = ProtectedData.Unprotect(
                Convert.FromBase64String(refreshBase64),
                null,
                DataProtectionScope.CurrentUser);

            return (Encoding.UTF8.GetString(accessBytes), Encoding.UTF8.GetString(refreshBytes));
        }
        catch
        {
            return (null, null);
        }
    }

    public static void ClearTokens()
    {
        Settings.Default[TokenKey] = null;
        Settings.Default[RefreshKey] = null;
        Settings.Default.Save();
    }
}
```

### 2. Input Validation

Always validate data before sending to server:

```csharp
public class ScheduleValidator
{
    public static ValidationResult ValidateSchedule(ScheduleUploadDto schedule)
    {
        var result = new ValidationResult();

        // Required field validation
        if (string.IsNullOrWhiteSpace(schedule.ScheduleId))
            result.AddError("ScheduleId", "Schedule ID is required");

        if (string.IsNullOrWhiteSpace(schedule.GroupName))
            result.AddError("GroupName", "Group name is required");

        // Date validation
        if (schedule.Date == default)
            result.AddError("Date", "Valid date is required");

        if (schedule.Date < DateTime.Today)
            result.AddError("Date", "Date cannot be in the past");

        // Session validation
        if (schedule.SessionNo < 1 || schedule.SessionNo > 50)
            result.AddError("SessionNo", "Session number must be between 1 and 50");

        // Time slot validation
        if (!IsValidTimeSlot(schedule.SlotTime))
            result.AddError("SlotTime", "Invalid time slot format. Expected format: HH:MM-HH:MM");

        return result;
    }

    private static bool IsValidTimeSlot(string slotTime)
    {
        if (string.IsNullOrWhiteSpace(slotTime))
            return false;

        var pattern = @"^([0-1]?[0-9]|2[0-3]):[0-5][0-9]-([0-1]?[0-9]|2[0-3]):[0-5][0-9]$";
        return Regex.IsMatch(slotTime, pattern);
    }
}

public class ValidationResult
{
    public bool IsValid => !Errors.Any();
    public Dictionary<string, List<string>> Errors { get; } = new();

    public void AddError(string field, string message)
    {
        if (!Errors.ContainsKey(field))
            Errors[field] = new List<string>();

        Errors[field].Add(message);
    }

    public string GetErrorSummary()
    {
        return string.Join("\n", Errors.SelectMany(kvp =>
            kvp.Value.Select(error => $"{kvp.Key}: {error}")));
    }
}
```

---

## Complete Example Implementation

Here's a complete WPF window example that demonstrates the integration:

```csharp
// MainWindow.xaml.cs
public partial class MainWindow : Window
{
    private readonly ApiService _apiService;
    private readonly ScheduleService _scheduleService;
    private readonly ScheduleManagementViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();

        _apiService = new ApiService();
        _scheduleService = new ScheduleService(_apiService);
        _viewModel = new ScheduleManagementViewModel();

        DataContext = _viewModel;

        // Load saved tokens
        LoadSavedTokens();
    }

    private void LoadSavedTokens()
    {
        var (accessToken, refreshToken) = SecureTokenStorage.GetTokens();
        if (!string.IsNullOrEmpty(accessToken))
        {
            // Try to use saved tokens
            _apiService.SetTokens(accessToken, refreshToken);

            // Verify tokens are still valid
            _ = Task.Run(async () =>
            {
                var response = await _apiService.CallApiAsync<object>(HttpMethod.Get, "/auth/me");
                if (!response.Success)
                {
                    // Tokens expired, show login
                    Dispatcher.Invoke(() => ShowLoginDialog());
                }
                else
                {
                    // Load initial data
                    Dispatcher.Invoke(() => _viewModel.LoadSchedulesCommand.Execute(null));
                }
            });
        }
        else
        {
            ShowLoginDialog();
        }
    }

    private void ShowLoginDialog()
    {
        var loginDialog = new LoginDialog();
        if (loginDialog.ShowDialog() == true)
        {
            // Login successful, save tokens
            SecureTokenStorage.StoreTokens(
                loginDialog.AccessToken,
                loginDialog.RefreshToken);

            // Load initial data
            _viewModel.LoadSchedulesCommand.Execute(null);
        }
        else
        {
            // User cancelled login
            Close();
        }
    }

    private async void UploadButton_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*",
            Title = "Select schedule file to upload"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            try
            {
                // Parse Excel file
                var schedules = ParseExcelFile(openFileDialog.FileName);

                // Validate data
                var validationErrors = new List<string>();
                foreach (var schedule in schedules)
                {
                    var validationResult = ScheduleValidator.ValidateSchedule(schedule);
                    if (!validationResult.IsValid)
                    {
                        validationErrors.Add($"Row {schedules.IndexOf(schedule) + 1}: {validationResult.GetErrorSummary()}");
                    }
                }

                if (validationErrors.Any())
                {
                    MessageBox.Show(
                        $"Data validation failed:\n{string.Join("\n", validationErrors.Take(10))}",
                        "Validation Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Upload to server
                await _viewModel.UploadSchedulesAsync(schedules);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to process file: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }

    private List<ScheduleUploadDto> ParseExcelFile(string filePath)
    {
        // Implementation depends on your Excel parsing library
        // This is a simplified example
        var schedules = new List<ScheduleUploadDto>();

        using var workbook = new XLWorkbook(filePath);
        var worksheet = workbook.Worksheet(1);

        var rows = worksheet.RowsUsed().Skip(1); // Skip header row

        foreach (var row in rows)
        {
            var schedule = new ScheduleUploadDto
            {
                ScheduleId = row.Cell(1).GetValue<string>(),
                GroupName = row.Cell(2).GetValue<string>(),
                SubjectCode = row.Cell(3).GetValue<string>(),
                Date = row.Cell(4).GetValue<DateTime>(),
                SlotTime = row.Cell(5).GetValue<string>(),
                RoomName = row.Cell(6).GetValue<string>(),
                SessionNo = row.Cell(7).GetValue<int>(),
                LecturerName = row.Cell(8).GetValue<string>(),
                SlotTypeCode = row.Cell(9).GetValue<string>(),
                StatusSlot = row.Cell(10).GetValue<string>(),
                TypeSlot = row.Cell(11).GetValue<string>(),
                RoomId = row.Cell(12).GetValue<string>(),
                PartOfDay = row.Cell(13).GetValue<string>(),
                Major = row.Cell(14).GetValue<string>(),
                LecturerId = row.Cell(15).GetValue<string>(),
                LecturerAccount = row.Cell(16).GetValue<string>(),
                TermInYear = row.Cell(17).GetValue<string>()
            };

            schedules.Add(schedule);
        }

        return schedules;
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        // Save any pending changes
        // Logout from server
        _ = Task.Run(async () => await _apiService.LogoutAsync());
    }
}
```

This comprehensive integration guide provides everything needed to successfully integrate your WPF client application with the enhanced schedule management server. The guide includes practical examples, error handling, security considerations, and performance optimizations to ensure a robust integration.
