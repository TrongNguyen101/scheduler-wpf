using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Repository.SubjectRepository
{
    public interface ISubjectRepository: IBaseRepository<Subject>
    {
        Task<Subject?> GetSubjectByCodeAsync(string code);
        Task DeleteAsync(string code);
        Task<bool> CheckSubjectCodeExistsAsync(string subjectCode);
    }
}
