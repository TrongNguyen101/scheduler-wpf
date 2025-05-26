using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Services.LecturerSubjectServices
{
    public interface InterfaceLecturerServices
    {
        Task<List<Lecturer>> GetAllLecturerAsync();
    }
}
