using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchedulerWpfApp.Model;
using SchedulerWpfApp.Repository;
using Syncfusion.XlsIO;

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
                return await _unitOfWork.ScheduleRepository.GetAllAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to add schedules due to an unexpected error.");
                throw new Exception("Lỗi khi lấy danh sách lịch học", ex);
            }
        }

        public async Task DeleteAllAsync()
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                // Call delete and reset identity operations
                await _unitOfWork.ScheduleRepository.DeleteAllAsync();
                await _unitOfWork.ScheduleRepository.ResetIdentitySchedulesAsync();  // Ensure ResetIdentity is part of the transaction

                await _unitOfWork.CommitAsync();
            }
            catch (DbUpdateException dbEx)
            {
                _logger?.LogError(dbEx, "Database error while deleting all schedules.");
                await _unitOfWork.RollbackAsync();
                throw new Exception("Lỗi khi xóa tất cả lịch học do lỗi cơ sở dữ liệu.", dbEx);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Unexpected error while deleting all schedules.");
                await _unitOfWork.RollbackAsync();
                throw new Exception("Lỗi khi xóa tất cả lịch học.", ex);
            }
        }

        public void ExportToExcel(List<Schedule> schedules, string filePath)
        {
            using ExcelEngine excelEngine = new();
            IApplication application = excelEngine.Excel;
            application.DefaultVersion = ExcelVersion.Xlsx;

            IWorkbook workbook = application.Workbooks.Create(1);
            IWorksheet sheet = workbook.Worksheets[0];

            // Header row
            string[] headers = new string[]
            {
                "ScheduleId", "GroupName", "SubjectCode", "Date", "Slot",
                "RoomNo", "SessionNo", "Lecturer", "SlotTypeCode", "StatusSlot",
                "TypeSlot"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                sheet[1, i + 1].Text = headers[i];
            }

            // Data rows
            int row = 2;
            foreach (var s in schedules)
            {
                sheet[row, 1].Number = s.ScheduleId;
                sheet[row, 2].Text = s.GroupName ?? "";
                sheet[row, 3].Text = s.SubjectCode ?? "";
                sheet[row, 4].Text = s.Date?.ToString("yyyy-MM-dd") ?? "";
                sheet[row, 5].Number = s.SlotTime ?? 0;
                sheet[row, 6].Text = s.RoomName ?? "";
                sheet[row, 7].Number = s.SessionNo ?? 0;
                sheet[row, 8].Text = s.LecturerName ?? "";
                sheet[row, 9].Text = s.SlotTypeCode ?? "";
                sheet[row, 10].Text = s.StatusSlot ?? "";
                sheet[row, 11].Text = s.TypeSlot ?? "";

                row++;
            }

            workbook.SaveAs(filePath);
            MessageBox.Show("Export thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        #endregion
    }
}
