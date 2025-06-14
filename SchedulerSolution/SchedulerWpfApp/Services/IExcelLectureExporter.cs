using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Services
{
    public interface IExcelLectureExporter
    {
        void ExportToExcel(List<Lecturer> lectures, string filePath);
    }
}
