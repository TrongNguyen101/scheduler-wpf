using Microsoft.EntityFrameworkCore;
using SchedulerWpfApp.Data;
using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Repository.CurriculumRepository
{
    public class CurriculumRepository : BaseRepository<Curriculum>, ICurriculumRepository
    {
        public CurriculumRepository(DataContext context) : base(context) { }

        public async Task<Curriculum> GetByCurriculumCodeAsync(string curriculumCode)
        {
            return await _context.Curriculums.FirstOrDefaultAsync(c => c.CurriculumCode == curriculumCode);
        }

        public async Task DeleteCurriculum(string curriculumCode)
        {
            var curriculum = await GetByCurriculumCodeAsync(curriculumCode);
            if (curriculum != null)
            {
                _context.Curriculums.Remove(curriculum);
            }
        }

        public async Task<bool> IsDuplicateData(string curriculumCode)
        {
           try
            {
                return await _context.Curriculums.AnyAsync(c => c.CurriculumCode == curriculumCode);
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while checking for duplicate data", ex);
            }
        }
    }
}
