using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Services
{
    public interface ISubjectServices
    {
        Task<List<Subject>> GetAllAsync();
        Task<Subject?> GetBySubjectCodeAsync(string subjectCode);
        Task AddSubject(Subject subject);
        Task UpdateSubject(Subject subject);
        Task DeleteSubject(string subjectCode);
        Task ImportSubjectFromExcel(List<Subject> listSubjectFromExcel);
    }
}
