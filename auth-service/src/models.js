const mongoose = require("mongoose");

const RoleSchema = new mongoose.Schema(
  {
    id: { type: String, unique: true, index: true },
    name: { type: String, required: true },
  },
  { timestamps: true }
);

const UserSchema = new mongoose.Schema(
  {
    id: { type: String, unique: true },
    username: { type: String, unique: true, required: true },
    passwordHash: { type: String, required: true },
    isActive: { type: Boolean, default: true },
    department: { type: String },
  },
  { timestamps: true }
);

const UserRoleSchema = new mongoose.Schema(
  {
    userId: { type: String, index: true },
    roleId: { type: String, index: true },
  },
  { timestamps: true }
);
UserRoleSchema.index({ userId: 1, roleId: 1 }, { unique: true });

const RefreshTokenSchema = new mongoose.Schema(
  {
    token: { type: String, unique: true },
    userId: { type: String },
    exp: { type: Number },
    revoked: { type: Boolean, default: false },
  },
  { timestamps: true }
);

const LecturerSchema = new mongoose.Schema(
  {
    lecturerId: { type: String, unique: true, required: true },
    lecturerAccount: { type: String },
    lecturerName: { type: String },
    role: { type: String },
    department: { type: String },
    lecturerSubjects: [
      { type: mongoose.Schema.Types.ObjectId, ref: "LecturerSubject" },
    ],
  },
  { timestamps: true }
);

const SubjectSchema = new mongoose.Schema(
  {
    subjectCode: { type: String, unique: true, required: true },
    subjectNameEnglish: { type: String },
    subjectNameVietnamese: { type: String },
    totalTime: { type: Number, required: true },
    totalCredits: { type: Number, required: true },
    lecturerSubjects: [
      { type: mongoose.Schema.Types.ObjectId, ref: "LecturerSubject" },
    ],
  },
  { timestamps: true }
);

const LecturerSubjectSchema = new mongoose.Schema(
  {
    lecturerId: { type: String, required: true }, // Có thể dùng để tra cứu nhanh
    subjectCode: { type: String, required: true },
    subjectName: { type: String },
    lecturerName: { type: String },
    major: { type: String },
    term: { type: Number },
    numberOfClasses: { type: Number },
    totalSlots: { type: Number },
    // Subject: { type: mongoose.Schema.Types.ObjectId, ref: 'Subject' }, // Nếu có model Subject
    lecturer: {
      type: mongoose.Schema.Types.ObjectId,
      ref: "Lecturer",
      required: true,
    }, // Many-to-one
  },
  { timestamps: true }
);

// Schedule Schema based on WPF ScheduleUploadDto
const ScheduleSchema = new mongoose.Schema(
  {
    scheduleId: { type: String, unique: true, required: true },
    groupName: { type: String, required: true },
    subjectCode: { type: String, required: true },
    date: { type: Date, required: true },
    slotTime: { type: String, required: true },
    roomName: { type: String, required: true },
    sessionNo: { type: Number, required: true },
    lecturerName: { type: String, required: true },
    slotTypeCode: { type: String, required: true },
    statusSlot: { type: String, required: true },
    typeSlot: { type: String, required: true },
    roomId: { type: String, required: true },
    partOfDay: { type: String, required: true },
    major: { type: String, required: true },
    lecturerId: { type: String, required: true },
    lecturerAccount: { type: String },
    uploadedBy: { type: String }, // Track who uploaded
    uploadBatch: { type: String }, // Track bulk upload batches
  },
  { timestamps: true }
);

// Compound indexes for common queries and performance optimization
ScheduleSchema.index({ date: 1, lecturerId: 1 });
ScheduleSchema.index({ subjectCode: 1, groupName: 1 });
ScheduleSchema.index({ roomId: 1, date: 1, slotTime: 1 });
ScheduleSchema.index({ major: 1, date: 1 });
ScheduleSchema.index({ uploadBatch: 1 });
ScheduleSchema.index({ createdAt: 1 });
ScheduleSchema.index({ statusSlot: 1, date: 1 });

// Performance settings
ScheduleSchema.set("autoIndex", process.env.NODE_ENV !== "production");
UserSchema.set("autoIndex", process.env.NODE_ENV !== "production");

const Role = mongoose.model("Role", RoleSchema);
const User = mongoose.model("User", UserSchema);
const UserRole = mongoose.model("UserRole", UserRoleSchema);
const RefreshToken = mongoose.model("RefreshToken", RefreshTokenSchema);
const Lecturer = mongoose.model("Lecturer", LecturerSchema);
const Subject = mongoose.model("Subject", SubjectSchema);
const LecturerSubject = mongoose.model(
  "LecturerSubject",
  LecturerSubjectSchema
);
const Schedule = mongoose.model("Schedule", ScheduleSchema);

module.exports = {
  Role,
  User,
  UserRole,
  RefreshToken,
  Lecturer,
  Subject,
  LecturerSubject,
  Schedule,
};
