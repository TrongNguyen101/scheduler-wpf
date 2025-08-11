using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Repository.ScheduleRepository
{
    public interface IScheduleRepository: IBaseRepository<Schedule>
    {
        Task<bool> AddScheduleAsync(List<Schedule> schedules);
        Task<bool> AddSlotAsync(Schedule schedule);
        Task DeleteAllAsync();
        Task DeleteAllDataAsync();
        Task ResetIdentitySchedulesAsync();
        Task ResetIdentityAllTableAsync();
        Task DeleteScheduleAsync(Schedule schedule);
    }
}
