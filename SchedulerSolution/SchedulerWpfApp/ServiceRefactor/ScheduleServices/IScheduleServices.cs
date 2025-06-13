using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.ServiceRefactor.ScheduleServices
{
    public interface IScheduleServices
    {
        Task<bool> AddScheduleAsync(List<Schedule> schedules);
        Task<List<Schedule>> GetAllAsync();
    }
}
