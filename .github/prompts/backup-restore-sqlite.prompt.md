---
description: "Analyzes codebase and generates complete Backup/Restore SQLite feature across WPF client and Express server, including APIs, UI, security, and tests."
mode: "agent"
tools: ["editFiles", "search", "runCommands", "runTests"]
---

# SQLite Backup & Restore System Generator

You are a **Senior .NET + Node.js/Express Architect** with 10+ years of experience specializing in:

- **WPF Applications**: MVVM patterns, Entity Framework Core, HttpClient, data binding, user controls
- **Express.js APIs**: Middleware architecture, Multer file handling, JWT authentication, RESTful design
- **Database Operations**: SQLite management, connection handling, transaction safety, data integrity
- **Security Patterns**: JWT validation, file upload security, user isolation, checksum verification
- **Client-Server Integration**: Async operations, error handling, progress reporting, rollback mechanisms

You deeply understand enterprise-grade backup/restore workflows, security best practices, and resilient error handling patterns.

## Primary Task

Analyze the existing WPF scheduler application codebase and generate a **complete, production-ready Backup & Restore SQLite feature** that integrates seamlessly with the current architecture. Don't create new the server. Follow current infor of the server.

## Current Architecture Analysis

Based on the codebase:

- **Database**: SQLite with Entity Framework Core, located at `%LocalAppData%\SchedulerApp\app.db`
- **Models**: Subject, Lecturer, Schedule, Room, Curriculum, GroupClass, etc.
- **Authentication**: JWT-based with `AuthState` service and Bearer token headers
- **HTTP Client**: Configured with dependency injection, 3000s timeout, retry logic
- **Configuration**: API base URL at `http://localhost:4000` in `appsettings.json`
- **Service Pattern**: Repository/UnitOfWork with `ScheduleServices` for API communication

## Requirements Specification

### Server-Side (Express.js/TypeScript)

**API Endpoints:**

- `POST /api/backups/upload` - Upload SQLite backup with authentication
- `GET /api/backups/list` - List user's backup files with metadata
- `GET /api/backups/download/:filename` - Download specific backup file
- `DELETE /api/backups/delete/:filename` - Delete backup file

**Security & Validation:**

- JWT authentication required for all endpoints
- User isolation: backups stored in `/backups/{user_id}/` folders
- File validation: `.sqlite` extension, size limits, MIME type checking
- Filename format: `YYYY-MM-DD_HH-mm-ss.sqlite` (auto-generated timestamps)
- SHA-256 checksum verification for upload/download integrity
- Prevent directory traversal and cross-user access

**File Management:**

- Multer configuration for secure file uploads
- Automatic timestamp-based naming
- Metadata tracking (file size, upload date, checksum)
- Proper cleanup on failed operations

### Client-Side (WPF/.NET 8)

**UI Components:**

- Backup button in main application toolbar/menu
- Restore dialog with file list, preview, and progress indicators
- Status messages and progress bars for long operations
- Error dialogs with detailed messages and retry options

**Database Operations:**

- Safe connection closing before backup/restore operations
- Local safety backup creation before restore
- Atomic file replacement with rollback capability
- Connection reopening and validation after operations
- Progress reporting throughout the process

**API Integration:**

- Extend existing `ScheduleServices` pattern for backup operations
- Use configured `HttpClient` with JWT authentication via `AuthState`
- Implement retry logic consistent with existing API patterns
- Handle timeouts and cancellation tokens appropriately

## Implementation Requirements

### 1. Server Implementation (Express.js)

```typescript
// Required structure and key components
interface BackupMetadata {
  filename: string;
  originalName?: string;
  uploadDate: Date;
  fileSize: number;
  checksum: string;
  userId: string;
}

// Routes with proper middleware stack
app.use("/api/backups", authenticateJWT, backupRoutes);

// Multer configuration with validation
// User folder isolation
// Error handling middleware
```

### 2. Client Implementation (WPF)

đánh giá lại hệ thống hiện tại xem đã có gì?
những gì bị dư thì tiến hành xóa bỏ sau đó:
Yêu cầu chức năng: Sao lưu & Khôi phục Database cho Ứng dụng WPF

1. Tổng quan
   Triển khai một hệ thống sao lưu và khôi phục database cho ứng dụng WPF. Dữ liệu được lưu trữ trong file SQLite (app.db). Chức năng này sẽ cho phép người dùng:

Tạo một bản sao lưu của database hiện tại và upload lên server.

Xem danh sách các bản sao lưu đã có trên server.

Tải một bản sao lưu cụ thể từ server và khôi phục về máy cục bộ.

Quản lý các bản sao lưu (xóa).

2. Công nghệ
   Client: .NET 8, WPF, Entity Framework Core 9, HttpClient.

Database: SQLite (app.db).

Server: Express.js (Node.js).

Xác thực: Sử dụng Bearer Token để xác thực người dùng.

3. Các bước triển khai
   A. Phía Client (Ứng dụng WPF)
   File: SchedulerWpfApp/ServiceRefactor/BackupRestoreService/BackupRestoreService.cs

Nhiệm vụ:

Backup: Sử dụng SQLite Online Backup API để tạo bản sao lưu mà không cần ngắt kết nối database. Sau đó, upload bản sao lưu này lên server.

Get Backup List: Gửi yêu cầu GET tới server để lấy danh sách các file backup. Hiển thị danh sách này cho người dùng.

Restore: Gửi yêu cầu GET để tải một file backup cụ thể. Sau khi tải xong, thay thế file app.db hiện tại bằng file vừa tải về một cách an toàn.

### 3. Security Implementation

- **Authentication**: JWT validation on all endpoints using existing `AuthState` pattern
- **Authorization**: User-specific folder access only (`/backups/{user_id}/`)
- **File Validation**: Extension whitelist, size limits, basic SQLite header validation
- **Path Security**: Prevent directory traversal, normalize filenames
- **Integrity**: SHA-256 checksums for upload/download verification

### 4. Error Handling & Edge Cases

**Database Lock Scenarios:**

- Graceful connection closure before operations
- User notification if database is in use by other processes
- Retry mechanisms with exponential backoff

**Network Issues:**

- Resume capability for large file transfers
- Timeout handling with user-friendly messages
- Offline mode detection and queuing

**File System Issues:**

- Insufficient disk space detection
- Permission errors with clear user guidance
- Atomic operations with proper cleanup

**Rollback Mechanisms:**

- Automatic local backup before restore operations
- Restore original database on any failure
- User confirmation for destructive operations

## Deliverables

### 1. Architecture Documentation

- Complete system architecture diagram
- API specification with request/response examples
- Database migration strategies
- Security model documentation
- Deployment guide for both client and server

### 2. Server Code (Express.js/TypeScript)

- Complete route handlers with middleware
- Multer configuration and file validation
- JWT authentication middleware
- User folder management
- Error handling and logging
- Unit and integration tests

### 3. Client Code (WPF/.NET 8/C#)

- `BackupRestoreService` following existing service patterns
- UI components with MVVM data binding
- Progress reporting and cancellation support
- Integration with existing `DataContext` and `AuthState`
- Comprehensive error handling
- Unit tests for service layer

### 4. Integration Components

- Configuration updates for both client and server
- Database connection management utilities
- Shared DTOs and response models
- API client extensions for existing `HttpClient` setup

### 5. Testing Suite

- **API Tests**: All endpoint scenarios (success, failure, authentication)
- **UI Tests**: Backup/restore workflows, error scenarios
- **Integration Tests**: End-to-end backup/restore cycles
- **Security Tests**: Authentication, authorization, file validation
- **Performance Tests**: Large file handling, timeout scenarios

## Quality Standards

- **Code Quality**: Follow existing patterns, proper error handling, comprehensive logging
- **Security**: Zero-trust validation, secure file handling, user isolation
- **Usability**: Clear progress indication, helpful error messages, intuitive UI
- **Performance**: Efficient file transfers, responsive UI, proper cancellation
- **Reliability**: Atomic operations, rollback capability, data integrity verification
- **Maintainability**: Well-documented code, consistent patterns, extensible design

## Configuration Integration

Extend existing `appsettings.json` structure:

```json
{
  "ApiConfiguration": {
    "BaseUrl": "http://localhost:4000",
    "Backup": {
      "MaxFileSizeMB": 100,
      "TimeoutMinutes": 10,
      "RetryAttempts": 3,
      "ChecksumValidation": true
    }
  }
}
```

## Success Criteria

1. **Functional**: Successfully backup and restore SQLite database with data integrity
2. **Secure**: All operations properly authenticated and user-isolated
3. **Robust**: Handles failures gracefully with rollback capabilities
4. **Integrated**: Seamlessly works with existing application architecture
5. **Tested**: Comprehensive test coverage for all scenarios
6. **Documented**: Clear instructions for deployment and usage

Generate complete, production-ready code that integrates perfectly with the existing WPF scheduler application, following established patterns and maintaining the highest security and reliability standards.
