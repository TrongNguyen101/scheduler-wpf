const express = require("express");
const multer = require("multer");
const path = require("path");
const rateLimit = require("express-rate-limit");
const { requireAuth } = require("../middleware/auth");
const backupController = require("../controllers/backupController");

const router = express.Router();

// Configure multer for file uploads
const storage = multer.diskStorage({
  destination: function (req, file, cb) {
    // Temporary upload directory - files will be moved to user-specific directories
    const tempDir = path.join(process.cwd(), "temp-uploads");
    cb(null, tempDir);
  },
  filename: function (req, file, cb) {
    // Generate temporary filename with timestamp to avoid conflicts
    const uniqueSuffix = Date.now() + "-" + Math.round(Math.random() * 1e9);
    cb(
      null,
      file.fieldname + "-" + uniqueSuffix + path.extname(file.originalname)
    );
  },
});

// File filter for backup uploads
const fileFilter = (req, file, cb) => {
  // Accept all files initially - detailed validation happens in controller
  // This allows for better error messages from the controller
  cb(null, true);
};

// Configure multer with size limits and storage
const upload = multer({
  storage: storage,
  fileFilter: fileFilter,
  limits: {
    fileSize: 100 * 1024 * 1024, // 100MB limit
    files: 1, // Only one file at a time
  },
});

// Rate limiting for backup operations
const backupOperationLimiter = rateLimit({
  windowMs: 15 * 60 * 1000, // 15 minutes
  max: 10, // 10 backup operations per 15 minutes per IP
  message: {
    success: false,
    error: {
      code: "RATE_LIMIT_EXCEEDED",
      message: "Too many backup operations. Please try again later.",
    },
    timestamp: new Date().toISOString(),
  },
  standardHeaders: true,
  legacyHeaders: false,
});

// More restrictive rate limiting for uploads
const uploadLimiter = rateLimit({
  windowMs: 60 * 60 * 1000, // 1 hour
  max: 5, // 5 uploads per hour per IP
  message: {
    success: false,
    error: {
      code: "UPLOAD_RATE_LIMIT_EXCEEDED",
      message: "Too many file uploads. Please try again later.",
    },
    timestamp: new Date().toISOString(),
  },
  standardHeaders: true,
  legacyHeaders: false,
});

// Ensure temp upload directory exists
const fs = require("fs");
const tempUploadDir = path.join(process.cwd(), "temp-uploads");
if (!fs.existsSync(tempUploadDir)) {
  fs.mkdirSync(tempUploadDir, { recursive: true });
}

// Middleware to handle multer errors
const handleMulterError = (error, req, res, next) => {
  if (error instanceof multer.MulterError) {
    switch (error.code) {
      case "LIMIT_FILE_SIZE":
        return res.status(400).json({
          success: false,
          message: "File size exceeds the maximum limit of 100MB",
          errorCode: "FILE_TOO_LARGE",
        });
      case "LIMIT_FILE_COUNT":
        return res.status(400).json({
          success: false,
          message: "Too many files. Only one file allowed per upload",
          errorCode: "TOO_MANY_FILES",
        });
      case "LIMIT_UNEXPECTED_FILE":
        return res.status(400).json({
          success: false,
          message: "Unexpected file field. Use 'file' field name",
          errorCode: "UNEXPECTED_FIELD",
        });
      default:
        return res.status(400).json({
          success: false,
          message: "File upload error: " + error.message,
          errorCode: "UPLOAD_ERROR",
        });
    }
  }
  next(error);
};

// All backup routes require authentication
router.use(requireAuth);

// POST /api/backups/upload - Upload backup file
router.post(
  "/upload",
  uploadLimiter,
  upload.single("file"),
  handleMulterError,
  backupController.uploadBackup.bind(backupController)
);

// GET /api/backups/list - List user's backups
router.get(
  "/list",
  backupOperationLimiter,
  backupController.listBackups.bind(backupController)
);

// GET /api/backups/download/:filename - Download specific backup
router.get(
  "/download/:filename",
  backupOperationLimiter,
  (req, res, next) => {
    // Validate filename parameter
    const filename = req.params.filename;
    if (
      !filename ||
      filename.includes("..") ||
      filename.includes("/") ||
      filename.includes("\\")
    ) {
      return res.status(400).json({
        success: false,
        message: "Invalid filename",
        errorCode: "INVALID_FILENAME",
      });
    }
    next();
  },
  backupController.downloadBackup.bind(backupController)
);

// DELETE /api/backups/:filename - Delete specific backup
router.delete(
  "/:filename",
  backupOperationLimiter,
  (req, res, next) => {
    // Validate filename parameter
    const filename = req.params.filename;
    if (
      !filename ||
      filename.includes("..") ||
      filename.includes("/") ||
      filename.includes("\\")
    ) {
      return res.status(400).json({
        success: false,
        message: "Invalid filename",
        errorCode: "INVALID_FILENAME",
      });
    }
    next();
  },
  backupController.deleteBackup.bind(backupController)
);

// GET /api/backups - Alias for /list for convenience
router.get(
  "/",
  backupOperationLimiter,
  backupController.listBackups.bind(backupController)
);

// Error handling for backup routes
router.use((error, req, res, next) => {
  console.error("Backup routes error:", error);

  // Clean up any uploaded files on error
  if (req.file && req.file.path) {
    const fs = require("fs");
    fs.unlink(req.file.path, (unlinkError) => {
      if (unlinkError) {
        console.error("Error cleaning up uploaded file:", unlinkError);
      }
    });
  }

  res.status(500).json({
    success: false,
    message: "Backup operation failed",
    errorCode: "BACKUP_ERROR",
    timestamp: new Date().toISOString(),
  });
});

module.exports = router;
