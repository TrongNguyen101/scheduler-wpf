using Microsoft.EntityFrameworkCore;
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
        /// <summary>
        /// Initializes a new instance of the ScheduleServices class
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="unitOfWork"></param>
        public ScheduleServices(ILogger<ScheduleServices> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger; // Injecting the logger to log errors and information
            _unitOfWork = unitOfWork; // Injecting the unit of work to manage database operations
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
                var existingScheduler = await _unitOfWork.ScheduleRepository.GetByIdAsync(schedule.ScheduleId);

                if (existingScheduler != null)
                {
                    existingScheduler.ScheduleId = schedule.ScheduleId;
                    existingScheduler.RoomId = schedule.RoomId;
                    existingScheduler.RoomName = schedule.RoomName;
                    existingScheduler.PartOfDay = schedule.PartOfDay;
                    existingScheduler.SlotTime = schedule.SlotTime;
                    existingScheduler.StatusSlot = schedule.StatusSlot;
                    existingScheduler.Date = schedule.Date;
                    existingScheduler.Major = schedule.Major;
                    existingScheduler.SubjectCode = schedule.SubjectCode;
                    existingScheduler.GroupName = schedule.GroupName;
                    existingScheduler.LecturerId = schedule.LecturerId;
                    existingScheduler.LecturerName = schedule.LecturerName;
                    existingScheduler.SlotTypeCode = schedule.SlotTypeCode;
                    existingScheduler.TypeSlot = schedule.TypeSlot;
                    existingScheduler.SessionNo = schedule.SessionNo;

                    await _unitOfWork.BeginTransactionAsync();
                    await _unitOfWork.Repository<Schedule>().UpdateAsync(existingScheduler);
                    await _unitOfWork.CommitAsync();
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to update schedule due to an unexpected error.");
                await _unitOfWork.RollbackAsync();
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
                throw new Exception("Lỗi khi lấy danh sách lịch học", ex);
            }
        }
        #endregion
    }
}
