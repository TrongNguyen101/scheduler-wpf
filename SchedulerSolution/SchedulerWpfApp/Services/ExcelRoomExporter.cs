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
            sheet[1, 1].Text = "Room";
            sheet[1, 2].Text = "SLSV";
            sheet[1, 3].Text = "TypeOfRoom";

            int row = 2;
            foreach (var rooms in room)
            {
                sheet[row, 1].Text = rooms.RoomName ?? "";
                sheet[row, 2].Number = rooms.TotalPersons ?? 0;
                sheet[row, 3].Text = rooms.TypeOfRoom ?? "";
                row++;
            }

            workbook.SaveAs(filePath);
        }
    }
}
