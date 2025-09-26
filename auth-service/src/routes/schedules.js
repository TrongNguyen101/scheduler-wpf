const express = require("express");
const rateLimit = require("express-rate-limit");
const {
  requireAuth,
  requireAcademicStaff,
  requireHeadOfDepartment,
} = require("../middleware/auth");
const {
  validateBody,
  validateQuery,
  bulkScheduleSchema,
  scheduleQuerySchema,
  scheduleUpdateSchema,
} = require("../middleware/validation");
const {
  bulkUploadSchedules,
  getSchedules,
  getScheduleById,
  updateSchedule,
  deleteSchedule,
  getScheduleStats,
  deleteAllSchedules,
  bulkDeleteSchedules,
} = require("../controllers/scheduleController");

const router = express.Router();

// Rate limiting for bulk operations
const bulkOperationLimiter = rateLimit({
  windowMs: 15 * 60 * 1000, // 15 minutes
  max: 5, // 5 bulk operations per 15 minutes
  message: {
    success: false,
    error: {
      code: "RATE_LIMIT_EXCEEDED",
      message: "Too many bulk operations. Please try again later.",
    },
    timestamp: new Date().toISOString(),
  },
  standardHeaders: true,
  legacyHeaders: false,
});

// All routes require authentication
router.use(requireAuth);

// GET /schedules - Query schedules with filtering and pagination
router.get("/", validateQuery(scheduleQuerySchema), getSchedules);

// GET /schedules/stats - Get schedule statistics
router.get("/stats", getScheduleStats);

// GET /schedules/:id - Get specific schedule by ID or scheduleId
router.get("/:id", getScheduleById);

// POST /schedules/upload - Bulk upload schedules (Academic staff and above)
router.post(
  "/upload",
  requireAcademicStaff,
  bulkOperationLimiter,
  validateBody(bulkScheduleSchema),
  bulkUploadSchedules
);

// PUT /schedules/:id - Update specific schedule (Academic staff and above)
router.put(
  "/:id",
  requireAcademicStaff,
  validateBody(scheduleUpdateSchema),
  updateSchedule
);

// DELETE /schedules/all - Delete ALL schedules (Head of Department and above) - DANGER!
router.delete(
  "/all",
  requireHeadOfDepartment,
  bulkOperationLimiter,
  deleteAllSchedules
);

// DELETE /schedules/:id - Delete specific schedule (Head of Department and above)
router.delete("/:id", requireHeadOfDepartment, deleteSchedule);

// POST /schedules/bulk-delete - Bulk delete schedules (Head of Department and above)
router.post(
  "/bulk-delete",
  requireHeadOfDepartment,
  bulkOperationLimiter,
  bulkDeleteSchedules
);

// Error handling for this router
router.use((error, req, res, next) => {
  console.error("Schedule routes error:", error);

  if (error.type === "entity.parse.failed") {
    return res.status(400).json({
      success: false,
      error: {
        code: "INVALID_JSON",
        message: "Invalid JSON in request body",
      },
      timestamp: new Date().toISOString(),
    });
  }

  if (error.type === "entity.too.large") {
    return res.status(413).json({
      success: false,
      error: {
        code: "PAYLOAD_TOO_LARGE",
        message: "Request payload too large",
      },
      timestamp: new Date().toISOString(),
    });
  }

  res.status(500).json({
    success: false,
    error: {
      code: "INTERNAL_ERROR",
      message: "An unexpected error occurred",
    },
    timestamp: new Date().toISOString(),
  });
});

module.exports = router;
