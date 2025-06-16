using SchedulerWpfApp.Model;
using Syncfusion.XlsIO;

namespace SchedulerWpfApp.Services
{
    public class ExcelSubjectImporter : IExcelSubjectImporter
    {
        public List<Subject> ReadSubjectsFromExcel(string filePath)
        {
            var subjects = new List<Subject>();

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

            string[] requiredHeaders = { "SubjectCode", "SubjectNameEnglish", "SubjectNameVietnamese", "TotalTime", "TotalCredits" };
            foreach (var h in requiredHeaders)
                if (!headerMap.ContainsKey(h))
                    throw new Exception($"Missing required column: {h}");

            for (int r = 2; r <= rowCount; r++)
            {
                bool isEmptyRow = requiredHeaders.All(h => string.IsNullOrWhiteSpace(sheet[r, headerMap[h]].Value));
                if (isEmptyRow)
                    continue;
                var subject = new Subject
                {
                    SubjectCode = sheet[r, headerMap["SubjectCode"]].Value,
                    SubjectNameEnglish = sheet[r, headerMap["SubjectNameEnglish"]].Value,
                    SubjectNameVietnamese = sheet[r, headerMap["SubjectNameVietnamese"]].Value,
                    TotalTime = int.TryParse(sheet[r, headerMap["TotalTime"]].Value, out int totalSessions) ? totalSessions : 0,
                    TotalCredits = int.TryParse(sheet[r, headerMap["TotalCredits"]].Value, out int SlotsPerWeek) ? SlotsPerWeek : 0
                };

                subjects.Add(subject);
            }

            return subjects;
        }
    }
}
