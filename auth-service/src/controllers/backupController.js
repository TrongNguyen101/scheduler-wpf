const fs = require("fs").promises;
const path = require("path");
const crypto = require("crypto");
const { v4: uuidv4 } = require("uuid");

class BackupController {
  constructor() {
    this.backupDir = path.join(process.cwd(), "backups");
    this.usersDir = path.join(this.backupDir, "users");
    this.maxFileSizeMB = 100;
    this.allowedExtensions = [".db", ".sqlite", ".sqlite3"];
  }

  // Ensure backup directories exist
  async ensureDirectories() {
    try {
      await fs.access(this.backupDir);
    } catch {
      await fs.mkdir(this.backupDir, { recursive: true });
    }

    try {
      await fs.access(this.usersDir);
    } catch {
      await fs.mkdir(this.usersDir, { recursive: true });
    }
  }

  // Get user-specific backup directory
  getUserBackupDir(userId) {
    return path.join(this.usersDir, userId.toString());
  }

  // Ensure user backup directory exists
  async ensureUserDirectory(userId) {
    const userDir = this.getUserBackupDir(userId);
    try {
      await fs.access(userDir);
    } catch {
      await fs.mkdir(userDir, { recursive: true });
    }
    return userDir;
  }

  // Calculate MD5 checksum of file
  async calculateChecksum(filePath) {
    try {
      const data = await fs.readFile(filePath);
      return crypto.createHash("md5").update(data).digest("hex");
    } catch (error) {
      console.error("Error calculating checksum:", error);
      throw error;
    }
  }

  // Validate file extension
  validateFileExtension(filename) {
    const ext = path.extname(filename).toLowerCase();
    return this.allowedExtensions.includes(ext);
  }

  // Generate unique filename to prevent conflicts
  generateUniqueFilename(originalName) {
    const ext = path.extname(originalName);
    const nameWithoutExt = path.basename(originalName, ext);
    const timestamp = new Date().toISOString().replace(/[:.]/g, "-");
    const uniqueId = uuidv4().substring(0, 8);
    return `${nameWithoutExt}_${timestamp}_${uniqueId}${ext}`;
  }

  // Upload backup file
  async uploadBackup(req, res) {
    try {
      await this.ensureDirectories();

      if (!req.file) {
        return res.status(400).json({
          success: false,
          message: "No file uploaded",
          errorCode: "NO_FILE",
        });
      }

      const userId = req.user.sub;
      const userDir = await this.ensureUserDirectory(userId);
      const originalName = req.body.originalName || req.file.originalname;
      const providedChecksum = req.body.checksum;

      // Validate file extension
      if (!this.validateFileExtension(originalName)) {
        // Clean up uploaded file
        try {
          await fs.unlink(req.file.path);
        } catch (cleanupError) {
          console.error("Error cleaning up invalid file:", cleanupError);
        }

        return res.status(400).json({
          success: false,
          message:
            "Invalid file type. Only database files (.db, .sqlite, .sqlite3) are allowed.",
          errorCode: "INVALID_FILE_TYPE",
        });
      }

      // Check file size (multer should handle this, but double-check)
      const fileSizeMB = req.file.size / (1024 * 1024);
      if (fileSizeMB > this.maxFileSizeMB) {
        // Clean up uploaded file
        try {
          await fs.unlink(req.file.path);
        } catch (cleanupError) {
          console.error("Error cleaning up oversized file:", cleanupError);
        }

        return res.status(400).json({
          success: false,
          message: `File size exceeds maximum limit of ${this.maxFileSizeMB}MB`,
          errorCode: "FILE_TOO_LARGE",
        });
      }

      // Calculate checksum of uploaded file
      const actualChecksum = await this.calculateChecksum(req.file.path);

      // Validate checksum if provided
      if (providedChecksum && providedChecksum !== actualChecksum) {
        // Clean up uploaded file
        try {
          await fs.unlink(req.file.path);
        } catch (cleanupError) {
          console.error(
            "Error cleaning up file with invalid checksum:",
            cleanupError
          );
        }

        return res.status(400).json({
          success: false,
          message: "File checksum validation failed. File may be corrupted.",
          errorCode: "CHECKSUM_MISMATCH",
        });
      }

      // Generate unique filename and move to user directory
      const uniqueFilename = this.generateUniqueFilename(originalName);
      const finalPath = path.join(userDir, uniqueFilename);

      await fs.rename(req.file.path, finalPath);

      // Create metadata
      const metadata = {
        filename: uniqueFilename,
        originalName: originalName,
        uploadDate: new Date().toISOString(),
        fileSize: req.file.size,
        checksum: actualChecksum,
        userId: userId,
      };

      // Save metadata file
      const metadataPath = path.join(userDir, `${uniqueFilename}.meta.json`);
      await fs.writeFile(metadataPath, JSON.stringify(metadata, null, 2));

      console.log(
        `Backup uploaded successfully: ${uniqueFilename} for user ${userId}`
      );

      res.json({
        success: true,
        message: "Backup uploaded successfully",
        metadata: metadata,
      });
    } catch (error) {
      console.error("Upload backup error:", error);

      // Clean up uploaded file if it exists
      if (req.file && req.file.path) {
        try {
          await fs.unlink(req.file.path);
        } catch (cleanupError) {
          console.error("Error cleaning up file after error:", cleanupError);
        }
      }

      res.status(500).json({
        success: false,
        message: "Failed to upload backup",
        errorCode: "UPLOAD_ERROR",
      });
    }
  }

  // List user's backups
  async listBackups(req, res) {
    try {
      const userId = req.user.sub;
      const userDir = this.getUserBackupDir(userId);

      try {
        await fs.access(userDir);
      } catch {
        return res.json({
          success: true,
          backups: [],
          totalCount: 0,
          message: "No backups found",
        });
      }

      const files = await fs.readdir(userDir);
      const metadataFiles = files.filter((file) => file.endsWith(".meta.json"));
      const backups = [];

      for (const metaFile of metadataFiles) {
        try {
          const metaPath = path.join(userDir, metaFile);
          const metaContent = await fs.readFile(metaPath, "utf8");
          const metadata = JSON.parse(metaContent);

          // Verify the actual backup file still exists
          const backupPath = path.join(userDir, metadata.filename);
          try {
            await fs.access(backupPath);
            backups.push(metadata);
          } catch {
            console.warn(
              `Metadata found but backup file missing: ${metadata.filename}`
            );
            // Optionally clean up orphaned metadata file
            try {
              await fs.unlink(metaPath);
            } catch (cleanupError) {
              console.error(
                "Error cleaning up orphaned metadata:",
                cleanupError
              );
            }
          }
        } catch (parseError) {
          console.error(`Error parsing metadata file ${metaFile}:`, parseError);
        }
      }

      // Sort by upload date (newest first)
      backups.sort((a, b) => new Date(b.uploadDate) - new Date(a.uploadDate));

      res.json({
        success: true,
        backups: backups,
        totalCount: backups.length,
        message:
          backups.length > 0
            ? `Found ${backups.length} backup(s)`
            : "No backups found",
      });
    } catch (error) {
      console.error("List backups error:", error);
      res.status(500).json({
        success: false,
        message: "Failed to list backups",
        errorCode: "LIST_ERROR",
      });
    }
  }

  // Download backup file
  async downloadBackup(req, res) {
    try {
      const userId = req.user.sub;
      const filename = req.params.filename;
      const userDir = this.getUserBackupDir(userId);
      const backupPath = path.join(userDir, filename);
      const metadataPath = path.join(userDir, `${filename}.meta.json`);

      // Check if backup file exists
      try {
        await fs.access(backupPath);
      } catch {
        return res.status(404).json({
          success: false,
          message: "Backup file not found",
          errorCode: "FILE_NOT_FOUND",
        });
      }

      // Load metadata if available
      let metadata = null;
      try {
        const metaContent = await fs.readFile(metadataPath, "utf8");
        metadata = JSON.parse(metaContent);
      } catch {
        console.warn(
          `Metadata not found for ${filename}, proceeding without it`
        );
      }

      // Get file stats
      const stats = await fs.stat(backupPath);
      const checksum = await this.calculateChecksum(backupPath);

      // Set response headers
      res.setHeader("Content-Type", "application/octet-stream");
      res.setHeader(
        "Content-Disposition",
        `attachment; filename="${metadata?.originalName || filename}"`
      );
      res.setHeader("Content-Length", stats.size);
      res.setHeader("X-Checksum", checksum);

      // Stream the file
      const readStream = require("fs").createReadStream(backupPath);
      readStream.pipe(res);

      readStream.on("error", (error) => {
        console.error("Error streaming backup file:", error);
        if (!res.headersSent) {
          res.status(500).json({
            success: false,
            message: "Error downloading backup",
            errorCode: "DOWNLOAD_ERROR",
          });
        }
      });

      console.log(`Backup downloaded: ${filename} for user ${userId}`);
    } catch (error) {
      console.error("Download backup error:", error);
      if (!res.headersSent) {
        res.status(500).json({
          success: false,
          message: "Failed to download backup",
          errorCode: "DOWNLOAD_ERROR",
        });
      }
    }
  }

  // Delete backup file
  async deleteBackup(req, res) {
    try {
      const userId = req.user.sub;
      const filename = req.params.filename;
      const userDir = this.getUserBackupDir(userId);
      const backupPath = path.join(userDir, filename);
      const metadataPath = path.join(userDir, `${filename}.meta.json`);

      // Check if backup file exists
      try {
        await fs.access(backupPath);
      } catch {
        return res.status(404).json({
          success: false,
          message: "Backup file not found",
          errorCode: "FILE_NOT_FOUND",
        });
      }

      // Delete backup file
      await fs.unlink(backupPath);

      // Delete metadata file if it exists
      try {
        await fs.unlink(metadataPath);
      } catch {
        // Metadata file might not exist, that's okay
      }

      console.log(`Backup deleted: ${filename} for user ${userId}`);

      res.json({
        success: true,
        message: "Backup deleted successfully",
      });
    } catch (error) {
      console.error("Delete backup error:", error);
      res.status(500).json({
        success: false,
        message: "Failed to delete backup",
        errorCode: "DELETE_ERROR",
      });
    }
  }
}

module.exports = new BackupController();
