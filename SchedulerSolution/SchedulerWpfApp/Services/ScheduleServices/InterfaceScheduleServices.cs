using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Services.ScheduleServices
{
    public interface InterfaceScheduleServices
    {
        Task<bool> AddScheduleAsync(List<Schedule> schedules);
        Task<bool> UpdateScheduleAsync(Schedule schedule);
        Task<List<Schedule>> GetAllAsync();
    }
}
