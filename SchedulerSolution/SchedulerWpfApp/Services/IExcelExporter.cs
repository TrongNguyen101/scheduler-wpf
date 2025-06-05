using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Services
{
    public interface IExcelPersonExporter
    {
        void ExportToExcel(List<Person> persons, string filePath);
        void ExportToExcelRoom(List<GroupClass> groupname, string filePath);

    }
}
