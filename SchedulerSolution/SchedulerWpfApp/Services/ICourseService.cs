using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Services
{
    public interface ICourseService
    {
        Task<List<Subject>> GetAllAsync();
        Task<Subject?> GetBySubjectCodeAsync(string subjectCode);
        Task AddSubject(Subject subject);
        Task UpdateSubject(Subject subject);
        Task DeleteSubject(string subjectCode);
        Task ImportSubjectFromExcel(List<Subject> listSubjectFromExcel);
    }
}
