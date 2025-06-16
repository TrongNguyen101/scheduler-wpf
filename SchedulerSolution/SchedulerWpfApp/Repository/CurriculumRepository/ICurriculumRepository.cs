using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Repository.CurriculumRepository
{
   public interface ICurriculumRepository: IBaseRepository<Curriculum>
    {
        Task<Curriculum> GetByCurriculumCodeAsync(string curriculumCode);
        Task DeleteCurriculum(string curriculumCode);
    }
}
