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
    }
}
