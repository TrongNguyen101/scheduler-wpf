using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Repository.ScheduleRepository
{
    public interface IScheduleRepository: IBaseRepository<Schedule>
    {
        Task<bool> AddScheduleAsync(List<Schedule> schedules);
        Task<bool> AddSlotAsync(Schedule schedule);
        Task DeleteAllAsync();
        Task ResetIdentitySchedulesAsync();
        Task DeleteScheduleAsync(Schedule schedule);
    }
}
