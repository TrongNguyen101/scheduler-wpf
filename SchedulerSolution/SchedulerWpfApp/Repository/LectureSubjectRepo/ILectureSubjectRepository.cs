using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Repository.LectureSubjectRepo
{
    public interface ILectureSubjectRepository: IBaseRepository<LecturerSubject>
    {
        Task<LecturerSubject> GetLectureSubjectByIdAsync(int lecturesubjectid);
    }
}

