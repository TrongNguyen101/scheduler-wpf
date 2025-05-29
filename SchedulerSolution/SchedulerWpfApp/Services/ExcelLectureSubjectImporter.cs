using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SchedulerWpfApp.Model;
using Syncfusion.XlsIO;

namespace SchedulerWpfApp.Services
{
    public class ExcelLectureSubjectImporter:IExcelLectureSubjectImporter
    {
        public List<LecturerSubject> ReadLectureSubjectFromExcel(string filePath)
        {
            var lectures = new List<LecturerSubject>();

            using ExcelEngine excelEngine = new();
            var app = excelEngine.Excel;
            app.DefaultVersion = ExcelVersion.Xlsx;

            var workbook = app.Workbooks.Open(filePath);
            var sheet = workbook.Worksheets[0];

            int rowCount = sheet.UsedRange.LastRow;
            int colCount = sheet.UsedRange.LastColumn;

            Dictionary<string, int> headerMap = new();
            for (int c = 1; c <= colCount; c++)
            {
                string header = sheet[1, c].Value?.Trim() ?? "";
                if (!string.IsNullOrWhiteSpace(header))
                    headerMap[header] = c;
            }

            string[] requiredHeaders = { "LecturerId", "LecturerName", "SubjectCode", "NumberOfClasses" };
            foreach (var h in requiredHeaders)
                if (!headerMap.ContainsKey(h))
                    throw new Exception($"Missing required column: {h}");

            for (int r = 2; r <= rowCount; r++)
            {
                var lecturesubject = new LecturerSubject
                {
                    LecturerId = sheet[r, headerMap["LecturerId"]].Value,
                    LecturerName = sheet[r, headerMap["LecturerName"]].Value,
                    SubjectCode = sheet[r, headerMap["SubjectCode"]].Value,
                    NumberOfClasses = int.TryParse(sheet[r, headerMap["NumberOfClasses"]].Value, out int numberOfClasses) ? numberOfClasses : 0
                };

                lectures.Add(lecturesubject);
            }
            return lectures;
        }
    }
}
