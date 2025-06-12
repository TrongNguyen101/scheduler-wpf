using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Repository.SubjectRepository
{
    public interface ISubjectRepository: IBaseRepository<Subject>
    {
        Task<Subject?> GetSubjectByCodeAsync(string code);
        Task DeleteAsync(string code);
    }
}
