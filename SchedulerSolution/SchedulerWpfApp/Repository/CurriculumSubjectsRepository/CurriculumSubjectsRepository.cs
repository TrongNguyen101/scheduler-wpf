using Microsoft.EntityFrameworkCore;
using SchedulerWpfApp.Data;
using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Repository.CurriculumSubjectsRepository
{
    public class CurriculumSubjectsRepository : BaseRepository<CurriculumSubject>, ICurriculumSubjectsRepository
    {
        public CurriculumSubjectsRepository(DataContext context) : base(context) { }

        public async Task<bool> IsDuplicateData(string CurriculumCode)
        {
            try
            {
                return await _context.CurriculumSubjects.AnyAsync(cs => cs.CurriculumCode == CurriculumCode);
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while checking for duplicate data", ex);
            }
        }
    }
}
