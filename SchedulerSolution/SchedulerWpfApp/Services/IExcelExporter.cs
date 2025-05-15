using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Services
{
    public interface IExcelPersonExporter
    {
        void ExportToExcel(List<Person> persons, string filePath);
        void ExportToExcelRoom(List<GroupName> groupname, string filePath);

    }
}
