using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

            string[] requiredHeaders = { "SubjectCode", "SubjectName", "Major", "TotalSessions", "SlotsPerWeek", "SemesterId" };
            foreach (var h in requiredHeaders)
                if (!headerMap.ContainsKey(h))
                    throw new Exception($"Missing required column: {h}");

            for (int r = 2; r <= rowCount; r++)
            {
                var subject = new Subject
                {
                    SubjectCode = sheet[r, headerMap["SubjectCode"]].Value,
                    SubjectName = sheet[r, headerMap["SubjectName"]].Value,
                    Major = sheet[r, headerMap["Major"]].Value,
                    TotalSessions = int.TryParse(sheet[r, headerMap["TotalSessions"]].Value, out int totalSessions) ? totalSessions : 0,
                    SlotsPerWeek = int.TryParse(sheet[r, headerMap["SlotsPerWeek"]].Value, out int SlotsPerWeek) ? SlotsPerWeek : 0,
                     SemesterId = sheet[r, headerMap["SemesterId"]].Value
                };

                subjects.Add(subject);
            }

            return subjects;
        }
    }
}
