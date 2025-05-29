using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SchedulerWpfApp.Model;
using Syncfusion.XlsIO;

namespace SchedulerWpfApp.Services
{
    public class ExcelLectureSubjectExporter:IExcelLectureSubjectExporter
    {
        public void ExportToExcel(List<LecturerSubject> lecturesubjects, string filePath)
        {
            using ExcelEngine excelEngine = new();
            IApplication application = excelEngine.Excel;
            application.DefaultVersion = ExcelVersion.Xlsx;

            IWorkbook workbook = application.Workbooks.Create(1);
            IWorksheet sheet = workbook.Worksheets[0];

            // Header
            sheet[1, 1].Text = "LecturerId";
            sheet[1, 2].Text = "LecturerName";
            sheet[1, 3].Text = "SubjectCode";
            sheet[1, 4].Text = "NumberOfClasses";

            int row = 2;
            foreach (var lecturesubject in lecturesubjects)
            {
                sheet[row, 1].Text = lecturesubject.LecturerId ?? "";
                sheet[row, 2].Text = lecturesubject.LecturerName ?? "";
                sheet[row, 3].Text = lecturesubject.SubjectCode ?? "";
                sheet[row, 4].Number = lecturesubject.NumberOfClasses;
                row++;
            }
            workbook.SaveAs(filePath);
        }
    }
}
