using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Repository.CurriculumSubjectsRepository
{
    public interface ICurriculumSubjectsRepository: IBaseRepository<CurriculumSubject>
    {
        Task<bool> IsDuplicateData(string CurriculumCode);
    }
}
