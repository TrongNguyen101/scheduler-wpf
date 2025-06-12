using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.ServiceRefactor.SubjectServices
{
    public interface ISubjectServices : IBaseService<Subject>
    {
        //Task<Subject?> GetSubjectByCodeAsync(string code);
    }
}
