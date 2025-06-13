using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Repository.LectureSubjectRepo
{
    public interface ILectureSubjectRepository: IBaseRepository<LecturerSubject>
    {
        Task<LecturerSubject> GetLectureSubjectByCodeAsync(int groupnameid);
    }
}

