# Hướng dẫn kiểm tra và sửa lỗi đăng nhập

## Vấn đề đã được sửa:

1. **Cấu trúc response từ server**: Server trả về data trong `res.data.data` nhưng frontend đọc `res.data`
2. **Error handling**: Cải thiện xử lý lỗi và logging
3. **Token storage**: Đảm bảo tokens được lưu đúng cách
4. **Debug logging**: Thêm console.log để debug

## Cách test:

### 1. Khởi động server:

```bash
cd auth-service
npm start
```

### 2. Khởi động frontend:

```bash
cd auth-admin
npm run dev
```

### 3. Test đăng nhập:

- Mở browser tại http://localhost:5173
- Mở Developer Tools (F12) để xem console logs
- Đăng nhập với:
  - Username: `admin`
  - Password: `admin123`

### 4. Kiểm tra logs:

- `Raw login response:` - Cấu trúc response từ server
- `Login response:` - Data được xử lý
- `User data:` - Thông tin user
- `User roles:` - Roles của user
- `PrivateRoute` logs - Kiểm tra routing và permissions

## Tài khoản test mặc định:

| Username  | Password | Roles                | Page redirect |
| --------- | -------- | -------------------- | ------------- |
| admin     | admin123 | Admin                | /users        |
| head_dept | password | HeadOfDepartment     | /lecturers    |
| academic  | password | AcademicOfDepartment | /lecturers    |

## Các lỗi có thể gặp:

1. **"Đăng nhập thất bại"**: Kiểm tra server có chạy không
2. **"Loading..." vô hạn**: Kiểm tra token format và user data
3. **Redirect không đúng**: Kiểm tra user roles trong console
4. **"Unauthorized"**: User không có quyền truy cập page đó

## Files đã sửa:

- `src/lib/auth.js`: Sửa cấu trúc response và error handling
- `src/pages/Login.jsx`: Thêm debug logs và cải thiện error handling
- `src/App.jsx`: Thêm debug logs cho PrivateRoute
- `test-login.js`: File test API login độc lập
