const express = require("express");
const bcrypt = require("bcryptjs");
const jwt = require("jsonwebtoken");
const ExcelJS = require("exceljs");
// const multer = require("multer");
// const XLSX = require("xlsx");
// const { v4: uuidv4 } = require("uuid");
const { loadConfig } = require("../setup");
const {
  User,
  Role,
  UserRole,
  Lecturer,
  LecturerSubject,
  Subject,
} = require("../models");

const router = express.Router();
const config = loadConfig();
// const upload = multer({ storage: multer.memoryStorage() });

function requireAuth(req, res, next) {
  const auth = req.headers.authorization || "";
  const token = auth.startsWith("Bearer ") ? auth.slice(7) : null;
  if (!token) return res.status(401).json({ message: "Unauthorized" });
  try {
    req.user = jwt.verify(token, config.accessSecret);
    next();
  } catch (e) {
    return res.status(401).json({ message: "Unauthorized" });
  }
}

function requireHeadOfDepartment(req, res, next) {
  if (
    !req.user ||
    !Array.isArray(req.user.roles) ||
    !req.user.roles.includes("HeadOfDepartment")
  ) {
    return res.status(403).json({ message: "Forbidden" });
  }
  return next();
}

router.use(requireAuth);

// router.post(
//   "/import-excel",
//   requireHeadOfDepartment,
//   upload.single("file"),
//   async (req, res) => {
//     if (!req.file) return res.status(400).json({ message: "No file uploaded" });

//     try {
//       const workbook = XLSX.read(req.file.buffer, { type: "buffer" });
//       const sheetName = workbook.SheetNames[0];
//       const data = XLSX.utils.sheet_to_json(workbook.Sheets[sheetName], {
//         defval: "",
//       });

//       // Map header to Lecturer fields
//       const lecturers = data
//         .filter((row) => row.MaNV && row.MaNV.toString().trim() !== "")
//         .map((row) => ({
//           lecturerId: row.MaNV.toString(),
//           lecturerName: row.Fullname,
//           lecturerAccount: row.accGV,
//           department: row.Bomon,
//           role: row.LoaiGV,
//         }));

//       // Upsert lecturers by lecturerId
//       const bulkOps = lecturers.map((l) => ({
//         updateOne: {
//           filter: { lecturerId: l.lecturerId },
//           update: { $set: l },
//           upsert: true,
//         },
//       }));

//       if (bulkOps.length > 0) await Lecturer.bulkWrite(bulkOps);

//       res.json({ imported: lecturers.length });
//     } catch (err) {
//       res.status(500).json({ message: "Import failed", error: err.message });
//     }
//   }
// );

// router.post(
//   "/import-lecturer-subject-excel",
//   requireHeadOfDepartment,
//   upload.single("file"),
//   async (req, res) => {
//     if (!req.file) return res.status(400).json({ message: "No file uploaded" });

//     try {
//       const workbook = XLSX.read(req.file.buffer, { type: "buffer" });
//       const sheetName = workbook.SheetNames[0];
//       const data = XLSX.utils.sheet_to_json(workbook.Sheets[sheetName], {
//         defval: "",
//       });

//       // Lọc bỏ dòng không có MAGV hoặc MAMH
//       const rows = data.filter((row) => row.MAGV && row.MAMH);

//       let imported = 0;
//       for (const row of rows) {
//         // Tìm Lecturer theo lecturerId (MAGV)
//         const lecturer = await Lecturer.findOne({ lecturerId: row.MAGV });
//         if (!lecturer) continue; // Bỏ qua nếu không tìm thấy giảng viên

//         // Tạo LecturerSubject mới
//         await LecturerSubject.create({
//           lecturerId: row.MAGV,
//           subjectCode: row.MAMH,
//           subjectName: row.TENMH,
//           lecturerName: row.GIANGVIEN,
//           major: row.NGANH,
//           term: Number(row.KY) || undefined,
//           numberOfClasses: Number(row.SLL) || undefined,
//           totalSlots: Number(row.TONGSLOT) || undefined,
//           lecturer: lecturer._id,
//         });
//         imported++;
//       }

//       res.json({ imported });
//     } catch (err) {
//       res.status(500).json({ message: "Import failed", error: err.message });
//     }
//   }
// );

// router.post(
//   "/import-excel/subjects",
//   requireHeadOfDepartment,
//   upload.single("file"),
//   async (req, res) => {
//     if (!req.file) return res.status(400).json({ message: "No file uploaded" });

//     try {
//       const workbook = XLSX.read(req.file.buffer, { type: "buffer" });
//       const sheetName = workbook.SheetNames[0];
//       const data = XLSX.utils.sheet_to_json(workbook.Sheets[sheetName], {
//         defval: "",
//       });

//       // Map header to Subject fields, bỏ qua dòng không có SubjectCode
//       const subjects = data
//         .filter(
//           (row) => row.SubjectCode && row.SubjectCode.toString().trim() !== ""
//         )
//         .map((row) => ({
//           subjectCode: row.SubjectCode.toString(),
//           subjectNameEnglish: row.SubjectNameEnglish,
//           subjectNameVietnamese: row.SubjectNameVietnamese,
//           totalTime: Number(row.TotalTime) || 0,
//           totalCredits: Number(row.TotalCredits) || 0,
//         }));

//       // Upsert subjects by subjectCode
//       const bulkOps = subjects.map((s) => ({
//         updateOne: {
//           filter: { subjectCode: s.subjectCode },
//           update: { $set: s },
//           upsert: true,
//         },
//       }));

//       if (bulkOps.length > 0) await Subject.bulkWrite(bulkOps);

//       res.json({ imported: subjects.length });
//     } catch (err) {
//       res.status(500).json({ message: "Import failed", error: err.message });
//     }
//   }
// );

router.get("/", requireAuth, async (req, res) => {
  const department = req.user.department;
  // Lấy lecturers kèm subjects
  const lecturers = await Lecturer.aggregate([
    {
      $lookup: {
        from: "lecturersubjects",
        localField: "lecturerId",
        foreignField: "lecturerId",
        as: "subjects",
      },
    },
    {
      $project: {
        lecturerId: 1,
        lecturerAccount: 1,
        lecturerName: 1,
        department: 1,
        subjects: 1,
      },
    },
  ]);

  // Group subjects theo major ở Node.js
  let result = lecturers.map((l) => {
    const majors = {};
    for (const s of l.subjects || []) {
      if (!s.major) continue;
      if (department && s.major !== department) continue;
      if (!majors[s.major]) majors[s.major] = [];
      majors[s.major].push({
        subjectCode: s.subjectCode,
        subjectName: s.subjectName,
        term: s.term,
        numberOfClasses: s.numberOfClasses,
        totalSlots: s.totalSlots,
      });
    }
    return {
      lecturerId: l.lecturerId,
      lecturerAccount: l.lecturerAccount,
      lecturerName: l.lecturerName,
      department: l.department,
      majors,
    };
  });

  if (department) {
    result = result.filter(
      (l) => l.majors[department] && l.majors[department].length > 0
    );
  }

  res.json(result);
});

router.post("/", requireAuth, async (req, res) => {
  const { lecturerId, majors } = req.body || {};
  if (!lecturerId || !majors || typeof majors !== "object")
    return res.status(400).json({ message: "Missing or invalid data" });

  const lecturer = await Lecturer.findOne({ lecturerId });
  if (!lecturer) return res.status(404).json({ message: "Lecturer not found" });

  let created = 0;
  for (const [major, subjects] of Object.entries(majors)) {
    if (!Array.isArray(subjects)) continue;
    for (const subject of subjects) {
      const { subjectCode, subjectName, term, numberOfClasses, totalSlots } =
        subject;

      const existing = await LecturerSubject.findOne({
        lecturerId,
        subjectCode,
      });
      if (existing) continue; // Skip if already exists

      await LecturerSubject.create({
        lecturerId,
        subjectCode,
        subjectName,
        lecturerName: lecturer.lecturerName,
        major,
        term,
        numberOfClasses,
        totalSlots,
        lecturer: lecturer._id,
      });
      created++;
    }
  }

  res.status(201).json({ created });
});

router.put("/", requireAuth, async (req, res) => {
  const { lecturerId, majors } = req.body || {};
  if (!lecturerId || !majors || typeof majors !== "object")
    return res.status(400).json({ message: "Missing or invalid data" });

  const lecturer = await Lecturer.findOne({ lecturerId });
  if (!lecturer) return res.status(404).json({ message: "Lecturer not found" });

  // Xóa tất cả LecturerSubject cũ của lecturer này
  await LecturerSubject.deleteMany({ lecturerId });

  let created = 0;
  for (const [major, subjects] of Object.entries(majors)) {
    if (!Array.isArray(subjects)) continue;
    for (const subject of subjects) {
      const { subjectCode, subjectName, term, numberOfClasses, totalSlots } =
        subject;

      await LecturerSubject.create({
        lecturerId,
        subjectCode,
        subjectName,
        lecturerName: lecturer.lecturerName,
        major,
        term,
        numberOfClasses,
        totalSlots,
        lecturer: lecturer._id,
      });
      created++;
    }
  }

  res.status(200).json({ updated: created });
});

router.delete("/:lecturerId", requireAuth, async (req, res) => {
  const { lecturerId } = req.params;
  if (!lecturerId)
    return res.status(400).json({ message: "Missing lecturerId" });

  await LecturerSubject.deleteMany({ lecturerId });

  res.json({ ok: true });
});

router.get("/export", requireAuth, async (req, res) => {
  try {
    const department = req.user.department;

    const query = {};
    if (department) {
      query.major = department;
    }
    const lecturerSubjects = await LecturerSubject.find(query).lean();

    const workbook = new ExcelJS.Workbook();
    const worksheet = workbook.addWorksheet("LecturerSubjects");

    worksheet.columns = [
      { header: "MAGV", key: "lecturerId", width: 15 },
      { header: "GIANGVIEN", key: "lecturerName", width: 30 },
      { header: "MAMH", key: "subjectCode", width: 15 },
      { header: "TENMH", key: "subjectName", width: 30 },
      { header: "NGANH", key: "major", width: 15 },
      { header: "KY", key: "term", width: 10 },
      { header: "SLL", key: "numberOfClasses", width: 10 },
      { header: "TONGSLOT", key: "totalSlots", width: 15 },
      { header: "NGAYKHOITAO", key: "createdAt", width: 30 },
      { header: "NGAYCAPNHAT", key: "updatedAt", width: 30 }
    ];

    lecturerSubjects.forEach((row) => {
      worksheet.addRow({
        lecturerId: row.lecturerId,
        lecturerName: row.lecturerName,
        subjectCode: row.subjectCode,
        subjectName: row.subjectName,
        major: row.major,
        term: row.term,
        numberOfClasses: row.numberOfClasses,
        totalSlots: row.totalSlots,
        createdAt:  row.createdAt
          ? new Date(row.createdAt).toLocaleString("vi-VN")
          : "",
        updatedAt: row.updatedAt
          ? new Date(row.updatedAt).toLocaleString("vi-VN")
          : "",
      });
    });

    res.setHeader(
      "Content-Type",
      "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
    );
    res.setHeader(
      "Content-Disposition",
      "attachment; filename=lecturers.xlsx"
    );

    await workbook.xlsx.write(res);
    res.end();
  } catch (err) {
    console.error("Export error:", err);
    res.status(500).json({ error: "Failed to export lecturers" });
  }
});

module.exports = router;
