using Microsoft.EntityFrameworkCore;
using SchedulerWpfApp.Data;
using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Repository.LectureSubjectRepository
{
    public class LectureSubjectRepository : BaseRepository<LecturerSubject>, ILectureSubjectRepository
    {
        #region Contracstor
        public LectureSubjectRepository(DataContext context) : base(context) { }
        #endregion

        #region Methods
        public async Task<LecturerSubject> GetLectureSubjectByIdAsync(int lectureSubjectId)
        {
            try
            {
                return await _context.LecturerSubjects.FirstOrDefaultAsync(ls => ls.Id == lectureSubjectId);
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while retrieving the GroupClass by code.", ex);
            }
        }
        #endregion
    }

}
