# Chức năng Delete File Backup

## Tổng quan

Chức năng này cho phép người dùng xóa các file backup khỏi server, bao gồm cả file backup và metadata tương ứng.

## Kiến trúc hệ thống

### Backend (Node.js/Express)

#### 1. Routes (`/auth-service/src/routes/backups.js`)

```javascript
// DELETE /api/backups/:filename - Delete specific backup
router.delete(
  "/:filename",
  backupOperationLimiter,
  validateFilename,
  backupController.deleteBackup.bind(backupController)
);
```

**Features:**

- Rate limiting: 10 operations per 15 minutes
- Filename validation (ngăn chặn path traversal)
- Authentication required

#### 2. Controller (`/auth-service/src/controllers/backupController.js`)

```javascript
async deleteBackup(req, res) {
  // 1. Lấy userId từ JWT token
  // 2. Xác định đường dẫn file backup
  // 3. Kiểm tra file tồn tại
  // 4. Xóa file backup
  // 5. Xóa metadata file (nếu có)
  // 6. Trả về kết quả
}
```

**Security measures:**

- User isolation: Mỗi user chỉ có thể xóa backup của mình
- File existence check
- Error handling và logging

### Frontend

#### 1. WPF Application (`/SchedulerWpfApp/`)

**Files liên quan:**

- `Views/BackupRestoreWindow.xaml` - UI với button delete
- `ViewModel/BackupRestoreViewModel.cs` - Logic xử lý
- `ServiceRefactor/BackupRestoreService/BackupRestoreService.cs` - API client

**Workflow:**

1. User chọn backup từ danh sách
2. Click nút "🗑️ Xóa Sao lưu"
3. Hiển thị dialog xác nhận
4. Gọi API DELETE
5. Refresh danh sách backup

#### 2. Web Admin Interface (`/auth-admin/`)

**Files:**

- `src/pages/Backups.jsx` - Giao diện quản lý backup
- Tích hợp với `src/App.jsx` qua routing

**Features:**

- Danh sách tất cả backup files
- Stats hiển thị (tổng số file, dung lượng, ngày backup mới nhất)
- Download backup
- Delete backup với confirmation
- Pagination support

## Cấu trúc thư mục backup

```
/backups/
  /users/
    /[userId]/          # Thư mục riêng cho mỗi user
      backup1.db        # File backup
      backup1.db.meta.json  # Metadata (optional)
      backup2.db
      backup2.db.meta.json
```

## API Endpoints

### DELETE /api/backups/:filename

**Request:**

```http
DELETE /api/backups/mybackup.db
Authorization: Bearer <jwt-token>
```

**Response Success (200):**

```json
{
  "success": true,
  "message": "Backup deleted successfully"
}
```

**Response Error (404):**

```json
{
  "success": false,
  "message": "Backup file not found",
  "errorCode": "FILE_NOT_FOUND"
}
```

### GET /api/backups/list

**Response:**

```json
{
  "success": true,
  "data": [
    {
      "filename": "backup_20240926.db",
      "size": 1024000,
      "uploadDate": "2024-09-26T10:30:00Z",
      "checksum": "abc123def456"
    }
  ]
}
```

## Security Features

1. **Authentication:** JWT token required
2. **Authorization:** User chỉ có thể xóa backup của mình
3. **Rate Limiting:** Tối đa 10 operations/15 phút
4. **Input Validation:**
   - Filename không chứa `..`, `/`, `\`
   - Path traversal protection
5. **File Isolation:** Mỗi user có thư mục riêng

## Error Handling

### Backend Errors:

- `FILE_NOT_FOUND`: File backup không tồn tại
- `DELETE_ERROR`: Lỗi khi xóa file
- `INVALID_FILENAME`: Tên file không hợp lệ
- `RATE_LIMIT_EXCEEDED`: Vượt quá giới hạn rate limit

### Frontend Error Handling:

- Network errors
- Authentication errors
- Server errors
- User feedback với notifications

## Testing

### Manual Testing:

1. Upload một backup file
2. Verify file xuất hiện trong danh sách
3. Delete file và confirm
4. Verify file đã bị xóa khỏi danh sách
5. Verify file đã bị xóa khỏi file system

### API Testing:

Sử dụng `test-backup-api.js` để test API endpoints

## Deployment Considerations

1. **File Permissions:** Đảm bảo ứng dụng có quyền write/delete trong thư mục backup
2. **Backup Strategy:** Cân nhắc backup thư mục backup trước khi enable delete
3. **Monitoring:** Log tất cả delete operations
4. **Recovery:** Cân nhắc soft delete hoặc recycle bin mechanism

## Future Enhancements

1. **Soft Delete:** Move files to trash thay vì xóa vĩnh viễn
2. **Bulk Delete:** Xóa nhiều files cùng lúc
3. **Admin Override:** Admin có thể xóa backup của user khác
4. **Audit Trail:** Log chi tiết các thao tác delete
5. **Storage Cleanup:** Tự động dọn dẹp files cũ

## Troubleshooting

### Common Issues:

1. **403 Forbidden:** Kiểm tra JWT token và permissions
2. **404 Not Found:** File có thể đã bị xóa hoặc không tồn tại
3. **500 Server Error:** Kiểm tra file permissions và disk space
4. **Rate Limited:** Đợi 15 phút hoặc reduce request frequency

### Logs to Check:

- Backend console logs
- File system access logs
- Authentication logs
- Network request logs (browser dev tools)
