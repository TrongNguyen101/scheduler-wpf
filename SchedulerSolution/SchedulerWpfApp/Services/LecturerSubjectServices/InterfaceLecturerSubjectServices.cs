

using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Services.LecturerSubjectServices
{
    public interface InterfaceLecturerSubjectServices
    {
        Task<List<LecturerSubject>> GetAllLecturerSubjectAsync();
    }
}
