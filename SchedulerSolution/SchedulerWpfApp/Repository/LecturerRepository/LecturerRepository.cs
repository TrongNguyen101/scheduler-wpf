using Microsoft.EntityFrameworkCore;
using SchedulerWpfApp.Data;
using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Repository.LecturerRepository
{
    public class LecturerRepository : BaseRepository<Lecturer>, ILecturerRepository
    {
        #region Constructor
        /// <summary>
        /// Constructor for the LecturerRepository class.
        /// </summary>
        /// <param name="context"></param>
        public LecturerRepository(DataContext context) : base(context) { }
        #endregion

        #region Methods
        /// <summary>
        /// Retrieves a lecturer from the database by lecturerId.
        /// </summary>
        /// <param name="LecturerId"></param>
        /// <returns></returns>
        public async Task<Lecturer?> GetByLecturerCodeAsync(string LecturerId)
        {
            return await _context.Lecturers.FirstOrDefaultAsync(l => l.LecturerId == LecturerId);
        }

        /// <summary>
        /// Deletes a lecture from the database by lecturerId.
        /// </summary>
        /// <param name="LecturerId"></param>
        /// <returns></returns>
        public async Task DeleteLecturer(string LecturerId)
        {
            try
            {
                var lecturer = await GetByLecturerCodeAsync(LecturerId);
                if (lecturer != null)
                {
                    _context.Lecturers.Remove(lecturer);
                }
            }
            catch (Exception ex)
            {
                // Handle delete lecturer failure
                throw new Exception("Failed to delete lecturer", ex);
            }
        }

        public async Task<bool> CheckLecturerExistsAsync(string lecturerId)
        {
            try
            {
                return await _context.Lecturers
                .AnyAsync(l => l.LecturerId.ToLower() == lecturerId.ToLower());
            }
            catch (Exception ex)
            {
                throw new Exception("Đã xảy ra lỗi khi kiểm tra sự tồn tại của mã Lecturer", ex);
            }
            #endregion
        }
    }
}
