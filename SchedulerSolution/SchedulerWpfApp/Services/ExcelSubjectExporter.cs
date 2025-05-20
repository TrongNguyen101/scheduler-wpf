using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SchedulerWpfApp.Model;
using Syncfusion.XlsIO;

namespace SchedulerWpfApp.Services
{
    public class ExcelSubjectExporter : IExcelSubjectExporter
    {
        public void ExportToExcel(List<Subject> subjects, string filePath)
        {
            using ExcelEngine excelEngine = new();
            IApplication application = excelEngine.Excel;
            application.DefaultVersion = ExcelVersion.Xlsx;

            IWorkbook workbook = application.Workbooks.Create(1);
            IWorksheet sheet = workbook.Worksheets[0];

            // Header
            sheet[1, 1].Text = "SubjectCode";
            sheet[1, 2].Text = "SubjectName";
            sheet[1, 3].Text = "Major";
            sheet[1, 4].Text = "TotalSessions";
            sheet[1, 5].Text = "SlotsPerWeek";
            sheet[1, 6].Text = "SemesterId";

            int row = 2;
            foreach (var subject in subjects)
            {
                sheet[row, 1].Text = subject.SubjectCode ?? "";
                sheet[row, 2].Text = subject.SubjectName ?? "";
                sheet[row, 3].Text = subject.Major ?? "";
                sheet[row, 4].Text = subject.TotalSessions.ToString() ?? "";
                sheet[row, 5].Text = subject.SlotsPerWeek.ToString() ?? "";
                sheet[row, 6].Text = subject.SemesterId ?? "";
                row++;
            }

            workbook.SaveAs(filePath);
        }
    }
}
