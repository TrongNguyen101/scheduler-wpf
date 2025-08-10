using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Repository.LecturerSubjectRepository
{
    public interface ILecturerSubjectRepository: IBaseRepository<LecturerSubject>
    {
        Task<LecturerSubject> GetLecturerSubjectByIdAsync(int lecturerSubjectId);
        Task<List<LecturerSubject>> GetLecturerSubjectBySubjectCodeAsync(string subjectCode);
        Task<bool> CheckLecturerSubjectExits(LecturerSubject lecturerSubject);
        Task<LecturerSubject?> GetLecturerSubjectAsync(LecturerSubject lecturerSubject);
        Task<bool> CheckSubjectExits(string subjectCode);
        Task<bool> CheckLecturerExits(string lecturerCode);
    }
}

