using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Repository.ScheduleRepository
{
    public interface IScheduleRepository: IBaseRepository<Schedule>
    {
        Task<bool> AddScheduleAsync(List<Schedule> schedules);
        Task DeleteAllAsync();
        Task ResetIdentitySchedulesAsync();
    }
}
