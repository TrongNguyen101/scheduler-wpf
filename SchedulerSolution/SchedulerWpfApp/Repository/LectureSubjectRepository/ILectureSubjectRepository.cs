using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Repository.LectureSubjectRepository
{
    public interface ILectureSubjectRepository: IBaseRepository<LecturerSubject>
    {
        Task<LecturerSubject> GetLectureSubjectByIdAsync(int lecturesubjectId);
    }
}

