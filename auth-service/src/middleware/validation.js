const Joi = require("joi");

// Schedule validation schema based on WPF ScheduleUploadDto
const scheduleSchema = Joi.object({
  scheduleId: Joi.string().required().trim(),
  groupName: Joi.string().required().trim(),
  subjectCode: Joi.string().required().trim(),
  date: Joi.date().required(),
  slotTime: Joi.string().required().trim(),
  roomName: Joi.string().required().trim(),
  sessionNo: Joi.number().integer().min(1).required(),
  lecturerName: Joi.string().required().trim(),
  slotTypeCode: Joi.string().required().trim(),
  statusSlot: Joi.string().required().trim(),
  typeSlot: Joi.string().required().trim(),
  roomId: Joi.string().required().trim(),
  partOfDay: Joi.string().required().trim(),
  major: Joi.string().required().trim(),
  lecturerId: Joi.string().required().trim(),
  lecturerAccount: Joi.string().optional().allow("").trim(),
});

// Bulk schedule upload validation
const bulkScheduleSchema = Joi.object({
  schedules: Joi.array().items(scheduleSchema).min(1).max(10000).required(),
  overwriteExisting: Joi.boolean().default(false),
  validateOnly: Joi.boolean().default(false),
});

// Schedule query validation
const scheduleQuerySchema = Joi.object({
  page: Joi.number().integer().min(1).default(1),
  limit: Joi.number().integer().min(1).max(1000).default(50),
  lecturerId: Joi.string().optional().trim(),
  subjectCode: Joi.string().optional().trim(),
  groupName: Joi.string().optional().trim(),
  roomId: Joi.string().optional().trim(),
  major: Joi.string().optional().trim(),
  statusSlot: Joi.string().optional().trim(),
  startDate: Joi.date().optional(),
  endDate: Joi.date().optional(),
  sortBy: Joi.string()
    .valid("date", "lecturerName", "groupName", "subjectCode", "roomName")
    .default("date"),
  sortOrder: Joi.string().valid("asc", "desc").default("asc"),
});

// Schedule update validation
const scheduleUpdateSchema = Joi.object({
  groupName: Joi.string().optional().trim(),
  subjectCode: Joi.string().optional().trim(),
  date: Joi.date().optional(),
  slotTime: Joi.string().optional().trim(),
  roomName: Joi.string().optional().trim(),
  sessionNo: Joi.number().integer().min(1).optional(),
  lecturerName: Joi.string().optional().trim(),
  slotTypeCode: Joi.string().optional().trim(),
  statusSlot: Joi.string().optional().trim(),
  typeSlot: Joi.string().optional().trim(),
  roomId: Joi.string().optional().trim(),
  partOfDay: Joi.string().optional().trim(),
  major: Joi.string().optional().trim(),
  lecturerId: Joi.string().optional().trim(),
  lecturerAccount: Joi.string().optional().allow("").trim(),
}).min(1);

// Generic validation middleware
function validateBody(schema) {
  return (req, res, next) => {
    const { error, value } = schema.validate(req.body, {
      abortEarly: false,
      stripUnknown: true,
      convert: true,
    });

    if (error) {
      const details = error.details.map((detail) => ({
        field: detail.path.join("."),
        message: detail.message,
        value: detail.context?.value,
      }));

      return res.status(400).json({
        success: false,
        error: {
          code: "VALIDATION_ERROR",
          message: "Invalid request data",
          details,
        },
        timestamp: new Date().toISOString(),
      });
    }

    req.body = value;
    next();
  };
}

// Query validation middleware
function validateQuery(schema) {
  return (req, res, next) => {
    const { error, value } = schema.validate(req.query, {
      abortEarly: false,
      stripUnknown: true,
      convert: true,
    });

    if (error) {
      const details = error.details.map((detail) => ({
        field: detail.path.join("."),
        message: detail.message,
        value: detail.context?.value,
      }));

      return res.status(400).json({
        success: false,
        error: {
          code: "VALIDATION_ERROR",
          message: "Invalid query parameters",
          details,
        },
        timestamp: new Date().toISOString(),
      });
    }

    req.query = value;
    next();
  };
}

module.exports = {
  scheduleSchema,
  bulkScheduleSchema,
  scheduleQuerySchema,
  scheduleUpdateSchema,
  validateBody,
  validateQuery,
};
