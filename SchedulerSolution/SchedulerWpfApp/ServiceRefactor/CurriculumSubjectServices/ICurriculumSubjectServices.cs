using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.ServiceRefactor.CurriculumSubjectServices
{
    public interface ICurriculumSubjectServices
    {
        Task<List<CurriculumSubject>> GetAllCurriculumSubjectAsync();
        Task ImportCurriculumSubjectFromExcel(List<CurriculumSubject> listCurriculumFromExcel);
        List<CurriculumSubject> ReadCurriculumSubjectsFromExcel(string filePath);
    }
}
