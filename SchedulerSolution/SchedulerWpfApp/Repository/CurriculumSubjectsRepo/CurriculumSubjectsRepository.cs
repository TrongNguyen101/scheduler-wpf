using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SchedulerWpfApp.Data;
using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Repository.CurriculumSubjectsRepo
{
    public class CurriculumSubjectsRepository : BaseRepository<CurriculumSubject>, ICurriculumSubjectsRepository
    {
        public CurriculumSubjectsRepository(DataContext context) : base(context) { }
    }
}
