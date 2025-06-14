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
            sheet[1, 2].Text = "SubjectNameEnglish";
            sheet[1, 3].Text = "SubjectNameVietnamese";
            sheet[1, 4].Text = "TotalTime";
            sheet[1, 5].Text = "TotalCredits";

            int row = 2;
            foreach (var subject in subjects)
            {
                sheet[row, 1].Text = subject.SubjectCode ?? "";
                sheet[row, 2].Text = subject.SubjectNameEnglish ?? "";
                sheet[row, 3].Text = subject.SubjectNameVietnamese ?? "";
                sheet[row, 4].Text = subject.TotalTime.ToString() ?? "";
                sheet[row, 5].Text = subject.TotalCredits.ToString() ?? "";
                row++;
            }

            workbook.SaveAs(filePath);
        }
    }
}
