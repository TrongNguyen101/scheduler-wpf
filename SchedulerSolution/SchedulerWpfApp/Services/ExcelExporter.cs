using Syncfusion.XlsIO;
using SchedulerWpfApp.Model;
using SchedulerWpfApp.Services;

public class ExcelPersonExporter : IExcelPersonExporter
{
    public void ExportToExcel(List<Person> persons, string filePath)
    {
        using ExcelEngine excelEngine = new();
        IApplication application = excelEngine.Excel;
        application.DefaultVersion = ExcelVersion.Xlsx;

        IWorkbook workbook = application.Workbooks.Create(1);
        IWorksheet sheet = workbook.Worksheets[0];

        // Header
        sheet[1, 1].Text = "FirstName";
        sheet[1, 2].Text = "LastName";
        sheet[1, 3].Text = "Email";
        sheet[1, 4].Text = "Phone";

        int row = 2;
        foreach (var person in persons)
        {
            sheet[row, 1].Text = person.FirstName ?? "";
            sheet[row, 2].Text = person.LastName ?? "";
            sheet[row, 3].Text = person.Email ?? "";
            sheet[row, 4].Text = person.Phone ?? "";
            row++;
        }

        workbook.SaveAs(filePath);
    }


    public void ExportToExcelGroupName(List<GroupClass> groupname, string filePath)
    {
        using ExcelEngine excelEngine = new();
        IApplication application = excelEngine.Excel;
        application.DefaultVersion = ExcelVersion.Xlsx;
        IWorkbook workbook = application.Workbooks.Create(1);
        IWorksheet sheet = workbook.Worksheets[0];
        // Header
        sheet[1, 1].Text = "GroupName";
        sheet[1, 2].Text = "Khóa";
        sheet[1, 3].Text = "Ngành";
        sheet[1, 4].Text = "BM";
        sheet[1, 5].Text = "Kỳ";
        int row = 2;
        foreach (var groupnames in groupname)
        {
            sheet[row, 1].Text = groupnames.GroupName ?? "";
            sheet[row, 2].Text = groupnames.CurriculumCode ?? "";
            sheet[row, 3].Text = groupnames.Major ?? "";
            sheet[row, 4].Text = groupnames.Department ?? "";
            sheet[row, 5].Text = groupnames.Term ?? "";
            //sheet[row, 5].Text = groupnames.Term.ToString();
            row++;
        }
        workbook.SaveAs(filePath);
    }
}
