# Server Implementation Guide

## 🎯 Overview

This document provides comprehensive instructions for implementing the Express.js backup server that works with the WPF Scheduler application's backup/restore functionality.

## 📋 Prerequisites

- Node.js 18+
- npm or yarn
- Basic understanding of Express.js
- JWT authentication knowledge

## 🏗️ Project Structure

```
backup-server/
├── package.json
├── server.js                   # Main server entry point
├── middleware/
│   ├── auth.js                 # JWT authentication middleware
│   ├── upload.js               # File upload configuration
│   └── validation.js           # Request validation
├── routes/
│   └── backups.js              # Backup API routes
├── controllers/
│   └── backupController.js     # Business logic
├── utils/
│   ├── fileValidator.js        # SQLite file validation
│   └── logger.js               # Logging utility
├── config/
│   └── config.js               # Server configuration
├── uploads/                    # Temporary upload directory
└── backups/                    # Backup storage directory
    └── users/                  # User-specific backup folders
        └── {userId}/           # Individual user backups
```

## 🚀 Step 1: Initialize Project

```bash
mkdir backup-server
cd backup-server
npm init -y

# Install dependencies
npm install express cors helmet morgan multer jsonwebtoken bcryptjs
npm install --save-dev nodemon jest supertest
```

## 📦 Step 2: Package.json Configuration

```json
{
  "name": "scheduler-backup-server",
  "version": "1.0.0",
  "description": "Backup server for Scheduler WPF application",
  "main": "server.js",
  "scripts": {
    "start": "node server.js",
    "dev": "nodemon server.js",
    "test": "jest",
    "test:watch": "jest --watch"
  },
  "dependencies": {
    "express": "^4.18.2",
    "cors": "^2.8.5",
    "helmet": "^7.0.0",
    "morgan": "^1.10.0",
    "multer": "^1.4.5",
    "jsonwebtoken": "^9.0.0",
    "bcryptjs": "^2.4.3"
  },
  "devDependencies": {
    "nodemon": "^3.0.0",
    "jest": "^29.0.0",
    "supertest": "^6.3.0"
  }
}
```

## ⚙️ Step 3: Main Server Configuration (server.js)

```javascript
const express = require("express");
const cors = require("cors");
const helmet = require("helmet");
const morgan = require("morgan");
const path = require("path");
const fs = require("fs");

const backupRoutes = require("./routes/backups");
const config = require("./config/config");
const logger = require("./utils/logger");

const app = express();

// Security middleware
app.use(helmet());
app.use(
  cors({
    origin: config.cors.allowedOrigins,
    credentials: true,
  })
);

// Logging
app.use(
  morgan("combined", {
    stream: { write: (message) => logger.info(message.trim()) },
  })
);

// Body parsing
app.use(express.json({ limit: "50mb" }));
app.use(express.urlencoded({ extended: true, limit: "50mb" }));

// Create required directories
const requiredDirs = ["./uploads", "./backups", "./backups/users", "./logs"];

requiredDirs.forEach((dir) => {
  if (!fs.existsSync(dir)) {
    fs.mkdirSync(dir, { recursive: true });
    logger.info(`Created directory: ${dir}`);
  }
});

// Routes
app.use("/api/backups", backupRoutes);

// Health check
app.get("/health", (req, res) => {
  res.json({
    status: "OK",
    timestamp: new Date().toISOString(),
    version: "1.0.0",
  });
});

// Error handling
app.use((error, req, res, next) => {
  logger.error(`Error: ${error.message}`, {
    stack: error.stack,
    url: req.url,
    method: req.method,
  });

  res.status(error.status || 500).json({
    error: {
      message: error.message,
      ...(process.env.NODE_ENV === "development" && { stack: error.stack }),
    },
  });
});

// 404 handler
app.use("*", (req, res) => {
  res.status(404).json({ error: "Route not found" });
});

const PORT = config.server.port || 4000;
app.listen(PORT, () => {
  logger.info(`Backup server running on port ${PORT}`);
  console.log(`🚀 Server running at http://localhost:${PORT}`);
});

module.exports = app;
```

## 🔐 Step 4: Authentication Middleware (middleware/auth.js)

```javascript
const jwt = require("jsonwebtoken");
const config = require("../config/config");
const logger = require("../utils/logger");

const authMiddleware = (req, res, next) => {
  try {
    const authHeader = req.headers.authorization;

    if (!authHeader || !authHeader.startsWith("Bearer ")) {
      return res.status(401).json({
        error: "Authentication token required",
      });
    }

    const token = authHeader.substring(7);

    try {
      const decoded = jwt.verify(token, config.jwt.secret);
      req.user = {
        id: decoded.sub || decoded.userId,
        username: decoded.username || decoded.name,
        email: decoded.email,
      };

      logger.info(`Authenticated user: ${req.user.username} (${req.user.id})`);
      next();
    } catch (jwtError) {
      logger.warn(`JWT verification failed: ${jwtError.message}`);
      return res.status(401).json({
        error: "Invalid or expired token",
      });
    }
  } catch (error) {
    logger.error(`Auth middleware error: ${error.message}`);
    return res.status(500).json({
      error: "Authentication service unavailable",
    });
  }
};

module.exports = authMiddleware;
```

## 📁 Step 5: File Upload Configuration (middleware/upload.js)

```javascript
const multer = require("multer");
const path = require("path");
const fs = require("fs");
const config = require("../config/config");

// Configure storage
const storage = multer.diskStorage({
  destination: (req, file, cb) => {
    const uploadDir = path.join("./uploads", req.user.id.toString());

    // Ensure user upload directory exists
    if (!fs.existsSync(uploadDir)) {
      fs.mkdirSync(uploadDir, { recursive: true });
    }

    cb(null, uploadDir);
  },
  filename: (req, file, cb) => {
    // Generate unique filename with timestamp
    const timestamp = new Date().toISOString().replace(/[:.]/g, "-");
    const filename = `backup_${timestamp}.db`;
    cb(null, filename);
  },
});

// File filter for SQLite files
const fileFilter = (req, file, cb) => {
  const allowedMimes = ["application/x-sqlite3", "application/octet-stream"];
  const isValidExtension = file.originalname.toLowerCase().endsWith(".db");

  if (allowedMimes.includes(file.mimetype) || isValidExtension) {
    cb(null, true);
  } else {
    cb(new Error("Only SQLite database files (.db) are allowed"), false);
  }
};

const upload = multer({
  storage: storage,
  fileFilter: fileFilter,
  limits: {
    fileSize: config.upload.maxFileSize || 100 * 1024 * 1024, // 100MB default
    files: 1,
  },
});

module.exports = upload;
```

## 🛣️ Step 6: Backup Routes (routes/backups.js)

```javascript
const express = require("express");
const router = express.Router();
const authMiddleware = require("../middleware/auth");
const upload = require("../middleware/upload");
const backupController = require("../controllers/backupController");
const { validateBackupRequest } = require("../middleware/validation");

// Apply authentication to all routes
router.use(authMiddleware);

// Upload backup
router.post(
  "/upload",
  upload.single("backup"),
  validateBackupRequest,
  backupController.uploadBackup
);

// List user backups
router.get("/list", backupController.listBackups);

// Download backup
router.get("/download/:filename", backupController.downloadBackup);

// Delete backup
router.delete("/delete/:filename", backupController.deleteBackup);

// Get backup metadata
router.get("/metadata/:filename", backupController.getBackupMetadata);

module.exports = router;
```

## 🎮 Step 7: Backup Controller (controllers/backupController.js)

```javascript
const fs = require("fs").promises;
const path = require("path");
const config = require("../config/config");
const logger = require("../utils/logger");
const fileValidator = require("../utils/fileValidator");

class BackupController {
  async uploadBackup(req, res) {
    let tempFilePath = null;

    try {
      if (!req.file) {
        return res.status(400).json({
          error: "No backup file provided",
        });
      }

      tempFilePath = req.file.path;
      const { description = "", isAutomatic = false } = req.body;

      // Validate SQLite file
      const isValid = await fileValidator.validateSQLiteFile(tempFilePath);
      if (!isValid) {
        await fs.unlink(tempFilePath);
        return res.status(400).json({
          error: "Invalid SQLite database file",
        });
      }

      // Create user backup directory
      const userBackupDir = path.join(
        "./backups/users",
        req.user.id.toString()
      );
      await fs.mkdir(userBackupDir, { recursive: true });

      // Move file to permanent location
      const finalFilePath = path.join(userBackupDir, req.file.filename);
      await fs.rename(tempFilePath, finalFilePath);

      // Create metadata
      const metadata = {
        filename: req.file.filename,
        originalName: req.file.originalname,
        description,
        isAutomatic,
        size: req.file.size,
        uploadDate: new Date().toISOString(),
        userId: req.user.id,
        username: req.user.username,
      };

      // Save metadata
      const metadataPath = path.join(
        userBackupDir,
        `${req.file.filename}.meta.json`
      );
      await fs.writeFile(metadataPath, JSON.stringify(metadata, null, 2));

      logger.info(`Backup uploaded successfully`, {
        userId: req.user.id,
        filename: req.file.filename,
        size: req.file.size,
      });

      res.status(201).json({
        message: "Backup uploaded successfully",
        backup: {
          filename: metadata.filename,
          uploadDate: metadata.uploadDate,
          size: metadata.size,
          description: metadata.description,
        },
      });
    } catch (error) {
      // Cleanup on error
      if (tempFilePath) {
        try {
          await fs.unlink(tempFilePath);
        } catch (cleanupError) {
          logger.error(`Failed to cleanup temp file: ${cleanupError.message}`);
        }
      }

      logger.error(`Upload failed: ${error.message}`, {
        userId: req.user?.id,
        error: error.stack,
      });

      res.status(500).json({
        error: "Failed to upload backup",
      });
    }
  }

  async listBackups(req, res) {
    try {
      const userBackupDir = path.join(
        "./backups/users",
        req.user.id.toString()
      );

      try {
        const files = await fs.readdir(userBackupDir);
        const backups = [];

        for (const file of files) {
          if (file.endsWith(".db")) {
            const metadataPath = path.join(userBackupDir, `${file}.meta.json`);
            let metadata = {
              filename: file,
              uploadDate: new Date().toISOString(),
              size: 0,
              description: "",
            };

            try {
              const metadataContent = await fs.readFile(metadataPath, "utf8");
              metadata = JSON.parse(metadataContent);
            } catch (metaError) {
              // Fallback to file stats if metadata missing
              const stats = await fs.stat(path.join(userBackupDir, file));
              metadata.size = stats.size;
              metadata.uploadDate = stats.ctime.toISOString();
            }

            backups.push(metadata);
          }
        }

        // Sort by upload date (newest first)
        backups.sort((a, b) => new Date(b.uploadDate) - new Date(a.uploadDate));

        res.json({ backups });
      } catch (dirError) {
        if (dirError.code === "ENOENT") {
          res.json({ backups: [] });
        } else {
          throw dirError;
        }
      }
    } catch (error) {
      logger.error(`List backups failed: ${error.message}`, {
        userId: req.user.id,
      });

      res.status(500).json({
        error: "Failed to list backups",
      });
    }
  }

  async downloadBackup(req, res) {
    try {
      const { filename } = req.params;

      // Security: Validate filename
      if (
        !filename ||
        filename.includes("..") ||
        filename.includes("/") ||
        filename.includes("\\")
      ) {
        return res.status(400).json({
          error: "Invalid filename",
        });
      }

      const filePath = path.join(
        "./backups/users",
        req.user.id.toString(),
        filename
      );

      try {
        await fs.access(filePath);
      } catch {
        return res.status(404).json({
          error: "Backup file not found",
        });
      }

      logger.info(`Backup download started`, {
        userId: req.user.id,
        filename,
      });

      res.download(filePath, filename, (error) => {
        if (error) {
          logger.error(`Download failed: ${error.message}`, {
            userId: req.user.id,
            filename,
          });
        } else {
          logger.info(`Backup download completed`, {
            userId: req.user.id,
            filename,
          });
        }
      });
    } catch (error) {
      logger.error(`Download backup failed: ${error.message}`, {
        userId: req.user.id,
        filename: req.params.filename,
      });

      res.status(500).json({
        error: "Failed to download backup",
      });
    }
  }

  async deleteBackup(req, res) {
    try {
      const { filename } = req.params;

      // Security: Validate filename
      if (
        !filename ||
        filename.includes("..") ||
        filename.includes("/") ||
        filename.includes("\\")
      ) {
        return res.status(400).json({
          error: "Invalid filename",
        });
      }

      const userBackupDir = path.join(
        "./backups/users",
        req.user.id.toString()
      );
      const filePath = path.join(userBackupDir, filename);
      const metadataPath = path.join(userBackupDir, `${filename}.meta.json`);

      try {
        await fs.access(filePath);
      } catch {
        return res.status(404).json({
          error: "Backup file not found",
        });
      }

      // Delete backup file
      await fs.unlink(filePath);

      // Delete metadata file (if exists)
      try {
        await fs.unlink(metadataPath);
      } catch (metaError) {
        // Metadata file might not exist, continue
      }

      logger.info(`Backup deleted successfully`, {
        userId: req.user.id,
        filename,
      });

      res.json({
        message: "Backup deleted successfully",
      });
    } catch (error) {
      logger.error(`Delete backup failed: ${error.message}`, {
        userId: req.user.id,
        filename: req.params.filename,
      });

      res.status(500).json({
        error: "Failed to delete backup",
      });
    }
  }

  async getBackupMetadata(req, res) {
    try {
      const { filename } = req.params;

      if (
        !filename ||
        filename.includes("..") ||
        filename.includes("/") ||
        filename.includes("\\")
      ) {
        return res.status(400).json({
          error: "Invalid filename",
        });
      }

      const metadataPath = path.join(
        "./backups/users",
        req.user.id.toString(),
        `${filename}.meta.json`
      );

      try {
        const metadataContent = await fs.readFile(metadataPath, "utf8");
        const metadata = JSON.parse(metadataContent);
        res.json({ metadata });
      } catch {
        return res.status(404).json({
          error: "Backup metadata not found",
        });
      }
    } catch (error) {
      logger.error(`Get metadata failed: ${error.message}`, {
        userId: req.user.id,
        filename: req.params.filename,
      });

      res.status(500).json({
        error: "Failed to get backup metadata",
      });
    }
  }
}

module.exports = new BackupController();
```

## 🔧 Step 8: Configuration (config/config.js)

```javascript
module.exports = {
  server: {
    port: process.env.PORT || 4000,
    environment: process.env.NODE_ENV || "development",
  },

  jwt: {
    secret:
      process.env.JWT_SECRET ||
      "your-super-secret-jwt-key-change-in-production",
    expiresIn: process.env.JWT_EXPIRES_IN || "24h",
  },

  cors: {
    allowedOrigins: [
      "http://localhost:3000",
      "http://localhost:8080",
      "http://127.0.0.1:3000",
      ...(process.env.ALLOWED_ORIGINS
        ? process.env.ALLOWED_ORIGINS.split(",")
        : []),
    ],
  },

  upload: {
    maxFileSize: process.env.MAX_FILE_SIZE
      ? parseInt(process.env.MAX_FILE_SIZE)
      : 100 * 1024 * 1024, // 100MB
    allowedExtensions: [".db", ".sqlite", ".sqlite3"],
  },

  backup: {
    retentionDays: process.env.BACKUP_RETENTION_DAYS
      ? parseInt(process.env.BACKUP_RETENTION_DAYS)
      : 30,
    maxBackupsPerUser: process.env.MAX_BACKUPS_PER_USER
      ? parseInt(process.env.MAX_BACKUPS_PER_USER)
      : 50,
  },
};
```

## 🔍 Step 9: File Validator (utils/fileValidator.js)

```javascript
const fs = require("fs").promises;

class FileValidator {
  async validateSQLiteFile(filePath) {
    try {
      const buffer = await fs.readFile(filePath, { encoding: null, flag: "r" });

      // Check SQLite file signature
      const sqliteSignature = "SQLite format 3\0";
      const fileHeader = buffer.slice(0, 16).toString("ascii");

      if (!fileHeader.startsWith(sqliteSignature)) {
        return false;
      }

      // Additional basic validation
      if (buffer.length < 100) {
        // SQLite files should be at least 100 bytes
        return false;
      }

      return true;
    } catch (error) {
      return false;
    }
  }

  validateFilename(filename) {
    // Basic filename validation
    const validPattern = /^[a-zA-Z0-9_\-\.]+$/;
    return validPattern.test(filename) && filename.length <= 255;
  }
}

module.exports = new FileValidator();
```

## 📝 Step 10: Logger Utility (utils/logger.js)

```javascript
const fs = require("fs");
const path = require("path");

class Logger {
  constructor() {
    this.logDir = "./logs";
    this.ensureLogDirectory();
  }

  ensureLogDirectory() {
    if (!fs.existsSync(this.logDir)) {
      fs.mkdirSync(this.logDir, { recursive: true });
    }
  }

  getLogFilePath() {
    const today = new Date().toISOString().split("T")[0];
    return path.join(this.logDir, `server_${today}.log`);
  }

  formatMessage(level, message, meta = {}) {
    const timestamp = new Date().toISOString();
    const metaString =
      Object.keys(meta).length > 0 ? ` | ${JSON.stringify(meta)}` : "";
    return `[${timestamp}] ${level.toUpperCase()}: ${message}${metaString}\n`;
  }

  writeLog(level, message, meta) {
    const logMessage = this.formatMessage(level, message, meta);
    const logFile = this.getLogFilePath();

    fs.appendFileSync(logFile, logMessage);

    // Also log to console in development
    if (process.env.NODE_ENV !== "production") {
      console.log(logMessage.trim());
    }
  }

  info(message, meta = {}) {
    this.writeLog("info", message, meta);
  }

  warn(message, meta = {}) {
    this.writeLog("warn", message, meta);
  }

  error(message, meta = {}) {
    this.writeLog("error", message, meta);
  }
}

module.exports = new Logger();
```

## 🎯 Step 11: Request Validation (middleware/validation.js)

```javascript
const validateBackupRequest = (req, res, next) => {
  if (!req.file) {
    return res.status(400).json({
      error: "Backup file is required",
    });
  }

  // Validate file extension
  const allowedExtensions = [".db", ".sqlite", ".sqlite3"];
  const fileExtension = req.file.originalname.toLowerCase().split(".").pop();

  if (!allowedExtensions.includes(`.${fileExtension}`)) {
    return res.status(400).json({
      error: "Invalid file type. Only SQLite database files are allowed.",
    });
  }

  // Validate description if provided
  if (req.body.description && req.body.description.length > 500) {
    return res.status(400).json({
      error: "Description must be 500 characters or less",
    });
  }

  next();
};

module.exports = {
  validateBackupRequest,
};
```

## 🚀 Step 12: Running the Server

### Development Mode

```bash
npm run dev
```

### Production Mode

```bash
npm start
```

### Environment Variables

Create a `.env` file in the root directory:

```env
NODE_ENV=production
PORT=4000
JWT_SECRET=your-super-secret-key-256-bits-long
MAX_FILE_SIZE=104857600
BACKUP_RETENTION_DAYS=30
MAX_BACKUPS_PER_USER=50
ALLOWED_ORIGINS=http://localhost:3000,https://yourdomain.com
```

## 🔒 Security Considerations

1. **JWT Secret**: Use a strong, randomly generated secret key
2. **File Validation**: Always validate uploaded files
3. **User Isolation**: Ensure users can only access their own backups
4. **Rate Limiting**: Consider implementing rate limiting for uploads
5. **HTTPS**: Use HTTPS in production
6. **File Size Limits**: Enforce reasonable file size limits
7. **Path Traversal**: Prevent directory traversal attacks

## 🌐 API Endpoints

| Method | Endpoint                          | Description          |
| ------ | --------------------------------- | -------------------- |
| POST   | `/api/backups/upload`             | Upload a backup file |
| GET    | `/api/backups/list`               | List user's backups  |
| GET    | `/api/backups/download/:filename` | Download a backup    |
| DELETE | `/api/backups/delete/:filename`   | Delete a backup      |
| GET    | `/api/backups/metadata/:filename` | Get backup metadata  |
| GET    | `/health`                         | Health check         |

## 🔧 Client Configuration

Update the WPF client's `appsettings.json`:

```json
{
  "ApiSettings": {
    "BaseUrl": "http://localhost:4000",
    "Timeout": 300000
  }
}
```

This completes the server implementation guide. The server provides secure, JWT-authenticated backup storage with proper file validation and user isolation.
