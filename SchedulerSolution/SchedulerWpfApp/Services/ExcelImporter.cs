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


        public List<GroupClass> ReadRoomFromExcel(string filePath)
        {
            var rooms = new List<GroupClass>();

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

            string[] requiredHeaders = { "Groupname", "CurriculumCode", "Term", "Department", "Major" };
            foreach (var h in requiredHeaders)
                if (!headerMap.ContainsKey(h))
                    throw new Exception($"Missing required column: {h}");

            for (int r = 2; r <= rowCount; r++)
            {
                var room = new GroupClass
                {
                    GroupName = sheet[r, headerMap["Groupname"]].Value,
                    CurriculumCode = sheet[r, headerMap["CurriculumCode"]].Value,
                    Department = sheet[r, headerMap["Department"]].Value,
                    Major = sheet[r, headerMap["Major"]].Value,
                    Term = sheet[r, headerMap["Term"]].Value,
                };

                rooms.Add(room);
            }

            return rooms;
        }
    }
}
