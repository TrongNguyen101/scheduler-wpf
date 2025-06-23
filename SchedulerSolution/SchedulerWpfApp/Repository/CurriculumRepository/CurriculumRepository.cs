using Microsoft.EntityFrameworkCore;
using SchedulerWpfApp.Data;
using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Repository.CurriculumRepository
{
    /// <summary>
    /// Repository for managing Curriculum entities.
    /// Provides methods for retrieving, deleting, and checking existence of curriculums.
    /// </summary>
    public class CurriculumRepository : BaseRepository<Curriculum>, ICurriculumRepository
    {
        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="CurriculumRepository"/> class.
        /// </summary>
        /// <param name="context">The database context to use for operations.</param>
        public CurriculumRepository(DataContext context) : base(context) { }
        #endregion

        #region Methods

        /// <summary>
        /// Retrieves a curriculum by its curriculum code.
        /// </summary>
        /// <param name="curriculumCode">The code of the curriculum to retrieve.</param>
        /// <returns>The curriculum with the specified code, or null if not found.</returns>
        public async Task<Curriculum> GetByCurriculumCodeAsync(string curriculumCode)
        {
            return await _context.Curriculums.FirstOrDefaultAsync(c => c.CurriculumCode == curriculumCode);
        }

        /// <summary>
        /// Deletes a curriculum by its curriculum code.
        /// </summary>
        /// <param name="curriculumCode">The code of the curriculum to delete.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task DeleteCurriculum(string curriculumCode)
        {
            var curriculum = await GetByCurriculumCodeAsync(curriculumCode);
            if (curriculum != null)
            {
                _context.Curriculums.Remove(curriculum);
            }
        }

        /// <summary>
        /// Checks if a curriculum code exists in the database (case-insensitive).
        /// </summary>
        /// <param name="curriculumCode">The curriculum code to check.</param>
        /// <returns>True if the curriculum code exists, otherwise false.</returns>
        public Task<bool> CheckCurriculumCodeExistsAsync(string curriculumCode)
        {
            return _context.Curriculums
                 .AnyAsync(c => c.CurriculumCode.ToLower() == curriculumCode.ToLower());
        }
        #endregion
    }
}
