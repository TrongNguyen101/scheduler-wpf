# Scheduler Auth Service

A robust Express.js server with MongoDB integration designed to serve as the backend for WPF Scheduler applications. This service provides comprehensive authentication, schedule management, and data processing capabilities with enterprise-grade security and performance optimizations.

## 🚀 Features

### Authentication & Authorization

- **JWT-based authentication** with access/refresh token pattern
- **Role-based access control** (Admin, HeadOfDepartment, AcademicStaff)
- **Secure password hashing** using bcrypt with configurable rounds
- **Token refresh mechanism** for seamless user experience
- **Rate limiting** to prevent brute force attacks

### Schedule Management

- **Bulk upload support** for 8000+ schedule records with transaction safety
- **Advanced filtering and pagination** for efficient data retrieval
- **CRUD operations** with comprehensive validation
- **Duplicate detection** and conflict resolution
- **Performance optimized** for large datasets

### Security & Performance

- **Input validation** using Joi schemas
- **SQL injection protection** through parameterized queries
- **Request rate limiting** and payload size restrictions
- **CORS configuration** for cross-origin requests
- **Comprehensive error handling** with standardized responses
- **Request logging** and performance monitoring

### Database Features

- **MongoDB integration** with Mongoose ODM
- **Optimized indexes** for query performance
- **Transaction support** for data consistency
- **Connection pooling** and automatic reconnection
- **Data migration utilities** for existing JSON data

## 📋 Prerequisites

- **Node.js 18+**
- **MongoDB** (local or cloud instance)
- **npm or yarn** package manager

## 🛠️ Installation

1. **Navigate to the project directory:**

```bash
cd auth-service
```

2. **Install dependencies:**

```bash
npm install
```

3. **Configure environment variables:**

```bash
cp .env.example .env
# Edit .env with your configuration
```

4. **Start the server:**

```bash
# Development mode with auto-reload
npm run dev

# Production mode
npm start
```

## ⚙️ Configuration

### Environment Variables

Create a `.env` file based on `.env.example`:

```env
# Server Configuration
PORT=4000
NODE_ENV=development

# Database Configuration
MONGO_URI=mongodb+srv://username:password@cluster.mongodb.net/scheduler-db

# JWT Configuration (CHANGE THESE IN PRODUCTION!)
JWT_ACCESS_SECRET=your-very-secret-access-key-here
JWT_REFRESH_SECRET=your-very-secret-refresh-key-here
JWT_ACCESS_EXPIRES=900          # 15 minutes
JWT_REFRESH_EXPIRES=1209600     # 14 days

# CORS Configuration
CORS_ORIGIN=http://localhost:5173
```

## 📚 API Documentation

Access the complete API documentation at: `GET /api`

### Key Endpoints

- **Authentication:**

  - `POST /auth/login` - User authentication
  - `POST /auth/refresh` - Token refresh
  - `GET /auth/me` - User information
  - `POST /auth/logout` - Logout

- **Schedule Management:**

  - `POST /schedules/upload` - Bulk upload (up to 10,000 records)
  - `GET /schedules` - Query with filtering and pagination
  - `GET /schedules/stats` - Schedule statistics
  - `PUT /schedules/:id` - Update schedule
  - `DELETE /schedules/:id` - Delete schedule

- **System:**
  - `GET /health` - Health check with database status
  - `GET /api` - API documentation

## 🧪 Testing

Run the comprehensive integration test suite:

```bash
# Start the server
npm run dev

# In another terminal, run tests
node test-server.js
```

## 🚀 Production Deployment

### Security Checklist

- [ ] Change default JWT secrets
- [ ] Configure production MongoDB credentials
- [ ] Set up HTTPS with SSL certificates
- [ ] Configure firewall rules
- [ ] Enable monitoring and logging
- [ ] Set up automated backups

### Performance Optimization

For high-traffic scenarios:

- Enable MongoDB clustering and read replicas
- Implement Redis for caching
- Set up load balancing
- Monitor with APM tools

## 🛡️ Security Features

- JWT tokens with short expiration times
- bcrypt password hashing with configurable rounds
- Rate limiting for authentication endpoints
- Input validation and sanitization
- CORS protection
- Security headers via Helmet.js

## 📊 Monitoring

The server includes built-in monitoring:

- Request logging with performance tracking
- Health check endpoint with database status
- Error tracking and reporting
- Memory and uptime monitoring

## 🔧 Development

### Project Structure

```
auth-service/
├── src/
│   ├── app.js              # Express configuration
│   ├── server.js           # Server startup
│   ├── db.js               # Database connection
│   ├── models.js           # Data models
│   ├── setup.js            # Configuration
│   ├── controllers/        # Business logic
│   ├── middleware/         # Auth & validation
│   └── routes/             # API endpoints
├── test-server.js          # Integration tests
└── .env.example            # Environment template
```

## 🆘 Troubleshooting

### Common Issues

**MongoDB Connection Failed:**

- Verify `MONGO_URI` in environment variables
- Check MongoDB server status
- Verify network connectivity

**Authentication Issues:**

- Ensure JWT secrets are properly configured
- Check token expiration settings
- Verify CORS configuration

**Performance Problems:**

- Monitor database indexes
- Check bulk operation sizes
- Review server resource usage

## 📄 API Response Format

All API responses follow a consistent format:

```json
{
  "success": true|false,
  "data": "response data",
  "error": {
    "code": "ERROR_CODE",
    "message": "Error description"
  },
  "meta": "optional metadata",
  "timestamp": "2024-01-15T10:30:00.000Z"
}
```

---

**Status:** ✅ Production Ready  
**Compatibility:** WPF Scheduler Client v1.0+  
**Last Updated:** January 2024
JWT_REFRESH_SECRET=your-refresh-secret-here

# JWT Token Expiration (in seconds)

JWT_ACCESS_EXPIRES=900 # 15 minutes
JWT_REFRESH_EXPIRES=1209600 # 14 days

# CORS Configuration

CORS_ORIGIN=http://localhost:5173

# Environment

NODE_ENV=development

````

### Database Setup

The server will automatically:

- Connect to MongoDB
- Create necessary collections and indexes
- Seed initial roles and admin user

### Running the Server

Development mode:

```bash
npm run dev
````

Production mode:

```bash
npm start
```

Seed database only:

```bash
npm run seed
```

## API Documentation

### Authentication Endpoints

#### POST /auth/login

Login with username/password

```json
{
  "username": "admin",
  "password": "admin123"
}
```

#### POST /auth/refresh

Refresh access token

```json
{
  "refreshToken": "your-refresh-token"
}
```

#### GET /auth/me

Get current user info (requires Bearer token)

#### POST /auth/logout

Logout and revoke refresh token

### Schedule Management Endpoints (NEW)

#### POST /schedules/upload

**Bulk upload schedules** (requires AcademicOfDepartment role or higher)

Request body:

```json
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
      "lecturerAccount": "nguyenvana"
    }
  ],
  "overwriteExisting": false,
  "validateOnly": false
}
```

#### GET /schedules

**Query schedules with filtering**

Query parameters:

- `page` (default: 1)
- `limit` (default: 50, max: 1000)
- `lecturerId`
- `subjectCode`
- `groupName`
- `roomId`
- `major`
- `statusSlot`
- `startDate` (ISO date)
- `endDate` (ISO date)
- `sortBy` (date, lecturerName, groupName, subjectCode, roomName)
- `sortOrder` (asc, desc)

#### GET /schedules/:id

**Get specific schedule** by ID or scheduleId

#### PUT /schedules/:id

**Update schedule** (requires AcademicOfDepartment role or higher)

#### DELETE /schedules/:id

**Delete schedule** (requires HeadOfDepartment role or higher)

#### GET /schedules/stats

**Get schedule statistics**

#### POST /schedules/bulk-delete

**Bulk delete schedules** (requires HeadOfDepartment role or higher)

### User Management Endpoints

#### GET /users

List users with filtering (Admin only)

#### POST /users

Create new user (Admin only)

#### PUT /users/:id

Update user (Admin only)

#### DELETE /users/:id

Delete user (Admin only)

#### POST /users/import

Import users from Excel (Admin only)

#### GET /users/import/template

Download Excel template (Admin only)

### Department & Lecturer Endpoints

#### GET /departments

Get lecturer assignments (HeadOfDepartment only)

#### POST /departments

Create lecturer assignments (HeadOfDepartment only)

#### PUT /departments

Update lecturer assignments (HeadOfDepartment only)

#### DELETE /departments/:lecturerId

Delete lecturer assignments (HeadOfDepartment only)

#### GET /lecturers

List all lecturers

#### GET /subjects

List all subjects

## Data Models

### Schedule Schema

Based on WPF ScheduleUploadDto with optimized indexes:

```javascript
{
  scheduleId: String,     // Unique identifier
  groupName: String,      // Class/group name
  subjectCode: String,    // Subject code
  date: Date,            // Schedule date
  slotTime: String,      // Time slot (e.g., "07:30-09:00")
  roomName: String,      // Room name
  sessionNo: Number,     // Session number
  lecturerName: String,  // Lecturer name
  slotTypeCode: String,  // Type of slot (THEORY, LAB, etc.)
  statusSlot: String,    // Status (ACTIVE, CANCELLED, etc.)
  typeSlot: String,      // Type (NORMAL, MAKEUP, etc.)
  roomId: String,        // Room identifier
  partOfDay: String,     // MORNING, AFTERNOON, EVENING
  major: String,         // Major/department
  lecturerId: String,    // Lecturer identifier
  lecturerAccount: String, // Optional lecturer account
  uploadedBy: String,    // User who uploaded
  uploadBatch: String,   // Batch identifier for bulk uploads
  createdAt: Date,       // Auto-generated
  updatedAt: Date        // Auto-generated
}
```

### User Schema

```javascript
{
  id: String,            // Unique user ID
  username: String,      // Login username
  passwordHash: String,  // Bcrypt hashed password
  isActive: Boolean,     // Account status
  createdAt: Date,
  updatedAt: Date
}
```

## Security Features

- **Helmet.js** for security headers
- **Rate limiting** for authentication and bulk operations
- **CORS** configuration
- **Input validation** with Joi schemas
- **SQL injection prevention** through Mongoose ODM
- **Password hashing** with bcrypt (10 rounds)
- **JWT token expiration** and refresh mechanism

## Performance Optimizations

- **Database indexes** for common query patterns
- **Bulk operations** with MongoDB transactions
- **Pagination** for large datasets
- **Connection pooling** through Mongoose
- **Request size limits** (50MB for bulk uploads)
- **Memory usage monitoring** in health check

## Error Handling

Standardized error response format:

```json
{
  "success": false,
  "error": {
    "code": "ERROR_CODE",
    "message": "Human readable message",
    "details": "Additional details if available"
  },
  "timestamp": "2024-09-20T10:30:00.000Z"
}
```

## API Response Format

Success response:

```json
{
  "success": true,
  "data": { ... },
  "meta": {
    "pagination": { ... },
    "message": "Optional message"
  },
  "timestamp": "2024-09-20T10:30:00.000Z"
}
```

## Default Credentials

- **Username**: `admin`
- **Password**: `admin123`
- **Roles**: Admin (full access)

⚠️ **Change default credentials in production!**

## Health Check

GET `/health` provides server status:

```json
{
  "ok": true,
  "timestamp": "2024-09-20T10:30:00.000Z",
  "uptime": 3600,
  "memory": { ... },
  "version": "1.0.0"
}
```

## Rate Limits

- **Authentication**: 100 requests per 15 minutes per IP
- **Bulk operations**: 5 requests per 15 minutes per IP
- **General API**: No specific limits (relies on server capacity)

## Integration with WPF Client

This server is designed to integrate seamlessly with WPF scheduler applications:

1. **Authentication**: Compatible with WPF JWT token handling
2. **Data Format**: Matches ScheduleUploadDto from WPF client
3. **Bulk Operations**: Optimized for large schedule uploads (~8000+ records)
4. **Error Messages**: Standardized format for client error handling
5. **Vietnamese Support**: Error messages support Vietnamese localization

## Development

### Project Structure

```
src/
├── controllers/           # Business logic
│   └── scheduleController.js
├── middleware/           # Authentication & validation
│   ├── auth.js
│   └── validation.js
├── models.js            # MongoDB schemas
├── routes/              # API endpoints
│   ├── auth.js
│   ├── schedules.js     # NEW: Schedule routes
│   ├── users.js
│   ├── departments.js
│   ├── lecturers.js
│   └── subjects.js
├── app.js              # Express app setup
├── server.js           # Server startup
├── db.js               # Database connection
└── setup.js            # Database seeding
```

### Adding New Features

1. Create model in `models.js`
2. Add validation schemas in `middleware/validation.js`
3. Implement business logic in `controllers/`
4. Define routes in `routes/`
5. Register routes in `app.js`
6. Update README documentation

## Deployment

### Environment Variables

Ensure all production environment variables are set:

- `MONGO_URI`: Production MongoDB connection string
- `JWT_ACCESS_SECRET`: Strong random secret
- `JWT_REFRESH_SECRET`: Different strong random secret
- `CORS_ORIGIN`: Production frontend URL
- `NODE_ENV=production`

### Security Checklist

- [ ] Change default admin credentials
- [ ] Use strong JWT secrets
- [ ] Configure proper CORS origins
- [ ] Set up HTTPS in production
- [ ] Enable MongoDB authentication
- [ ] Review rate limits for production load
- [ ] Set up monitoring and logging

## License

This project is private and proprietary.
