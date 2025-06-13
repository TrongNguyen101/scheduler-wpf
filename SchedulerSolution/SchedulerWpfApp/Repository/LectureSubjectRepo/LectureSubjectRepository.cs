using Microsoft.EntityFrameworkCore;
using SchedulerWpfApp.Data;
using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Repository.LectureSubjectRepo
{
    public class LectureSubjectRepository : BaseRepository<LecturerSubject>, ILectureSubjectRepository
    {
        public LectureSubjectRepository(DataContext context) : base(context) { }

        public async Task<LecturerSubject> GetLectureSubjectByCodeAsync(int groupnameid)
        {
            try
            {
                return await _context.LecturerSubjects.FirstOrDefaultAsync(s => s.Id == groupnameid);
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while retrieving the GroupClass by code.", ex);
            }
        }
    }

}
