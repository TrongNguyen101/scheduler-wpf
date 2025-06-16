using SchedulerWpfApp.Data;
using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Repository.CurriculumSubjectsRepository
{
    public class CurriculumSubjectsRepository : BaseRepository<CurriculumSubject>, ICurriculumSubjectsRepository
    {
        public CurriculumSubjectsRepository(DataContext context) : base(context) { }
    }
}
