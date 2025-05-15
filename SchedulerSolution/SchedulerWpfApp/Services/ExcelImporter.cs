using SchedulerWpfApp.Model;
using Syncfusion.XlsIO;

namespace SchedulerWpfApp.Services
{
    public class ExcelPersonImporter : IExcelPersonImporter
    {
        public List<Person> ReadPersonsFromExcel(string filePath)
        {
            var persons = new List<Person>();

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

            string[] requiredHeaders = { "FirstName", "LastName", "Email", "Phone" };
            foreach (var h in requiredHeaders)
                if (!headerMap.ContainsKey(h))
                    throw new Exception($"Missing required column: {h}");

            for (int r = 2; r <= rowCount; r++)
            {
                var person = new Person
                {
                    FirstName = sheet[r, headerMap["FirstName"]].Value,
                    LastName = sheet[r, headerMap["LastName"]].Value,
                    Email = sheet[r, headerMap["Email"]].Value,
                    Phone = sheet[r, headerMap["Phone"]].Value
                };

                persons.Add(person);
            }

            return persons;
        }


        public List<GroupName> ReadRoomFromExcel(string filePath)
        {
            var rooms = new List<GroupName>();

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

            string[] requiredHeaders = { "Major", "Category", "NumberOfStudents", "NumberOfScheduler" };
            foreach (var h in requiredHeaders)
                if (!headerMap.ContainsKey(h))
                    throw new Exception($"Missing required column: {h}");

            for (int r = 2; r <= rowCount; r++)
            {
                var room = new GroupName
                {
                    ClassId = sheet[r, headerMap["ClassId"]].Value,
                    Major = sheet[r, headerMap["Major"]].Value,
                    Category = sheet[r, headerMap["Category"]].Value,
                    NumberOfStudents = int.TryParse(sheet[r, headerMap["NumberOfStudents"]]?.Value?.ToString(), out int students) ? students : 0,
                    NumberOfScheduler = int.TryParse(sheet[r, headerMap["NumberOfScheduler"]]?.Value?.ToString(), out int scheduler) ? scheduler : 0,
                };

                rooms.Add(room);
            }

            return rooms;
        }
    }
}
