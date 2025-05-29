using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SchedulerWpfApp.Model;
using Syncfusion.XlsIO;

namespace SchedulerWpfApp.Services
{
    public class ExcelLectureExporter : IExcelLectureExporter
    {
        public void ExportToExcel(List<Lecturer> lectures, string filePath)
        {
            using ExcelEngine excelEngine = new();
            IApplication application = excelEngine.Excel;
            application.DefaultVersion = ExcelVersion.Xlsx;

            IWorkbook workbook = application.Workbooks.Create(1);
            IWorksheet sheet = workbook.Worksheets[0];

            // Header
            sheet[1, 1].Text = "LecturerId";
            sheet[1, 2].Text = "LecturerName";
            sheet[1, 3].Text = "Role";

            int row = 2;
            foreach (var lecture in lectures)
            {
                sheet[row, 1].Text = lecture.LecturerId ?? "";
                sheet[row, 2].Text = lecture.LecturerName ?? "";
                sheet[row, 3].Text = lecture.Role ?? "";
                row++;
            }
            workbook.SaveAs(filePath);
        }
    }
}
