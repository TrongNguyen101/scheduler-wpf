using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchedulerWpfApp.Data;
using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Services.ScheduleServices
{
    public class ImplementScheduleServices : InterfaceScheduleServices
    {
        private readonly DataContext _context;
        private readonly ILogger<ImplementScheduleServices> _logger;

        public ImplementScheduleServices(DataContext context, ILogger<ImplementScheduleServices> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> AddScheduleAsync(List<Schedule> schedules)
        {
            if (schedules == null)
                throw new ArgumentNullException(nameof(schedules));

            try
            {
                _context.Schedules.AddRange(schedules); // Dùng AddRange cho danh sách
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException ex)
            {
                _logger?.LogError(ex, "Failed to add schedules due to DB update error.");
                return false;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to add schedules due to an unexpected error.");
                return false;
            }
        }

        /// <summary>
        /// Get all scheduler
        /// </summary>
        /// <returns>List schedule</returns>
        public async Task<List<Schedule>> GetAllAsync()
        {
            try
            {
                return await _context.Schedules.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to add schedules due to an unexpected error.");
                throw new Exception("Lỗi khi lấy danh sách lớp học", ex);
            }
        }

        /// <summary>
        /// Update schedule in database
        /// </summary>
        /// <param name="schedule"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public async Task<bool> UpdateScheduleAsync(Schedule schedule)
        {
            if (schedule == null)
                throw new ArgumentNullException(nameof(schedule));
            try
            {
                var existingScheduler = await _context.Schedules.FindAsync(schedule.ScheduleId);

                if (existingScheduler != null)
                {
                    existingScheduler.ScheduleId = schedule.ScheduleId;
                    existingScheduler.RoomNo = schedule.RoomNo;
                    existingScheduler.PartOfDay = schedule.PartOfDay;
                    existingScheduler.SlotTime = schedule.SlotTime;
                    existingScheduler.StatusSlot = schedule.StatusSlot;
                    existingScheduler.Date = schedule.Date;
                    existingScheduler.Major = schedule.Major;
                    existingScheduler.SubjectCode = schedule.SubjectCode;
                    existingScheduler.GroupName = schedule.GroupName;
                    existingScheduler.LecturerId = schedule.LecturerId;
                    existingScheduler.SlotTypeCode = schedule.SlotTypeCode;
                    existingScheduler.TypeSlot = schedule.TypeSlot;
                    existingScheduler.SessionNo = schedule.SessionNo;

                    await _context.SaveChangesAsync();
                }
                return true;
            }
            catch (DbUpdateException ex)
            {
                _logger?.LogError(ex, "Failed to update schedule due to DB update error.");
                return false;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to update schedule due to an unexpected error.");
                return false;
            }
        }
    }
}
