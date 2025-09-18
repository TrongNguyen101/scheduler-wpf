# Schedule Upload API Documentation

## Endpoint: Upload Schedules

- **URL:** `/api/schedules/upload`
- **Method:** `POST`
- **Description:** Nhận và lưu một lô lớn dữ liệu lịch trình từ client WPF.

### Body Payload (JSON)

- **Type:** `application/json`
- **Structure:** Một mảng các đối tượng `Schedule`.

**Example Object:**

```json
{
  "ScheduleId": 1,
  "GroupName": "SE1801",
  "SubjectCode": "PRN211",
  "Date": "2025-09-18T00:00:00",
  "SlotTime": 1,
  "RoomName": "BE-101",
  "SessionNo": 1,
  "LecturerName": "Nguyen Van A",
  "SlotTypeCode": "A21",
  "StatusSlot": "Online",
  "TypeSlot": "New Slot",
  "RoomId": 1,
  "PartOfDay": "AM",
  "Major": "Software Engineering",
  "LecturerId": "FPT001",
  "LecturerAccount": "nguyenvana@fpt.edu.vn",
  "TermInYear": "Fall2025"
}
```

**Full Request Example:**

```json
[
  {
    "ScheduleId": 1,
    "GroupName": "SE1801",
    "SubjectCode": "PRN211",
    "Date": "2025-09-18T00:00:00",
    "SlotTime": 1,
    "RoomName": "BE-101",
    "SessionNo": 1,
    "LecturerName": "Nguyen Van A",
    "SlotTypeCode": "A21",
    "StatusSlot": "Online",
    "TypeSlot": "New Slot",
    "RoomId": 1,
    "PartOfDay": "AM",
    "Major": "Software Engineering",
    "LecturerId": "FPT001",
    "LecturerAccount": "nguyenvana@fpt.edu.vn",
    "TermInYear": "Fall2025"
  },
  {
    "ScheduleId": 2,
    "GroupName": "SE1802",
    "SubjectCode": "SWD392",
    "Date": "2025-09-19T00:00:00",
    "SlotTime": 2,
    "RoomName": "BE-102",
    "SessionNo": 2,
    "LecturerName": "Tran Thi B",
    "SlotTypeCode": "A42",
    "StatusSlot": "Offline",
    "TypeSlot": "Old Slot",
    "RoomId": 2,
    "PartOfDay": "PM",
    "Major": "Software Engineering",
    "LecturerId": "FPT002",
    "LecturerAccount": "tranthib@fpt.edu.vn",
    "TermInYear": "Fall2025"
  }
]
```

### Headers

- **Content-Type:** `application/json`
- **Accept:** `application/json`
- **Authorization:** `Bearer <access_token>` (Required)

### Authentication

This endpoint requires authentication using JWT Bearer tokens. The client must:

1. **Authenticate first**: Log in using the `/auth/login` endpoint to obtain access and refresh tokens
2. **Include Bearer token**: Add the `Authorization: Bearer <access_token>` header to all requests
3. **Handle token expiry**: Refresh tokens when they expire using the `/auth/refresh` endpoint

#### Authentication Flow

```
1. POST /auth/login
   Body: { "username": "user", "password": "pass" }
   Response: { "accessToken": "...", "refreshToken": "...", "user": {...} }

2. Store tokens securely (DPAPI in WPF client)

3. POST /api/schedules/upload
   Headers: { "Authorization": "Bearer <accessToken>" }
   
4. If 401 response, refresh token:
   POST /auth/refresh
   Body: { "refreshToken": "..." }
   Response: { "accessToken": "..." }
   
5. Retry upload with new token
```

#### Token Management

- **Access Token**: Short-lived (typically 15-60 minutes), used for API requests
- **Refresh Token**: Long-lived (typically days/weeks), used to obtain new access tokens
- **Token Storage**: Stored securely using Windows DPAPI encryption
- **Auto-refresh**: Client automatically refreshes tokens before expiry

### Example Authenticated Request

```bash
curl -X POST "https://api.example.com/api/schedules/upload" \
  -H "Content-Type: application/json" \
  -H "Accept: application/json" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -d @schedules.json
```

### Success Response

**Code:** `201 Created`

**Body:**

```json
{
  "message": "Schedules uploaded successfully.",
  "count": 8000,
  "uploadedAt": "2025-09-18T10:30:00Z",
  "status": "success"
}
```

### Error Responses

**Code:** `400 Bad Request`

**Body:**

```json
{
  "error": "Invalid data format.",
  "details": "Request body must be a valid JSON array of Schedule objects.",
  "status": "error"
}
```

**Code:** `401 Unauthorized`

**Body:**

```json
{
  "error": "Authentication required.",
  "details": "Valid Bearer token must be provided in Authorization header.",
  "status": "error"
}
```

**Code:** `403 Forbidden`

**Body:**

```json
{
  "error": "Insufficient permissions.",
  "details": "User does not have permission to upload schedules.",
  "status": "error"
}
```

**Code:** `413 Payload Too Large`

**Body:**

```json
{
  "error": "Payload too large.",
  "details": "Maximum allowed payload size is 50MB.",
  "status": "error"
}
```

**Code:** `422 Unprocessable Entity`

**Body:**

```json
{
  "error": "Validation failed.",
  "details": [
    {
      "field": "Date",
      "message": "Date is required and must be a valid datetime."
    },
    {
      "field": "GroupName",
      "message": "GroupName cannot be empty."
    }
  ],
  "status": "error"
}
```

**Code:** `500 Internal Server Error`

**Body:**

```json
{
  "error": "An error occurred while processing the request.",
  "details": "Internal server error occurred during schedule processing.",
  "status": "error"
}
```

### Request Constraints

- Maximum payload size: 50MB
- Maximum number of schedules per request: 10,000
- Timeout: 60 seconds
- All date fields must be in ISO 8601 format

### Data Validation Rules

- `ScheduleId`: Integer, auto-generated on server if not provided
- `GroupName`: Required, string, max 100 characters
- `SubjectCode`: Required, string, max 50 characters
- `Date`: Required, valid datetime in ISO 8601 format
- `SlotTime`: Integer between 1-8
- `RoomName`: String, max 50 characters
- `SessionNo`: Integer, positive number
- `LecturerName`: String, max 100 characters
- `SlotTypeCode`: String, max 10 characters
- `StatusSlot`: Enum values: "Online", "Offline"
- `TypeSlot`: Enum values: "New Slot", "Old Slot"

### Rate Limiting

- Maximum 10 requests per minute per client
- Maximum 100 requests per hour per client

### Authentication

- Currently no authentication required
- Future versions may require API key authentication

### Example cURL Request

```bash
curl -X POST "https://api.example.com/api/schedules/upload" \
  -H "Content-Type: application/json" \
  -H "Accept: application/json" \
  -d @schedules.json
```

### Notes for Implementation Team

1. **Database Considerations:**

   - Ensure database can handle bulk insert operations efficiently
   - Consider using batch processing for large datasets
   - Implement proper indexing on frequently queried fields

2. **Performance Optimization:**

   - Use streaming JSON parser for large payloads
   - Implement compression (gzip) support
   - Consider pagination for very large datasets

3. **Error Handling:**

   - Provide detailed validation errors with field-level information
   - Log all upload attempts for auditing purposes
   - Implement rollback mechanism for failed batch operations

4. **Monitoring:**
   - Track upload success/failure rates
   - Monitor payload sizes and processing times
   - Alert on unusual upload patterns
