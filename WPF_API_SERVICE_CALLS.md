# WPF Client API Service Calls Documentation

This document provides a comprehensive list of all API service calls made by the WPF client to the Express server. Use this to verify server-side endpoint configurations and identify potential mismatches.

## Table of Contents

1. [Server Configuration](#server-configuration)
2. [Authentication Service](#authentication-service)
3. [Schedule Services](#schedule-services)
4. [Backup & Restore Services](#backup--restore-services)
5. [Health Check](#health-check)
6. [Common Headers and Authentication](#common-headers-and-authentication)
7. [Error Response Formats](#error-response-formats)

---

## Server Configuration

### Base Configuration (appsettings.json)

```json
{
  "ApiConfiguration": {
    "BaseUrl": "http://localhost:4000",
    "TimeoutSeconds": 3000,
    "MaxRetryAttempts": 3,
    "EnableLogging": true,
    "BatchThreshold": 2000,
    "BatchSize": 1000,
    "DelayBetweenBatches": 2000,
    "RateLimitRetryDelay": 30000,
    "MaxRateLimitRetries": 5,
    "Backup": {
      "MaxFileSizeMB": 100,
      "TimeoutMinutes": 10,
      "RetryAttempts": 3,
      "ChecksumValidation": true
    }
  }
}
```

### Authentication Configuration

```csharp
public class AuthConfig
{
    public string BaseUrl { get; set; } = "http://localhost:4000";
    public int AutoRefreshBeforeExpirySeconds { get; set; } = 60;
    public string TokenStorage { get; set; } = "DPAPI";
}
```

---

## Authentication Service

### 1. User Login

- **Endpoint**: `POST /auth/login`
- **Method**: POST
- **Content-Type**: application/json
- **Authentication**: None (public endpoint)

**Request Body**:

```json
{
  "username": "string",
  "password": "string"
}
```

**Response Success (200)**:

```json
{
  "accessToken": "string (JWT)",
  "refreshToken": "string",
  "user": {
    "id": "string",
    "username": "string",
    "roles": ["string"]
  }
}
```

### 2. Refresh Token

- **Endpoint**: `POST /auth/refresh`
- **Method**: POST
- **Content-Type**: application/json
- **Authentication**: None (uses refresh token in body)

**Request Body**:

```json
{
  "refreshToken": "string"
}
```

**Response Success (200)**:

```json
{
  "accessToken": "string (JWT)"
}
```

### 3. Get User Profile

- **Endpoint**: `GET /auth/me`
- **Method**: GET
- **Authentication**: Bearer token required

**Response Success (200)**:

```json
{
  "id": "string",
  "username": "string",
  "roles": ["string"]
}
```

---

## Schedule Services

### 1. Bulk Schedule Upload

- **Endpoint**: `POST /schedules/upload`
- **Method**: POST
- **Content-Type**: application/json
- **Authentication**: Bearer token required
- **Timeout**: Configurable (default: 300 seconds per batch)

**Request Body**:

```json
{
  "schedules": [
    {
      "subjectId": "string",
      "subjectName": "string",
      "dayOfWeek": "integer (1-7)",
      "startPeriod": "integer",
      "endPeriod": "integer",
      "roomId": "string",
      "partOfDay": "string",
      "major": "string",
      "lecturerId": "string",
      "lecturerAccount": "string",
      "termInYear": "string"
    }
  ],
  "overwriteExisting": "boolean",
  "validateOnly": "boolean"
}
```

**Response Success (200)**:

```json
{
  "success": true,
  "data": {
    "statistics": {
      "created": "integer",
      "updated": "integer",
      "skipped": "integer",
      "errors": "integer"
    }
  }
}
```

**Response Error (400/500)**:

```json
{
  "success": false,
  "error": {
    "message": "string",
    "code": "string"
  }
}
```

**Rate Limiting Response (429)**:

```json
{
  "error": "Rate limit exceeded",
  "retryAfter": "integer (seconds)"
}
```

---

## Backup & Restore Services

### 1. Upload Backup

- **Endpoint**: `POST /api/backups/upload`
- **Method**: POST
- **Content-Type**: multipart/form-data
- **Authentication**: Bearer token required
- **Timeout**: Configurable (default: 10 minutes)

**Request (Multipart Form)**:

- `file`: Binary file data (SQLite database)
- `checksum`: String (MD5 hash of file)
- `originalName`: String (optional)

**Response Success (200)**:

```json
{
  "success": true,
  "message": "Backup uploaded successfully",
  "metadata": {
    "filename": "string",
    "originalName": "string",
    "uploadDate": "ISO 8601 datetime",
    "fileSize": "integer (bytes)",
    "checksum": "string (MD5)",
    "userId": "string"
  }
}
```

**Response Error**:

```json
{
  "success": false,
  "message": "Error description",
  "errorCode": "string"
}
```

### 2. Get Backup List

- **Endpoint**: `GET /api/backups/list`
- **Method**: GET
- **Authentication**: Bearer token required

**Response Success (200)**:

```json
{
  "success": true,
  "backups": [
    {
      "filename": "string",
      "originalName": "string",
      "uploadDate": "ISO 8601 datetime",
      "fileSize": "integer (bytes)",
      "checksum": "string (MD5)",
      "userId": "string"
    }
  ],
  "totalCount": "integer",
  "message": "string"
}
```

**Response Not Found (404)**:

```json
{
  "success": false,
  "message": "No backups found",
  "backups": [],
  "totalCount": 0
}
```

### 3. Download Backup

- **Endpoint**: `GET /api/backups/download/{filename}`
- **Method**: GET
- **Authentication**: Bearer token required
- **Response**: Binary file stream

**Headers in Response**:

- `Content-Type`: application/octet-stream
- `Content-Disposition`: attachment; filename="backup_filename.db"
- `Content-Length`: file size in bytes
- `X-Checksum`: MD5 hash of file

### 4. Delete Backup

- **Endpoint**: `DELETE /api/backups/{filename}`
- **Method**: DELETE
- **Authentication**: Bearer token required

**Response Success (200)**:

```json
{
  "success": true,
  "message": "Backup deleted successfully"
}
```

**Response Error**:

```json
{
  "success": false,
  "message": "Error description",
  "errorCode": "string"
}
```

---

## Health Check

### Server Health Check

- **Endpoint**: `GET /health`
- **Method**: GET
- **Authentication**: None
- **Timeout**: 10 seconds
- **Purpose**: Check server connectivity

**Expected Response**: Any HTTP response indicates server is reachable

---

## Common Headers and Authentication

### Authorization Header

All authenticated endpoints require:

```
Authorization: Bearer <JWT_TOKEN>
```

### Content-Type Headers

- **JSON requests**: `Content-Type: application/json`
- **File uploads**: `Content-Type: multipart/form-data`

### User-Agent

The WPF client should send an appropriate User-Agent header to identify itself.

---

## Error Response Formats

### Standard API Error Format

```json
{
  "success": false,
  "error": {
    "message": "Human-readable error message",
    "code": "ERROR_CODE",
    "details": "Additional error details (optional)"
  }
}
```

### Legacy Error Format (Backward Compatibility)

```json
{
  "error": "Error message string"
}
```

### HTTP Status Codes Used

- **200**: Success
- **400**: Bad Request (validation errors)
- **401**: Unauthorized (invalid/expired token)
- **403**: Forbidden (insufficient permissions)
- **404**: Not Found (resource not found)
- **429**: Too Many Requests (rate limiting)
- **500**: Internal Server Error

---

## Configuration Validation Checklist

When debugging client-server connectivity issues, verify:

### Authentication

- [ ] Server has `/auth/login` endpoint accepting POST with username/password
- [ ] Server has `/auth/refresh` endpoint accepting POST with refreshToken
- [ ] Server has `/auth/me` endpoint accepting GET with Bearer token
- [ ] JWT tokens are properly formatted and contain expiry information
- [ ] Server validates Bearer tokens on protected endpoints

### Schedule Management

- [ ] Server has `/schedules/upload` endpoint accepting POST with JSON array
- [ ] Server properly handles batch uploads with rate limiting
- [ ] Server returns proper statistics in response format shown above
- [ ] Server handles `overwriteExisting` and `validateOnly` parameters

### Backup Management

- [ ] Server has `/api/backups/upload` endpoint accepting multipart/form-data
- [ ] Server has `/api/backups/list` endpoint returning JSON array
- [ ] Server has `/api/backups/download/{filename}` endpoint returning binary
- [ ] Server has `/api/backups/{filename}` DELETE endpoint
- [ ] Server validates file checksums during upload/download

### General

- [ ] Server base URL matches `ApiConfiguration:BaseUrl` in appsettings.json
- [ ] Server handles CORS if client and server are on different origins
- [ ] Server has `/health` endpoint for connectivity checks
- [ ] Server implements proper rate limiting with 429 responses
- [ ] Server timeout settings accommodate large file uploads/downloads

### Common Issues

1. **CORS errors**: Ensure server allows requests from WPF client origin
2. **Authentication failures**: Check JWT token format and expiry handling
3. **Rate limiting**: Verify server implements exponential backoff for 429 responses
4. **File upload failures**: Check server file size limits and timeout settings
5. **Endpoint mismatches**: Ensure exact endpoint paths match between client and server

---

_This document should be kept in sync with any changes to the WPF client's API service calls._
