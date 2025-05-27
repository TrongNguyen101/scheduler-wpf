using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SchedulerWpfApp.Model;
using Syncfusion.XlsIO;

namespace SchedulerWpfApp.Services
{
    public class ExcelLectureImporter : IExcelLectureImporter
    {
        public List<Lecturer> ReadLecturesFromExcel(string filePath)
        {
            var lectures = new List<Lecturer>();

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

            string[] requiredHeaders = { "LecturerId", "LecturerName", "Role" };
            foreach (var h in requiredHeaders)
                if (!headerMap.ContainsKey(h))
                    throw new Exception($"Missing required column: {h}");

            for (int r = 2; r <= rowCount; r++)
            {
                var lecture = new Lecturer
                {
                    LecturerId = sheet[r, headerMap["LecturerId"]].Value,
                    LecturerName = sheet[r, headerMap["LecturerName"]].Value,
                    Role = sheet[r, headerMap["Role"]].Value
                };

                lectures.Add(lecture);
            }

            return lectures;
        }
    }
}
