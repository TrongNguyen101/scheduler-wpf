using Microsoft.Extensions.Logging;
using SchedulerWpfApp.Model;
using SchedulerWpfApp.Repository;

namespace SchedulerWpfApp.ServiceRefactor.ScheduleServices
{
    public class ScheduleServices : IScheduleServices
    {
        #region Fields
        private IUnitOfWork _unitOfWork;
        private readonly ILogger<ScheduleServices> _logger;
        #endregion

        #region Constructor
        public ScheduleServices(ILogger<ScheduleServices> logger, IUnitOfWork unitOfWork)
        {
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

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _unitOfWork.ScheduleRepository.AddScheduleAsync(schedules); // Sử dụng phương thức AddRange từ IUnitOfWork
                await _unitOfWork.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
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
