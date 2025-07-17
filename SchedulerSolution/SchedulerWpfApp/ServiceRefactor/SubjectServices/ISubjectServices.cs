using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.ServiceRefactor.SubjectServices
{
    public interface ISubjectServices
    {
        Task<List<Subject>> GetAllAsync();
        Task<Subject?> GetBySubjectCodeAsync(string subjectCode);
        Task AddSubject(Subject subject);
        Task UpdateSubject(Subject subject);
        Task DeleteSubject(string subjectCode);
        Task ImportSubjectFromExcel(List<Subject> listSubjectFromExcel, IProgress<int> progress);
        void ExportToExcel(List<Subject> subjects, string filePath);
        List<Subject> ReadSubjectsFromExcel(string filePath);
    }
}
