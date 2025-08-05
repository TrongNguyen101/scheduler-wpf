using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.ServiceRefactor.CurriculumSubjectServices
{
    public interface ICurriculumSubjectServices
    {
        Task<List<CurriculumSubject>> GetAllCurriculumSubjectAsync();
        Task<List<string>> GetSubjectCodeByCurriculumCodeAsync(string curriculumCode);
        Task<CurriculumSubject?> GetByIdAsync(int id);
        Task AddCurriculumSubject(CurriculumSubject curriculumSubject);
        Task UpdateCurriculumSubject(CurriculumSubject curriculumSubject);
        Task DeleteCurriculumSubject(int id);
        Task ImportCurriculumSubjectFromExcel(List<CurriculumSubject> listCurriculumFromExcel, IProgress<int> progress);
        void ExportToExcel(List<CurriculumSubject> curriculumSubjects, string filePath);
        List<CurriculumSubject> ReadCurriculumSubjectsFromExcel(string filePath);
    }
}
