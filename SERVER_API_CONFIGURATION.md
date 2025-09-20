# Server API Configuration Documentation

This document provides the **ACTUAL** implemented API configuration of the Express.js auth-service server. Use this to verify and compare with your WPF client implementation.

## Table of Contents

1. [Server Configuration](#server-configuration)
2. [Authentication Endpoints](#authentication-endpoints)
3. [Schedule Management Endpoints](#schedule-management-endpoints)
4. [Backup & Restore Endpoints](#backup--restore-endpoints)
5. [User Management Endpoints](#user-management-endpoints)
6. [System Endpoints](#system-endpoints)
7. [Authentication & Security](#authentication--security)
8. [Error Response Formats](#error-response-formats)
9. [Rate Limiting](#rate-limiting)
10. [WPF Client Comparison](#wpf-client-comparison)

---

## Server Configuration

### Base Server Settings

```javascript
{
  port: 4000,
  environment: "development",
  mongoUri: "mongodb+srv://...",
  corsOrigin: "http://localhost:5173",
  requestTimeout: 30000, // 30 seconds
  maxRequestSize: "50mb"
}
```

### JWT Configuration

```javascript
{
  accessSecret: "change-me-access-secret-for-production",
  refreshSecret: "change-me-refresh-secret-for-production",
  accessExpires: 900,     // 15 minutes (in seconds)
  refreshExpires: 1209600 // 14 days (in seconds)
}
```

### Security Settings

```javascript
{
  bcryptRounds: 12,
  maxLoginAttempts: 5,
  helmet: {
    crossOriginEmbedderPolicy: false,
    contentSecurityPolicy: {
      directives: {
        defaultSrc: ["'self'"],
        styleSrc: ["'self'", "'unsafe-inline'"],
        scriptSrc: ["'self'"],
        imgSrc: ["'self'", "data:", "https:"]
      }
    }
  }
}
```

---

## Authentication Endpoints

### 1. User Login

- **Endpoint**: `POST /auth/login`
- **Authentication**: None (public)
- **Rate Limited**: Yes (100 requests per 15 minutes)
- **Content-Type**: application/json

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
  "success": true,
  "data": {
    "accessToken": "JWT_TOKEN",
    "refreshToken": "REFRESH_TOKEN",
    "user": {
      "id": "string",
      "username": "string",
      "roles": ["array_of_role_names"],
      "department": "string"
    }
  },
  "timestamp": "2025-09-20T10:30:00.000Z"
}
```

**Response Error (400)**:

```json
{
  "success": false,
  "error": {
    "code": "MISSING_CREDENTIALS",
    "message": "Username and password are required"
  },
  "timestamp": "2025-09-20T10:30:00.000Z"
}
```

**Response Error (401)**:

```json
{
  "success": false,
  "error": {
    "code": "INVALID_CREDENTIALS",
    "message": "Invalid username or password"
  },
  "timestamp": "2025-09-20T10:30:00.000Z"
}
```

### 2. Refresh Access Token ✅ IMPLEMENTED

- **Endpoint**: `POST /auth/refresh`
- **Authentication**: None (uses refresh token)
- **Content-Type**: application/json

**Request Body**:

```json
{
  "refreshToken": "string"
}
```

**Response Success (200)**:

```json
{
  "success": true,
  "data": {
    "accessToken": "NEW_JWT_TOKEN"
  },
  "timestamp": "2025-09-20T10:30:00.000Z"
}
```

**Response Error (400)**:

```json
{
  "success": false,
  "error": {
    "code": "MISSING_REFRESH_TOKEN",
    "message": "Refresh token is required"
  },
  "timestamp": "2025-09-20T10:30:00.000Z"
}
```

**Response Error (401)**:

```json
{
  "success": false,
  "error": {
    "code": "INVALID_REFRESH_TOKEN",
    "message": "Invalid or revoked refresh token"
  },
  "timestamp": "2025-09-20T10:30:00.000Z"
}
```

### 3. Get User Profile

- **Endpoint**: `GET /auth/me`
- **Authentication**: Bearer token required

**Response Success (200)**:

```json
{
  "success": true,
  "data": {
    "id": "string",
    "username": "string",
    "roles": ["array_of_role_names"],
    "department": "string"
  },
  "timestamp": "2025-09-20T10:30:00.000Z"
}
```

### 4. Logout

- **Endpoint**: `POST /auth/logout`
- **Authentication**: None
- **Content-Type**: application/json

**Request Body**:

```json
{
  "refreshToken": "string"
}
```

**Response Success (200)**:

```json
{
  "success": true,
  "data": {
    "message": "Logged out successfully"
  },
  "timestamp": "2025-09-20T10:30:00.000Z"
}
```

---

## Schedule Management Endpoints

### 1. Query Schedules

- **Endpoint**: `GET /schedules`
- **Authentication**: Bearer token required
- **Query Parameters**: Validated with scheduleQuerySchema

**Response Success (200)**:

```json
{
  "success": true,
  "data": [
    {
      "id": "string",
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
  "meta": {
    "pagination": "object"
  },
  "timestamp": "2025-09-20T10:30:00.000Z"
}
```

### 2. Get Schedule Statistics

- **Endpoint**: `GET /schedules/stats`
- **Authentication**: Bearer token required

### 3. Get Schedule by ID

- **Endpoint**: `GET /schedules/:id`
- **Authentication**: Bearer token required

### 4. Bulk Upload Schedules ✅ MATCHES WPF EXPECTATION

- **Endpoint**: `POST /schedules/upload`
- **Authentication**: Bearer token required (Academic staff role)
- **Rate Limited**: Yes (5 bulk operations per 15 minutes)
- **Content-Type**: application/json
- **Validation**: Uses bulkScheduleSchema

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

### 5. Update Schedule

- **Endpoint**: `PUT /schedules/:id`
- **Authentication**: Bearer token required (Academic staff role)
- **Validation**: Uses scheduleUpdateSchema

### 6. Delete Schedule

- **Endpoint**: `DELETE /schedules/:id`
- **Authentication**: Bearer token required (Head of Department role)

### 7. Delete All Schedules

- **Endpoint**: `DELETE /schedules/all`
- **Authentication**: Bearer token required (Head of Department role)
- **Rate Limited**: Yes (5 bulk operations per 15 minutes)

### 8. Bulk Delete Schedules

- **Endpoint**: `POST /schedules/bulk-delete`
- **Authentication**: Bearer token required (Head of Department role)
- **Rate Limited**: Yes (5 bulk operations per 15 minutes)

---

## Backup & Restore Endpoints

### 1. Upload Backup ✅ IMPLEMENTED

- **Endpoint**: `POST /api/backups/upload`
- **Authentication**: Bearer token required
- **Content-Type**: multipart/form-data
- **Rate Limited**: Yes (5 uploads per hour, 10 operations per 15 minutes)
- **File Size Limit**: 100MB
- **Allowed Extensions**: .db, .sqlite, .sqlite3

**Request (Multipart Form)**:

- `file`: Binary file data (SQLite database)
- `checksum`: String (MD5 hash of file) - optional but recommended
- `originalName`: String (original filename) - optional

**Response Success (200)**:

```json
{
  "success": true,
  "message": "Backup uploaded successfully",
  "metadata": {
    "filename": "unique_generated_filename.db",
    "originalName": "user_provided_name.db",
    "uploadDate": "2025-09-20T10:30:00.000Z",
    "fileSize": 12345678,
    "checksum": "d41d8cd98f00b204e9800998ecf8427e",
    "userId": "user_id"
  }
}
```

**Response Error (400)**:

```json
{
  "success": false,
  "message": "Invalid file type. Only database files (.db, .sqlite, .sqlite3) are allowed.",
  "errorCode": "INVALID_FILE_TYPE"
}
```

**Response Error (400 - Checksum)**:

```json
{
  "success": false,
  "message": "File checksum validation failed. File may be corrupted.",
  "errorCode": "CHECKSUM_MISMATCH"
}
```

### 2. List Backups ✅ IMPLEMENTED

- **Endpoint**: `GET /api/backups/list`
- **Authentication**: Bearer token required
- **Rate Limited**: Yes (10 operations per 15 minutes)

**Response Success (200)**:

```json
{
  "success": true,
  "backups": [
    {
      "filename": "unique_generated_filename.db",
      "originalName": "user_provided_name.db",
      "uploadDate": "2025-09-20T10:30:00.000Z",
      "fileSize": 12345678,
      "checksum": "d41d8cd98f00b204e9800998ecf8427e",
      "userId": "user_id"
    }
  ],
  "totalCount": 1,
  "message": "Found 1 backup(s)"
}
```

**Response No Backups (200)**:

```json
{
  "success": true,
  "backups": [],
  "totalCount": 0,
  "message": "No backups found"
}
```

### 3. Download Backup ✅ IMPLEMENTED

- **Endpoint**: `GET /api/backups/download/:filename`
- **Authentication**: Bearer token required
- **Rate Limited**: Yes (10 operations per 15 minutes)
- **Response**: Binary file stream

**Headers in Response**:

- `Content-Type`: application/octet-stream
- `Content-Disposition`: attachment; filename="original_name.db"
- `Content-Length`: file size in bytes
- `X-Checksum`: MD5 hash of file

**Response Error (404)**:

```json
{
  "success": false,
  "message": "Backup file not found",
  "errorCode": "FILE_NOT_FOUND"
}
```

### 4. Delete Backup ✅ IMPLEMENTED

- **Endpoint**: `DELETE /api/backups/:filename`
- **Authentication**: Bearer token required
- **Rate Limited**: Yes (10 operations per 15 minutes)

**Response Success (200)**:

```json
{
  "success": true,
  "message": "Backup deleted successfully"
}
```

**Response Error (404)**:

```json
{
  "success": false,
  "message": "Backup file not found",
  "errorCode": "FILE_NOT_FOUND"
}
```

### 5. Alternative List Endpoint

- **Endpoint**: `GET /api/backups`
- **Authentication**: Bearer token required
- **Alias for**: `/api/backups/list`

---

## User Management Endpoints

### Users

- **Base Route**: `/users`
- **Authentication**: Bearer token required for all endpoints

### Departments

- **Base Route**: `/departments`
- **Authentication**: Bearer token required for all endpoints

### Subjects

- **Base Route**: `/subjects`
- **Authentication**: Bearer token required for all endpoints

### Lecturers

- **Base Route**: `/lecturers`
- **Authentication**: Bearer token required for all endpoints

---

## System Endpoints

### 1. Health Check ✅ IMPLEMENTED

- **Endpoint**: `GET /health`
- **Authentication**: None
- **Timeout**: No specific timeout

**Response Success (200)**:

```json
{
  "success": true,
  "data": {
    "status": "OK",
    "timestamp": "2025-09-20T10:30:00.000Z",
    "uptime": 12345.67,
    "memory": {
      "rss": 123456789,
      "heapTotal": 987654321,
      "heapUsed": 456789123,
      "external": 12345678
    },
    "version": "1.0.0",
    "environment": "development",
    "database": "connected",
    "databaseStats": {
      "userCount": 5
    }
  },
  "timestamp": "2025-09-20T10:30:00.000Z"
}
```

**Response Degraded (503)**:

```json
{
  "success": false,
  "data": {
    "status": "DEGRADED",
    "database": "disconnected",
    "timestamp": "2025-09-20T10:30:00.000Z"
  },
  "timestamp": "2025-09-20T10:30:00.000Z"
}
```

### 2. API Documentation

- **Endpoint**: `GET /api`
- **Authentication**: None

**Response**: Full API documentation with all endpoints and schemas

---

## Authentication & Security

### Bearer Token Format

```
Authorization: Bearer <JWT_ACCESS_TOKEN>
```

### JWT Token Structure

```javascript
// Access Token Payload
{
  "sub": "user_id",
  "username": "string",
  "roles": ["array_of_roles"],
  "department": "string",
  "iat": 1695200000,
  "exp": 1695200900  // 15 minutes from iat
}

// Refresh Token Payload
{
  "sub": "user_id",
  "type": "refresh",
  "iat": 1695200000,
  "exp": 1696409600  // 14 days from iat
}
```

### Role-Based Access Control

- **General User**: Basic read access
- **Academic Staff**: Can upload/modify schedules
- **Head of Department**: Can delete schedules

### CORS Configuration

```javascript
{
  origin: "http://localhost:5173",
  credentials: false,
  methods: ["GET", "POST", "PUT", "DELETE", "OPTIONS"],
  allowedHeaders: ["Content-Type", "Authorization", "X-Requested-With"]
}
```

---

## Error Response Formats

### Standard Success Format

```json
{
  "success": true,
  "data": "response_data",
  "meta": "optional_metadata",
  "timestamp": "ISO_8601_timestamp"
}
```

### Standard Error Format

```json
{
  "success": false,
  "error": {
    "code": "ERROR_CODE",
    "message": "Human readable message",
    "details": "Optional additional details"
  },
  "timestamp": "ISO_8601_timestamp"
}
```

### HTTP Status Codes

- **200**: Success
- **400**: Bad Request (validation errors, missing data)
- **401**: Unauthorized (missing/invalid/expired token)
- **403**: Forbidden (insufficient role permissions)
- **404**: Not Found
- **409**: Conflict (duplicate resources)
- **429**: Too Many Requests (rate limiting)
- **500**: Internal Server Error
- **503**: Service Unavailable (health check degraded)

---

## Rate Limiting

### Auth Endpoints Rate Limiting

- **Applies to**: `POST /auth/login`
- **Limit**: 100 requests per IP per 15 minutes
- **Response**: 429 with rate limit message

### Bulk Operations Rate Limiting

- **Applies to**:
  - `POST /schedules/upload`
  - `DELETE /schedules/all`
  - `POST /schedules/bulk-delete`
- **Limit**: 5 operations per IP per 15 minutes
- **Response**: 429 with rate limit message

### Rate Limit Response Format

```json
{
  "success": false,
  "error": {
    "code": "RATE_LIMIT_EXCEEDED",
    "message": "Too many requests. Please try again later."
  },
  "timestamp": "2025-09-20T10:30:00.000Z"
}
```

---

## WPF Client Comparison

### ✅ IMPLEMENTED & MATCHING

1. **Authentication Flow**: Login, refresh, logout, get profile
2. **Bearer Token Authentication**: Proper JWT handling
3. **Schedule Bulk Upload**: Matches expected format
4. **Health Check**: Basic connectivity test
5. **Error Response Format**: Standardized format
6. **Rate Limiting**: Implemented for bulk operations
7. **Backup & Restore Functionality**: Full implementation with file upload/download

### ✅ NEWLY IMPLEMENTED (September 2025)

1. **Backup & Restore Endpoints**:
   - ✅ `POST /api/backups/upload` - Upload backup files with checksum validation
   - ✅ `GET /api/backups/list` - List user's backup files
   - ✅ `GET /api/backups/download/{filename}` - Download backup files
   - ✅ `DELETE /api/backups/{filename}` - Delete backup files
2. **File Upload Support**:
   - ✅ Multipart/form-data handling with multer
   - ✅ File size validation (100MB limit)
   - ✅ File type validation (.db, .sqlite, .sqlite3)
   - ✅ MD5 checksum validation for data integrity

### ⚠️ DIFFERENCES FROM WPF DOCUMENTATION

1. **Response Format**: Server uses `success/data/timestamp` structure vs simpler format in WPF docs
2. **Authentication Response**: Server includes more user details (department, structured user object)
3. **Refresh Token Response**: Server wraps accessToken in data object
4. **Error Codes**: Server uses specific error codes (MISSING_CREDENTIALS, etc.)
5. **Backup File Storage**: Server uses user-specific directories for better organization

### ✅ WPF REFRESH TOKEN COMPATIBILITY

**The server DOES support refresh tokens correctly:**

- ✅ `POST /auth/refresh` endpoint exists
- ✅ Accepts `refreshToken` in request body
- ✅ Returns new `accessToken` in response
- ✅ Validates refresh token against stored tokens
- ✅ Handles token expiry and revocation
- ✅ Proper error handling for invalid tokens

---

## Configuration Checklist for WPF Client

### Base Configuration

- [ ] **Server URL**: `http://localhost:4000`
- [ ] **Timeout**: 30 seconds (server default)
- [ ] **Content-Type**: `application/json`
- [ ] **User-Agent**: Set appropriate client identifier

### Authentication

- [ ] **Login Endpoint**: `POST /auth/login`
- [ ] **Refresh Endpoint**: `POST /auth/refresh` ✅
- [ ] **Profile Endpoint**: `GET /auth/me`
- [ ] **Bearer Token**: `Authorization: Bearer <token>`
- [ ] **Token Refresh**: Handle 401 responses with refresh flow

### Schedule Management

- [ ] **Upload Endpoint**: `POST /schedules/upload`
- [ ] **Bulk Data Format**: Array of schedule objects
- [ ] **Academic Staff Role**: Required for uploads
- [ ] **Rate Limiting**: Handle 429 responses

### Error Handling

- [ ] **Success Detection**: Check `success: true` field
- [ ] **Error Structure**: Read `error.code` and `error.message`
- [ ] **HTTP Status Codes**: Handle 401, 403, 429, 500
- [ ] **Token Expiry**: Auto-refresh on TOKEN_EXPIRED

### Backup Management ✅ FULLY IMPLEMENTED

- [ ] **Upload Endpoint**: `POST /api/backups/upload` with multipart/form-data
- [ ] **List Endpoint**: `GET /api/backups/list` returning JSON array
- [ ] **Download Endpoint**: `GET /api/backups/download/{filename}` returning binary
- [ ] **Delete Endpoint**: `DELETE /api/backups/{filename}`
- [ ] **Checksum Validation**: MD5 hash verification for file integrity
- [ ] **File Type Validation**: Only .db, .sqlite, .sqlite3 files allowed
- [ ] **User Isolation**: Each user has their own backup directory

### Additional Features (Optional)

- [ ] **Batch Backup Operations**: Multiple file operations
- [ ] **Backup Encryption**: Client-side encryption before upload
- [ ] **Backup Scheduling**: Automated backup creation

---

_This document reflects the current state of the auth-service implementation as of September 20, 2025._
_✅ Backup & Restore functionality added September 19, 2025 - FULLY COMPATIBLE with WPF client expectations._
