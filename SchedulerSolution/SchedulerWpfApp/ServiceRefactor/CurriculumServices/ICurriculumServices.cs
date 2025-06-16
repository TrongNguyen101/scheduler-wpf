using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.ServiceRefactor.CurriculumServices
{
    public interface ICurriculumServices
    {
        Task<List<Curriculum>> GetAllCurriculumAsync();
        Task ImportCurriculumFromExcel(List<Curriculum> listCurriculumFromExcel);
        List<Curriculum> ReadCurriculumsFromExcel(string filePath);
    }
}
