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
        public async Task<LecturerSubject> GetLecturerSubjectByIdAsync(int lecturerSubjectId)
        {
            try
            {
                return await _context.LecturerSubjects.FirstOrDefaultAsync(ls => ls.Id == lecturerSubjectId);
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while retrieving the LecturerSubject by code.", ex);
            }
        }

        public Task<bool> CheckLecturerSubjectExits(LecturerSubject lecturerSubject)
        {
            try
            {
                return _context.LecturerSubjects
              .AnyAsync(ls => ls.LecturerId == lecturerSubject.LecturerId &&
                              ls.SubjectCode == lecturerSubject.SubjectCode &&
                              ls.Term == lecturerSubject.Term);
            }
            catch (Exception ex)
            {
                throw new Exception("Đã xảy ra lỗi khi kiểm tra xem LecturerSubject có tồn tại không.", ex);
            }
        }

        public async Task<LecturerSubject?> GetLecturerSubjectAsync(LecturerSubject lecturerSubject)
        {
            try
            {
                return await _context.LecturerSubjects
                    .FirstOrDefaultAsync(ls =>
                        ls.LecturerId == lecturerSubject.LecturerId &&
                        ls.SubjectCode == lecturerSubject.SubjectCode &&
                        ls.Term == lecturerSubject.Term &&
                        ls.Id != lecturerSubject.Id);
            }
            catch (Exception ex)
            {
                throw new Exception("Đã xảy ra lỗi khi kiểm tra bản ghi LecturerSubject trùng.", ex);
            }
        }

        #endregion
    }
}
