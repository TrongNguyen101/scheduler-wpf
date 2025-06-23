using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.ServiceRefactor.ScheduleServices
{
    public interface IScheduleServices
    {
        Task<bool> AddScheduleAsync(List<Schedule> schedules);
        Task<bool> UpdateScheduleAsync(Schedule schedule);
        Task<List<Schedule>> GetAllAsync();
    }
}
