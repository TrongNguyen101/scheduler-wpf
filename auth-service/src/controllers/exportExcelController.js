const ExcelJS = require("exceljs");
const { Lecturer } = require("../models");

async function exportLecturers(req, res) {
  try {
    const lecturers = await Lecturer.find().lean();

    const workbook = new ExcelJS.Workbook();
    const worksheet = workbook.addWorksheet("Lecturers");

    worksheet.columns = [
      { header: "MAGV", key: "lecturerId", width: 15 },
      { header: "GIANGVIEN", key: "lecturerName", width: 30 },
      { header: "MAMH", key: "subjectCode", width: 15 },
      { header: "TENMH", key: "subjectName", width: 30 },
      { header: "NGANH", key: "major", width: 10 },
      { header: "KY", key: "term", width: 10 },
      { header: "SLL", key: "numberOfClasses", width: 10 },
      { header: "TONGSLOT", key: "totalSlots", width: 15 },
    ];

    lecturers.forEach((lec) => {
      worksheet.addRow({
        lecturerId: lec.lecturerId,
        lecturerName: lec.lecturerName,
        subjectCode: lec.subjectCode,
        subjectName: lec.subjectName,
        major: lec.major,
        term: lec.term,
        numberOfClasses: lec.numberOfClasses,
        totalSlots: lec.totalSlots,
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
}

module.exports = { exportLecturers };
