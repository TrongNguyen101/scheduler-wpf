using SchedulerWpfApp.Model;
using Syncfusion.XlsIO;

namespace SchedulerWpfApp.Services
{
    public class ExcelRoomImport : IExcelRoomImport
    {
        public List<Room> ReadRoomListFromExcel(string filePath)
        {
            var rooms = new List<Room>();

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

            string[] requiredHeaders = { "Room", "SLSV", "TypeOfRoom"};
            foreach (var h in requiredHeaders)
                if (!headerMap.ContainsKey(h))
                    throw new Exception($"Missing required column: {h}");

            for (int r = 2; r <= rowCount; r++)
            {
                var room = new Room
                {
                    //RoomName = sheet[r, headerMap["Room"]].Value,
                    //TotalPersons = int.TryParse(sheet[r, headerMap["SLSV"]].Value, out var persons) ? persons : null,
                    TypeOfRoom = sheet[r, headerMap["TypeOfRoom"]].Value,
                };

                rooms.Add(room);
            }
            return rooms;
        }

    }
}
