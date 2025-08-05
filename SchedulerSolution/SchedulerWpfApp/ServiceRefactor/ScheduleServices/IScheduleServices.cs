using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.ServiceRefactor.ScheduleServices
{
    public interface IScheduleServices
    {
        Task<bool> AddScheduleAsync(List<Schedule> schedules);
        Task<bool> AddSlot(Schedule schedule);
        Task<bool> UpdateScheduleAsync(Schedule schedule);
        Task<List<Schedule>> GetAllAsync();
        void ExportToExcel(List<Schedule> schedules, string filePath);
        Task DeleteAllAsync();
        Task DeleteScheduleAsync(int scheduleId);
    }
}
