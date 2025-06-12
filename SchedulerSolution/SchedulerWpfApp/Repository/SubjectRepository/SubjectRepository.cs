using Microsoft.EntityFrameworkCore;
using SchedulerWpfApp.Data;
using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Repository.SubjectRepository
{
    public class SubjectRepository : BaseRepository<Subject>, ISubjectRepository
    {
        #region Constructors
        /// <summary>
        /// Constructor for SubjectRepository that initializes the base repository with the provided DataContext.
        /// </summary>
        /// <param name="context"></param>
        public SubjectRepository(DataContext context) : base(context) { }
        #endregion

        #region Methods
        /// <summary>
        /// Retrieves a subject by code.
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public async Task<Subject?> GetSubjectByCodeAsync(string code)
        {
            return await _context.Subjects.FirstOrDefaultAsync(s => s.SubjectCode == code);
        }

        /// <summary>
        /// Deletes a subject by code.
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public async Task DeleteAsync(string code)
        {
            try
            {
                var subject = await GetSubjectByCodeAsync(code);
                if (subject != null)
                {
                    _context.Subjects.Remove(subject);
                }
            }
            catch (Exception ex)
            {
                // Handle delete subject failure
                throw new Exception("An error occurred while deleting the subject.", ex);
            }
        }
        #endregion

    }
}
