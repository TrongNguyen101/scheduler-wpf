using SchedulerWpfApp.Model;
using Syncfusion.XlsIO;

namespace SchedulerWpfApp.Services
{
    public class ExcelRoomExporter : IExcelRoomExporter
    {
        public void ExportRoomToExcel(List<Room> room, string filePath)
        {
            using ExcelEngine excelEngine = new();
            IApplication application = excelEngine.Excel;
            application.DefaultVersion = ExcelVersion.Xlsx;

            IWorkbook workbook = application.Workbooks.Create(1);
            IWorksheet sheet = workbook.Worksheets[0];
            // Header
            sheet[1, 1].Text = "Phòng học";
            sheet[1, 2].Text = "RoomName";
            sheet[1, 3].Text = "Loại phòng";
            sheet[1, 4].Text = "Tầng";
            sheet[1, 5].Text = "Tòa";
            sheet[1, 6].Text = "SLSV";
            sheet[1, 7].Text = "Status";
            int row = 2;
            foreach (var rooms in room)
            {
                sheet[row, 1].Number = rooms.RoomId;
                sheet[row, 2].Text = rooms.RoomName ?? "";
                sheet[row, 3].Text = rooms.TypeOfRoom ?? "";
                sheet[row, 4].Number = rooms.Floor;
                sheet[row, 5].Text = rooms.Building ?? "";
                sheet[row, 6].Number = rooms.TotalPersons;
                sheet[row, 7].Text = rooms.Status ?? "";
                row++;
            }
            workbook.SaveAs(filePath);
        }
    }
}
