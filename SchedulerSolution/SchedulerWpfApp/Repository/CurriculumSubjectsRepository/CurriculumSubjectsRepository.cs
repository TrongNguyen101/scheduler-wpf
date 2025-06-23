using Microsoft.EntityFrameworkCore;
using SchedulerWpfApp.Data;
using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Repository.CurriculumSubjectsRepository
{
    /// <summary>
    /// Repository for managing CurriculumSubject entities.
    /// Inherits from BaseRepository and implements ICurriculumSubjectsRepository.
    /// </summary>
    public class CurriculumSubjectsRepository : BaseRepository<CurriculumSubject>, ICurriculumSubjectsRepository
    {
        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="CurriculumSubjectsRepository"/> class.
        /// </summary>
        /// <param name="context">The database context to use for operations.</param>
        public CurriculumSubjectsRepository(DataContext context) : base(context) { }
        #endregion

        #region Methods
        /// <summary>
        /// Checks asynchronously if a CurriculumSubject with the same CurriculumCode, SubjectCode, and TermNo exists.
        /// </summary>
        /// <param name="curriculumSubject">The CurriculumSubject to check for existence.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if the CurriculumSubject exists, otherwise false.</returns>
        public Task<bool> CheckCurriculumSubjectCodeExistsAsync(CurriculumSubject curriculumSubject)
        {
            try
            {
                return _context.CurriculumSubjects
                .AnyAsync(cs => cs.CurriculumCode.ToLower() == curriculumSubject.CurriculumCode.ToLower() &&
                                cs.SubjectCode.ToLower() == curriculumSubject.SubjectCode.ToLower() &&
                                cs.TermNo == curriculumSubject.TermNo);
            }
            catch (Exception ex)
            {
                throw new Exception("Đã xảy ra lỗi khi kiểm tra sự tồn tại của mã CurriculumSubject.", ex);
            }
            #endregion
        }
    }
}
