using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Repository.CurriculumSubjectsRepository
{
    public interface ICurriculumSubjectsRepository: IBaseRepository<CurriculumSubject>
    { 
        Task<bool> CheckCurriculumSubjectCodeExistsAsync(CurriculumSubject curriculumSubject);
        Task<List<string>> GetSubjectCodeByCurriculumCode(string curriculumCode);
    }
}
