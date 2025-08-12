using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Repository.ScheduleRepository
{
    public interface IScheduleRepository: IBaseRepository<Schedule>
    {
        Task<bool> AddScheduleAsync(Schedule schedules);
        Task<bool> AddSlotAsync(Schedule schedule);
        Task DeleteAllAsync(int schedulerId);
        Task ResetIdentitySchedulesAsync();
        Task DeleteScheduleAsync(Schedule schedule);
    }
}
