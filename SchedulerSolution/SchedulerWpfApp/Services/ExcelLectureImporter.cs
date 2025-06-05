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

            string[] requiredHeaders = { "MaNV", "Fullname", "Bomon", "LoaiGV" };
            foreach (var h in requiredHeaders)
                if (!headerMap.ContainsKey(h))
                    throw new Exception($"Missing required column: {h}");

            for (int r = 2; r <= rowCount; r++)
            {
                bool isEmptyRow = requiredHeaders.All(h => string.IsNullOrWhiteSpace(sheet[r, headerMap[h]].Value));
                if (isEmptyRow)
                    continue;
                var lecture = new Lecturer
                {
                    LecturerId = sheet[r, headerMap["MaNV"]].Value,
                    LecturerName = sheet[r, headerMap["Fullname"]].Value,
                    //Major = sheet[r, headerMap["Bomon"]].Value,
                    Role = sheet[r, headerMap["LoaiGV"]].Value
                };

                lectures.Add(lecture);
            }
            return lectures;
        }
    }
}
