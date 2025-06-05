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
            string[] requiredHeaders = { "RoomId", "RoomName", "TypeOfRoom", "Floor", "Building", "TotalPersons", "Status" };
            foreach (var h in requiredHeaders)
                if (!headerMap.ContainsKey(h))
                    throw new Exception($"Missing required column: {h}");

            for (int r = 2; r <= rowCount; r++)
            {
                var room = new Room
                {
                    RoomId = int.TryParse(sheet[r, headerMap["RoomId"]].Value, out var roomId) ? roomId : 0,
                    Building = sheet[r, headerMap["Building"]].Value?.Trim(),
                    Floor = int.TryParse(sheet[r, headerMap["Floor"]].Value, out var floor) ? floor : 0,
                    RoomName = sheet[r, headerMap["RoomName"]].Value?.Trim(),
                    Status = sheet[r, headerMap["Status"]].Value?.Trim() ?? "available", // Default to "available" if not specified
                    TotalPersons = int.TryParse(sheet[r, headerMap["TotalPersons"]].Value, out var totalPersons) ? totalPersons : 0,
                    TypeOfRoom = sheet[r, headerMap["TypeOfRoom"]].Value,
                };
                rooms.Add(room);
            }
            return rooms;
        }

    }
}
