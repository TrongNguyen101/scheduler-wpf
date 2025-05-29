using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Services
{
    public interface IExcelLectureImporter
    {
        List<Lecturer> ReadLecturesFromExcel(string filePath);
    }
}
