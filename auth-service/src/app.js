const express = require("express");
const cors = require("cors");
const helmet = require("helmet");
const rateLimit = require("express-rate-limit");
const { loadConfig } = require("./setup");
const authRoutes = require("./routes/auth");
const userRoutes = require("./routes/users");
const departmentRoutes = require("./routes/department");
const subjectRoutes = require("./routes/subject");
const lecturerRoutes = require("./routes/lecturer");
const scheduleRoutes = require("./routes/schedules");
const backupRoutes = require("./routes/backups");

const app = express();
const config = loadConfig();

// Security middleware
app.use(
  helmet({
    crossOriginEmbedderPolicy: false,
    contentSecurityPolicy: {
      directives: {
        defaultSrc: ["'self'"],
        styleSrc: ["'self'", "'unsafe-inline'"],
        scriptSrc: ["'self'"],
        imgSrc: ["'self'", "data:", "https:"],
      },
    },
  })
);

// Body parsing middleware
app.use(express.json({ limit: "50mb" })); // Increased limit for bulk uploads
app.use(express.urlencoded({ extended: true, limit: "50mb" }));

// Request logging middleware
app.use((req, res, next) => {
  const start = Date.now();
  const originalSend = res.send;

  res.send = function (data) {
    const duration = Date.now() - start;
    const logData = {
      method: req.method,
      url: req.url,
      status: res.statusCode,
      duration: `${duration}ms`,
      userAgent: req.get("User-Agent"),
      ip: req.ip || req.connection.remoteAddress,
      timestamp: new Date().toISOString(),
    };

    // Log error responses
    if (res.statusCode >= 400) {
      console.error("[Request Error]", logData);
    } else if (duration > 1000) {
      // Log slow requests (over 1 second)
      console.warn("[Slow Request]", logData);
    }

    originalSend.call(this, data);
  };

  next();
});

// CORS configuration
app.use(
  cors({
    origin: config.corsOrigin,
    credentials: false,
    methods: ["GET", "POST", "PUT", "DELETE", "OPTIONS"],
    allowedHeaders: ["Content-Type", "Authorization", "X-Requested-With"],
  })
);

// Rate limiting for auth endpoints
const authLimiter = rateLimit({
  windowMs: 15 * 60 * 1000, // 15 minutes
  max: 100, // limit each IP to 100 requests per windowMs
  message: {
    success: false,
    error: {
      code: "RATE_LIMIT_EXCEEDED",
      message: "Too many requests. Please try again later.",
    },
    timestamp: new Date().toISOString(),
  },
  standardHeaders: true,
  legacyHeaders: false,
});

app.use("/auth/login", authLimiter);

// Health check endpoint
app.get("/health", async (req, res) => {
  try {
    // Basic health status
    const healthData = {
      status: "OK",
      timestamp: new Date().toISOString(),
      uptime: process.uptime(),
      memory: process.memoryUsage(),
      version: process.env.npm_package_version || "1.0.0",
      environment: process.env.NODE_ENV || "development",
    };

    // Database connectivity check
    try {
      const mongoose = require("mongoose");
      if (mongoose.connection.readyState === 1) {
        healthData.database = "connected";

        // Quick database operation test
        const { User } = require("./models");
        const userCount = await User.estimatedDocumentCount();
        healthData.databaseStats = { userCount };
      } else {
        healthData.database = "disconnected";
        healthData.status = "DEGRADED";
      }
    } catch (dbError) {
      healthData.database = "error";
      healthData.databaseError = dbError.message;
      healthData.status = "DEGRADED";
    }

    const statusCode = healthData.status === "OK" ? 200 : 503;
    res.status(statusCode).json({
      success: healthData.status === "OK",
      data: healthData,
      timestamp: new Date().toISOString(),
    });
  } catch (error) {
    console.error("Health check error:", error);
    res.status(503).json({
      success: false,
      data: {
        status: "ERROR",
        timestamp: new Date().toISOString(),
        error: error.message,
      },
      timestamp: new Date().toISOString(),
    });
  }
});

// API Documentation endpoint
app.get("/api", (req, res) => {
  const apiDocumentation = {
    title: "Scheduler Auth Service API",
    version: "1.0.0",
    description:
      "Authentication and schedule management API for WPF Scheduler application",
    baseUrl: `${req.protocol}://${req.get("host")}`,
    endpoints: {
      authentication: {
        "POST /auth/login": "Authenticate user and get access/refresh tokens",
        "POST /auth/refresh": "Refresh access token using refresh token",
        "GET /auth/me": "Get current authenticated user information",
        "POST /auth/logout": "Logout and revoke refresh token",
      },
      schedules: {
        "GET /schedules": "Query schedules with filtering and pagination",
        "GET /schedules/stats": "Get schedule statistics",
        "GET /schedules/:id": "Get specific schedule by ID",
        "POST /schedules/upload":
          "Bulk upload schedules (requires authentication)",
        "PUT /schedules/:id":
          "Update specific schedule (requires authentication)",
        "DELETE /schedules/:id":
          "Delete specific schedule (requires authentication)",
        "POST /schedules/bulk-delete":
          "Bulk delete schedules (requires authentication)",
        "DELETE /schedules/all":
          "Delete all schedules (requires authentication)",
      },
      users: {
        "GET /users": "Get all users (requires authentication)",
        "POST /users": "Create new user (requires authentication)",
        "PUT /users/:id": "Update user (requires authentication)",
        "DELETE /users/:id": "Delete user (requires authentication)",
      },
      backups: {
        "POST /api/backups/upload":
          "Upload backup file (requires authentication)",
        "GET /api/backups/list":
          "List user's backup files (requires authentication)",
        "GET /api/backups/download/:filename":
          "Download specific backup file (requires authentication)",
        "DELETE /api/backups/:filename":
          "Delete specific backup file (requires authentication)",
      },
      system: {
        "GET /health": "Health check endpoint",
        "GET /api": "This API documentation",
      },
    },
    authentication: {
      type: "Bearer Token",
      header: "Authorization: Bearer <token>",
      description:
        "Use the access token obtained from /auth/login in the Authorization header",
    },
    errorFormat: {
      success: false,
      error: {
        code: "ERROR_CODE",
        message: "Human readable error message",
        details: "Additional error details if available",
      },
      timestamp: "ISO 8601 timestamp",
    },
    successFormat: {
      success: true,
      data: "Response data object or array",
      meta: "Optional metadata (pagination, etc.)",
      timestamp: "ISO 8601 timestamp",
    },
  };

  res.json({
    success: true,
    data: apiDocumentation,
    timestamp: new Date().toISOString(),
  });
});

// API routes
app.use("/auth", authRoutes);
app.use("/users", userRoutes);
app.use("/departments", departmentRoutes);
app.use("/subjects", subjectRoutes);
app.use("/lecturers", lecturerRoutes);
app.use("/schedules", scheduleRoutes); // NEW: Schedule management routes
app.use("/api/backups", backupRoutes); // NEW: Backup/restore functionality

// 404 handler
app.use("*", (req, res) => {
  res.status(404).json({
    success: false,
    error: {
      code: "NOT_FOUND",
      message: "Endpoint not found",
    },
    timestamp: new Date().toISOString(),
  });
});

// Global error handler
app.use((err, req, res, next) => {
  console.error("[Error]", {
    message: err.message,
    stack: err.stack,
    url: req.url,
    method: req.method,
    body: req.body,
    timestamp: new Date().toISOString(),
  });

  // Mongoose validation error
  if (err.name === "ValidationError") {
    return res.status(400).json({
      success: false,
      error: {
        code: "VALIDATION_ERROR",
        message: "Database validation failed",
        details: err.message,
      },
      timestamp: new Date().toISOString(),
    });
  }

  // Mongoose duplicate key error
  if (err.code === 11000) {
    return res.status(409).json({
      success: false,
      error: {
        code: "DUPLICATE_ERROR",
        message: "Resource already exists",
        details: err.message,
      },
      timestamp: new Date().toISOString(),
    });
  }

  // JWT errors
  if (err.name === "JsonWebTokenError") {
    return res.status(401).json({
      success: false,
      error: {
        code: "INVALID_TOKEN",
        message: "Invalid authentication token",
      },
      timestamp: new Date().toISOString(),
    });
  }

  // Default error response
  res.status(err.status || 500).json({
    success: false,
    error: {
      code: "INTERNAL_ERROR",
      message: err.message || "Internal server error",
    },
    timestamp: new Date().toISOString(),
  });
});

module.exports = app;
