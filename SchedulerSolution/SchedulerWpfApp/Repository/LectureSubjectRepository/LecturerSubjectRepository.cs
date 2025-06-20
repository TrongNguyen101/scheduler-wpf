using Microsoft.EntityFrameworkCore;
using SchedulerWpfApp.Data;
using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Repository.LecturerSubjectRepository
{
    public class LecturerSubjectRepository : BaseRepository<LecturerSubject>, ILecturerSubjectRepository
    {
        #region Contracstor
        public LecturerSubjectRepository(DataContext context) : base(context) { }
        #endregion

        #region Methods
        public async Task<LecturerSubject> GetLecturerSubjectByIdAsync(int lectureSubjectId)
        {
            try
            {
                return await _context.LecturerSubjects.FirstOrDefaultAsync(ls => ls.Id == lectureSubjectId);
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while retrieving the LecturerSubject by code.", ex);
            }
        }
        #endregion
    }

}
