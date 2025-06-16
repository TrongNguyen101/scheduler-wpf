using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Repository.LectureSubjectRepository
{
    public interface ILectureSubjectRepository: IBaseRepository<LecturerSubject>
    {
        Task<LecturerSubject> GetLectureSubjectByCodeAsync(int groupnameid);
    }
}

