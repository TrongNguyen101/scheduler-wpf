using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Repository.LecturerSubjectRepository
{
    public interface ILecturerSubjectRepository: IBaseRepository<LecturerSubject>
    {
        Task<LecturerSubject> GetLecturerSubjectByIdAsync(int lectureSubjectId);
    }
}

