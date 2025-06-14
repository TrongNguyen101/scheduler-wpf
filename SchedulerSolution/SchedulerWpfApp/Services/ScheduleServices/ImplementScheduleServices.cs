using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchedulerWpfApp.Data;
using SchedulerWpfApp.Model;
using SchedulerWpfApp.Repository;

namespace SchedulerWpfApp.Services.ScheduleServices
{
    public class ImplementScheduleServices : InterfaceScheduleServices
    {
        #region Fields
        private IUnitOfWork _unitOfWork;
        private readonly DataContext _context;
        private readonly ILogger<ImplementScheduleServices> _logger;
        #endregion

        #region Constructor
        public ImplementScheduleServices(DataContext context, ILogger<ImplementScheduleServices> logger, IUnitOfWork unitOfWork)
        {
            _context = context;
            _logger = logger;
            _unitOfWork = unitOfWork;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Add a new schedules to the database
        /// </summary>
        /// <param name="schedules"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public async Task<bool> AddScheduleAsync(List<Schedule> schedules)
        {
            if (schedules == null)
                throw new ArgumentNullException(nameof(schedules));

            try
            {
                await _unitOfWork.BeginTransactionAsync();
                await _unitOfWork.ScheduleRepository.AddScheduleAsync(schedules); // Sử dụng phương thức AddRange từ IUnitOfWork
                await _unitOfWork.CommitAsync();
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
                return await _unitOfWork.Repository<Schedule>().GetAllAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to add schedules due to an unexpected error.");
                throw new Exception("Lỗi khi lấy danh sách lớp học", ex);
            }
        }
        #endregion
    }
}
