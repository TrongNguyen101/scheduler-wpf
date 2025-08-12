using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Repository.ScheduleRepository
{
    public interface IScheduleRepository: IBaseRepository<Schedule>
    {
        Task<bool> AddScheduleAsync(Schedule schedules);
        Task DeleteAllAsync(int schedulerId);
        Task ResetIdentitySchedulesAsync();
    }
}
