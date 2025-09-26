# API Configuration Summary

## Server Configuration (auth-service)

- **Port**: 4000
- **Base URL**: http://localhost:4000
- **CORS Origin**: http://localhost:5173 (allows frontend access)

## Frontend Configuration (auth-admin)

- **Port**: 5173 (Vite dev server)
- **API Base**: http://localhost:4000 (configured via VITE_API_BASE)

## Available API Endpoints

### Authentication (/auth)

- `POST /auth/login` - User login
- `POST /auth/refresh` - Refresh access token
- `GET /auth/me` - Get current user info
- `POST /auth/logout` - User logout

### Schedules (/schedules)

- `GET /schedules` - Get schedules with filters/pagination
- `GET /schedules/stats` - Get schedule statistics
- `GET /schedules/:id` - Get specific schedule
- `POST /schedules/upload` - Bulk upload schedules
- `PUT /schedules/:id` - Update schedule
- `DELETE /schedules/:id` - Delete specific schedule
- `DELETE /schedules/all` - Delete ALL schedules
- `POST /schedules/bulk-delete` - Bulk delete schedules

### Users (/users)

- `GET /users` - Get all users
- `POST /users` - Create new user
- `PUT /users/:id` - Update user
- `DELETE /users/:id` - Delete user
- `POST /users/import` - Import users from Excel
- `GET /users/import/template` - Download Excel template

### Departments (/departments)

- `GET /departments` - Get departments/lecturers
- `POST /departments` - Create/assign lecturer
- `PUT /departments` - Update lecturer assignment
- `DELETE /departments/:lecturerId` - Remove lecturer
- `GET /departments/export` - Export to Excel

### Backups (/api/backups)

- `POST /api/backups/upload` - Upload backup file
- `GET /api/backups/list` - List user's backups
- `GET /api/backups/download/:filename` - Download backup
- `DELETE /api/backups/:filename` - Delete backup
- `GET /api/backups` - Alias for list

### Other Endpoints

- `GET /lecturers` - Get all lecturers
- `GET /subjects` - Get all subjects
- `GET /health` - Health check
- `GET /api` - API documentation

## Authentication

- **Type**: Bearer Token
- **Token Storage**: localStorage with key `aa_accessToken`
- **Refresh Token**: Automatic refresh when access token expires

## Fixed Configuration Issues

### 1. Token Storage Consistency

**Problem**: Different token keys used across files
**Solution**: Standardized to `aa_accessToken` everywhere

### 2. Environment Variables

**Problem**: Mixed use of VITE_API_BASE and VITE_API_URL
**Solution**: Standardized to `VITE_API_BASE`

### 3. Hardcoded URLs

**Problem**: Multiple files had hardcoded `http://localhost:4000`
**Solution**: Created centralized `apiConfig.js` with all endpoints

### 4. API Call Consistency

**Problem**: Mixed use of fetch, axios, and custom helpers
**Solution**: Standardized API calls through centralized configuration

## Usage Examples

### Frontend API Calls

```javascript
import { API_CONFIG, apiCall } from "../lib/apiConfig.js";

// Get schedules
const schedules = await apiCall(API_CONFIG.ENDPOINTS.SCHEDULES.BASE);

// Upload backup
const formData = new FormData();
formData.append("file", file);
const result = await fetch(buildUrl(API_CONFIG.ENDPOINTS.BACKUPS.UPLOAD), {
  method: "POST",
  headers: { Authorization: `Bearer ${getAuthToken()}` },
  body: formData,
});
```

### Testing API Endpoints

```bash
# Health check
curl http://localhost:4000/health

# Login
curl -X POST http://localhost:4000/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username": "admin", "password": "admin123"}'

# Get schedules (with auth)
curl http://localhost:4000/schedules \
  -H "Authorization: Bearer YOUR_TOKEN"
```

## Development Setup

1. **Start auth-service**:

   ```bash
   cd auth-service
   npm run dev
   ```

   Server runs on: http://localhost:4000

2. **Start auth-admin**:

   ```bash
   cd auth-admin
   npm run dev
   ```

   Frontend runs on: http://localhost:5173

3. **Environment Configuration**:
   - Copy `.env.example` to `.env` in auth-admin folder
   - Adjust `VITE_API_BASE` if server runs on different port

## Notes

- Server configuration is NOT changed (as requested)
- Only frontend configuration was updated for consistency
- All hardcoded URLs replaced with centralized configuration
- Token handling standardized across all components
