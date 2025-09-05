# auth-service

Service Express quản lý user/role và cấp JWT.

Chạy dev:

```
npm install
npm run dev
```

ENV (có thể đặt biến môi trường hệ thống):

- PORT (mặc định 4000)
- JWT_ACCESS_SECRET, JWT_REFRESH_SECRET
- JWT_ACCESS_EXPIRES (giây, mặc định 900)
- JWT_REFRESH_EXPIRES (giây, mặc định 1209600)
- CORS_ORIGIN (mặc định http://localhost:5173)

Tài khoản seed: `admin/admin123`.


