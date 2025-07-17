using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.ServiceRefactor.CurriculumServices
{
    public interface ICurriculumServices
    {
        Task<List<Curriculum>> GetAllCurriculumAsync();
        Task<Curriculum?> GetByCurriculumCodeAsync(string curriculumCode);
        Task AddCurriculum(Curriculum curriculum);
        Task UpdateCurriculum(Curriculum curriculum);
        Task DeleteCurriculum(string curriculumCode);
        Task ImportCurriculumFromExcel(List<Curriculum> listCurriculumFromExcel, IProgress<int> progress);
        void ExportToExcel(List<Curriculum> curriculums, string filePath);
        List<Curriculum> ReadCurriculumsFromExcel(string filePath);
    }
}
