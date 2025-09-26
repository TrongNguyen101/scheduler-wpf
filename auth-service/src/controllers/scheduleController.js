const mongoose = require("mongoose");
const { v4: uuidv4 } = require("uuid");
const { Schedule } = require("../models");

// Helper function to create standardized API responses
function createResponse(success, data = null, error = null, meta = null) {
  return {
    success,
    data,
    error,
    meta,
    timestamp: new Date().toISOString(),
  };
}

// Helper function to handle transient transaction errors
async function withTransactionRetry(operation, maxRetries = 3) {
  for (let attempt = 1; attempt <= maxRetries; attempt++) {
    try {
      return await operation();
    } catch (error) {
      const isTransientError =
        error.hasErrorLabel && error.hasErrorLabel("TransientTransactionError");
      const isRetriableError =
        error.code === 251 || error.codeName === "NoSuchTransaction";

      if ((isTransientError || isRetriableError) && attempt < maxRetries) {
        console.log(
          `Transaction attempt ${attempt} failed with transient error, retrying...`
        );
        // Wait before retry with exponential backoff
        await new Promise((resolve) =>
          setTimeout(resolve, Math.pow(2, attempt) * 100)
        );
        continue;
      }
      throw error;
    }
  }
}

// Bulk upload schedules with improved transaction handling
async function bulkUploadSchedules(req, res) {
  let session = null;

  try {
    const {
      schedules,
      overwriteExisting = false,
      validateOnly = false,
    } = req.body;
    const uploadBatch = uuidv4();
    const uploadedBy = req.user.username;

    let created = 0;
    let updated = 0;
    let skipped = 0;
    const errors = [];

    // If validation only, don't use transactions
    if (validateOnly) {
      return res.json(
        createResponse(true, {
          message: "Validation successful",
          totalRecords: schedules.length,
          validRecords: schedules.length,
          errors: [],
        })
      );
    }

    // For small datasets, use transactions. For large ones, use regular operations
    const useTransaction = schedules.length <= 1000;

    if (useTransaction) {
      session = await mongoose.startSession();
      session.startTransaction();
    }

    // Process schedules in smaller batches to avoid transaction timeouts
    const batchSize = useTransaction ? 50 : 100;

    for (let i = 0; i < schedules.length; i += batchSize) {
      const batch = schedules.slice(i, i + batchSize);

      try {
        // Process batch operations
        const batchOperations = [];

        for (const [index, scheduleData] of batch.entries()) {
          const absoluteIndex = i + index;

          try {
            // Check if schedule exists
            const findQuery = { scheduleId: scheduleData.scheduleId };
            const existingSchedule = useTransaction
              ? await Schedule.findOne(findQuery).session(session)
              : await Schedule.findOne(findQuery);

            if (existingSchedule) {
              if (overwriteExisting) {
                const updateData = {
                  ...scheduleData,
                  uploadedBy,
                  uploadBatch,
                  updatedAt: new Date(),
                };

                if (useTransaction) {
                  await Schedule.updateOne(
                    { scheduleId: scheduleData.scheduleId },
                    updateData,
                    { session }
                  );
                } else {
                  await Schedule.updateOne(
                    { scheduleId: scheduleData.scheduleId },
                    updateData
                  );
                }
                updated++;
              } else {
                skipped++;
                errors.push({
                  index: absoluteIndex,
                  scheduleId: scheduleData.scheduleId,
                  error: "Schedule already exists",
                });
              }
            } else {
              const newSchedule = {
                ...scheduleData,
                uploadedBy,
                uploadBatch,
              };

              if (useTransaction) {
                await Schedule.create([newSchedule], { session });
              } else {
                await Schedule.create(newSchedule);
              }
              created++;
            }
          } catch (itemError) {
            errors.push({
              index: absoluteIndex,
              scheduleId: scheduleData.scheduleId || `Unknown-${absoluteIndex}`,
              error: itemError.message,
            });
          }
        }

        // If using transactions, check if we need to commit periodically for very large batches
        if (useTransaction && i > 0 && i % 500 === 0) {
          // For very large datasets within transaction limit, commit periodically
          await session.commitTransaction();
          session.endSession();

          session = await mongoose.startSession();
          session.startTransaction();
        }
      } catch (batchError) {
        console.error(
          `Batch ${Math.floor(i / batchSize) + 1} error:`,
          batchError
        );
        // Continue with next batch instead of failing completely
        errors.push({
          index: i,
          scheduleId: `Batch-${Math.floor(i / batchSize) + 1}`,
          error: `Batch processing error: ${batchError.message}`,
        });
      }
    }

    // Commit transaction if used
    if (useTransaction && session) {
      await withTransactionRetry(async () => {
        await session.commitTransaction();
      });
      session.endSession();
      session = null;
    }

    // Prepare response data
    const responseData = {
      message: "Bulk upload completed",
      uploadBatch,
      statistics: {
        totalRecords: schedules.length,
        created,
        updated,
        skipped,
        errors: errors.length,
      },
      errors: errors.slice(0, 100), // Limit error details to first 100
      transactionMode: useTransaction ? "transactional" : "non-transactional",
    };

    // Send response
    res.status(201).json(createResponse(true, responseData));
  } catch (error) {
    console.error("Bulk upload error:", error);

    // Cleanup session if it exists
    if (session) {
      try {
        if (session.inTransaction()) {
          await session.abortTransaction();
        }
        session.endSession();
      } catch (sessionError) {
        console.error("Session cleanup error:", sessionError);
      }
    }

    res.status(500).json(
      createResponse(false, null, {
        code: "BULK_UPLOAD_ERROR",
        message: "Failed to process bulk upload",
        details: error.message,
      })
    );
  }
}

// Get schedules with filtering and pagination
async function getSchedules(req, res) {
  try {
    const {
      page,
      limit,
      lecturerId,
      subjectCode,
      groupName,
      roomId,
      major,
      statusSlot,
      startDate,
      endDate,
      sortBy,
      sortOrder,
    } = req.query;

    // Build filter object
    const filter = {};
    if (lecturerId) filter.lecturerId = lecturerId;
    if (subjectCode) filter.subjectCode = subjectCode;
    if (groupName) filter.groupName = { $regex: groupName, $options: "i" };
    if (roomId) filter.roomId = roomId;
    if (major) filter.major = major;
    if (statusSlot) filter.statusSlot = statusSlot;

    if (startDate || endDate) {
      filter.date = {};
      if (startDate) filter.date.$gte = new Date(startDate);
      if (endDate) filter.date.$lte = new Date(endDate);
    }

    // Build sort object
    const sort = {};
    sort[sortBy] = sortOrder === "desc" ? -1 : 1;

    // Calculate pagination
    const skip = (page - 1) * limit;

    // Execute queries
    const [schedules, total] = await Promise.all([
      Schedule.find(filter).sort(sort).skip(skip).limit(limit).lean(),
      Schedule.countDocuments(filter),
    ]);

    const totalPages = Math.ceil(total / limit);

    res.json(
      createResponse(true, schedules, null, {
        pagination: {
          currentPage: page,
          totalPages,
          totalRecords: total,
          recordsPerPage: limit,
          hasNextPage: page < totalPages,
          hasPreviousPage: page > 1,
        },
        filters: filter,
      })
    );
  } catch (error) {
    console.error("Get schedules error:", error);
    res.status(500).json(
      createResponse(false, null, {
        code: "QUERY_ERROR",
        message: "Failed to retrieve schedules",
        details: error.message,
      })
    );
  }
}

// Get schedule by ID
async function getScheduleById(req, res) {
  try {
    const { id } = req.params;

    const schedule = await Schedule.findOne({
      $or: [
        { _id: mongoose.isValidObjectId(id) ? id : null },
        { scheduleId: id },
      ],
    }).lean();

    if (!schedule) {
      return res.status(404).json(
        createResponse(false, null, {
          code: "NOT_FOUND",
          message: "Schedule not found",
        })
      );
    }

    res.json(createResponse(true, schedule));
  } catch (error) {
    console.error("Get schedule by ID error:", error);
    res.status(500).json(
      createResponse(false, null, {
        code: "QUERY_ERROR",
        message: "Failed to retrieve schedule",
        details: error.message,
      })
    );
  }
}

// Update schedule by ID
async function updateSchedule(req, res) {
  try {
    const { id } = req.params;
    const updateData = { ...req.body, updatedAt: new Date() };

    const schedule = await Schedule.findOneAndUpdate(
      {
        $or: [
          { _id: mongoose.isValidObjectId(id) ? id : null },
          { scheduleId: id },
        ],
      },
      updateData,
      { new: true, runValidators: true }
    );

    if (!schedule) {
      return res.status(404).json(
        createResponse(false, null, {
          code: "NOT_FOUND",
          message: "Schedule not found",
        })
      );
    }

    res.json(
      createResponse(true, schedule, null, {
        message: "Schedule updated successfully",
      })
    );
  } catch (error) {
    console.error("Update schedule error:", error);

    if (error.name === "ValidationError") {
      return res.status(400).json(
        createResponse(false, null, {
          code: "VALIDATION_ERROR",
          message: "Invalid schedule data",
          details: error.message,
        })
      );
    }

    res.status(500).json(
      createResponse(false, null, {
        code: "UPDATE_ERROR",
        message: "Failed to update schedule",
        details: error.message,
      })
    );
  }
}

// Delete schedule by ID
async function deleteSchedule(req, res) {
  try {
    const { id } = req.params;

    const schedule = await Schedule.findOneAndDelete({
      $or: [
        { _id: mongoose.isValidObjectId(id) ? id : null },
        { scheduleId: id },
      ],
    });

    if (!schedule) {
      return res.status(404).json(
        createResponse(false, null, {
          code: "NOT_FOUND",
          message: "Schedule not found",
        })
      );
    }

    res.json(
      createResponse(true, null, null, {
        message: "Schedule deleted successfully",
        deletedSchedule: {
          id: schedule._id,
          scheduleId: schedule.scheduleId,
        },
      })
    );
  } catch (error) {
    console.error("Delete schedule error:", error);
    res.status(500).json(
      createResponse(false, null, {
        code: "DELETE_ERROR",
        message: "Failed to delete schedule",
        details: error.message,
      })
    );
  }
}

// Get schedule statistics
async function getScheduleStats(req, res) {
  try {
    const { major, lecturerId } = req.query;

    const matchFilter = {};
    if (major) matchFilter.major = major;
    if (lecturerId) matchFilter.lecturerId = lecturerId;

    const stats = await Schedule.aggregate([
      { $match: matchFilter },
      {
        $group: {
          _id: null,
          totalSchedules: { $sum: 1 },
          uniqueLecturers: { $addToSet: "$lecturerId" },
          uniqueSubjects: { $addToSet: "$subjectCode" },
          uniqueRooms: { $addToSet: "$roomId" },
          uniqueGroups: { $addToSet: "$groupName" },
          statusBreakdown: {
            $push: "$statusSlot",
          },
        },
      },
      {
        $project: {
          totalSchedules: 1,
          uniqueLecturersCount: { $size: "$uniqueLecturers" },
          uniqueSubjectsCount: { $size: "$uniqueSubjects" },
          uniqueRoomsCount: { $size: "$uniqueRooms" },
          uniqueGroupsCount: { $size: "$uniqueGroups" },
          statusBreakdown: 1,
        },
      },
    ]);

    // Count status breakdown
    const statusCounts = {};
    if (stats.length > 0 && stats[0].statusBreakdown) {
      stats[0].statusBreakdown.forEach((status) => {
        statusCounts[status] = (statusCounts[status] || 0) + 1;
      });
    }

    const result =
      stats.length > 0
        ? {
            ...stats[0],
            statusBreakdown: statusCounts,
          }
        : {
            totalSchedules: 0,
            uniqueLecturersCount: 0,
            uniqueSubjectsCount: 0,
            uniqueRoomsCount: 0,
            uniqueGroupsCount: 0,
            statusBreakdown: {},
          };

    res.json(createResponse(true, result));
  } catch (error) {
    console.error("Get schedule stats error:", error);
    res.status(500).json(
      createResponse(false, null, {
        code: "STATS_ERROR",
        message: "Failed to retrieve schedule statistics",
        details: error.message,
      })
    );
  }
}

// Delete all schedules - DANGER: This will remove all schedules from the database
async function deleteAllSchedules(req, res) {
  const session = await mongoose.startSession();
  session.startTransaction();

  try {
    // Get total count before deletion for confirmation
    const totalCount = await Schedule.countDocuments({}, { session });

    if (totalCount === 0) {
      await session.commitTransaction();
      return res.json(
        createResponse(true, null, null, {
          message: "No schedules found to delete",
          deletedCount: 0,
          totalCount: 0,
        })
      );
    }

    // Delete all schedules
    const deleteResult = await Schedule.deleteMany({}, { session });

    await session.commitTransaction();

    res.json(
      createResponse(true, null, null, {
        message: "All schedules have been deleted successfully",
        deletedCount: deleteResult.deletedCount,
        totalCount: totalCount,
      })
    );
  } catch (error) {
    await session.abortTransaction();
    console.error("Delete all schedules error:", error);
    res.status(500).json(
      createResponse(false, null, {
        code: "DELETE_ALL_ERROR",
        message: "Failed to delete all schedules",
        details: error.message,
      })
    );
  } finally {
    session.endSession();
  }
}

// Bulk delete schedules
async function bulkDeleteSchedules(req, res) {
  const session = await mongoose.startSession();
  session.startTransaction();

  try {
    const { scheduleIds, filters } = req.body;

    let deleteFilter = {};

    if (scheduleIds && Array.isArray(scheduleIds) && scheduleIds.length > 0) {
      deleteFilter = {
        $or: [
          {
            _id: {
              $in: scheduleIds.filter((id) => mongoose.isValidObjectId(id)),
            },
          },
          { scheduleId: { $in: scheduleIds } },
        ],
      };
    } else if (filters) {
      // Build filter from provided criteria
      if (filters.major) deleteFilter.major = filters.major;
      if (filters.lecturerId) deleteFilter.lecturerId = filters.lecturerId;
      if (filters.statusSlot) deleteFilter.statusSlot = filters.statusSlot;
      if (filters.uploadBatch) deleteFilter.uploadBatch = filters.uploadBatch;
    } else {
      return res.status(400).json(
        createResponse(false, null, {
          code: "INVALID_REQUEST",
          message: "Either scheduleIds or filters must be provided",
        })
      );
    }

    const deleteResult = await Schedule.deleteMany(deleteFilter, { session });

    await session.commitTransaction();

    res.json(
      createResponse(true, null, null, {
        message: "Bulk delete completed",
        deletedCount: deleteResult.deletedCount,
      })
    );
  } catch (error) {
    await session.abortTransaction();
    console.error("Bulk delete error:", error);
    res.status(500).json(
      createResponse(false, null, {
        code: "BULK_DELETE_ERROR",
        message: "Failed to process bulk delete",
        details: error.message,
      })
    );
  } finally {
    session.endSession();
  }
}

module.exports = {
  bulkUploadSchedules,
  getSchedules,
  getScheduleById,
  updateSchedule,
  deleteSchedule,
  getScheduleStats,
  deleteAllSchedules,
  bulkDeleteSchedules,
};
