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
        public async Task<Lecturer?> GetByLectureCodeAsync(string LecturerId)
        {
            return await _context.Lecturers.FirstOrDefaultAsync(l => l.LecturerId == LecturerId);
        }

        /// <summary>
        /// Deletes a lecture from the database by lecturerId.
        /// </summary>
        /// <param name="LecturerId"></param>
        /// <returns></returns>
        public async Task DeleteLecture(string LecturerId)
        {
            try
            {
                var lecturer = await GetByLectureCodeAsync(LecturerId);
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
        #endregion
    }
}
