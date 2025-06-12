using SchedulerWpfApp.Data;
using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Repository.LectureSubjectRepo
{
    public class LectureSubjectRepository : BaseRepository<LecturerSubject>, ILectureSubjectRepository
    {
        public LectureSubjectRepository(DataContext context) : base(context) { }
    }
}
