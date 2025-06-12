using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SchedulerWpfApp.Data;
using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Repository.CurriculumRepo
{
    public class CurriculumRepository:BaseRepository<Curriculum>, ICurriculumRepository
    {
        public CurriculumRepository(DataContext context) : base(context) { }
    }
}
