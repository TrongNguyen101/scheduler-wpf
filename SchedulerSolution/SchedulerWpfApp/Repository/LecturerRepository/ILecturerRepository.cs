using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Repository.LecturerRepository
{
    public interface ILecturerRepository : IBaseRepository<Lecturer>
    {
        Task<Lecturer?> GetByLecturerCodeAsync(string LecturerId);
        Task DeleteLecturer(string LecturerId);
    }
}
