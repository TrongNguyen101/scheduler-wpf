using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Repository.ScheduleRepository
{
    public interface IScheduleRepository: IBaseRepository<Schedule>
    {
        Task<bool> AddScheduleAsync(Schedule schedules);
        Task<bool> AddSlotAsync(Schedule schedule);
        Task DeleteAllAsync(int schedulerId);
        Task DeleteAllDataAsync();
        Task ResetIdentitySchedulesAsync();
        Task ResetIdentityAllTableAsync();
        Task DeleteScheduleAsync(Schedule schedule);
    }
}
