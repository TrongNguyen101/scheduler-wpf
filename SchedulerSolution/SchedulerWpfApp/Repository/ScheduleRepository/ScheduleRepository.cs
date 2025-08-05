using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchedulerWpfApp.Data;
using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Repository.ScheduleRepository
{
    public class ScheduleRepository : BaseRepository<Schedule>, IScheduleRepository
    {
        private readonly ILogger<ScheduleRepository> _logger;

        #region Constructors
        /// <summary>
        /// Constructor for ScheduleRepository that initializes the base repository with the provided DataContext.
        /// </summary>
        /// <param name="context"></param>
        public ScheduleRepository(DataContext context, ILogger<ScheduleRepository> logger) : base(context)
        {
            _logger = logger;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Retrieves all schedules from the database, including related Room and Lecturer entities.
        /// </summary>
        /// <returns></returns>
        public override async Task<List<Schedule>> GetAllAsync()
        {
            try
            {
                var schedules = await _context.Schedules
                     //.Include(s => s.Room)
                     //.Include(s => s.Lecturer)
                     .ToListAsync();
                return schedules;
            }
            catch (Exception ex)
            {
                // Log the exception (not implemented here)
                return new List<Schedule>();
            }
        }
        /// <summary>
        /// Adds a list of schedules to the database.
        /// </summary>
        /// <param name="schedules"></param>
        /// <returns></returns>
        public Task<bool> AddScheduleAsync(List<Schedule> schedules)
        {
            try
            {
                _context.Schedules.AddRange(schedules);
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                // Log the exception (not implemented here)
                return Task.FromResult(false);
            }
        }

        public async Task<bool> AddSlotAsync(Schedule schedule)
        {
            try
            {
                await _context.Schedules.AddAsync(schedule);
                return true;
            }
            catch (Exception ex)
            {
                // Log the exception (not implemented here)
                return false;
            }
        }

        public Task DeleteAllAsync()
        {
            _logger.LogInformation("Marking all schedules as deleted from the database.");
            _context.Schedules.RemoveRange(_context.Schedules);
            return Task.CompletedTask;
        }

        public async Task ResetIdentitySchedulesAsync()
        {
            // SQL query to reset the auto-increment value in SQLite
            var sql = "DELETE FROM sqlite_sequence WHERE name='Schedules';";

            // Execute the SQL command asynchronously using EF Core
            await _context.Database.ExecuteSqlRawAsync(sql);
        }

        public async Task DeleteScheduleAsync(Schedule schedule)
        {
            try
            {
                if (schedule != null)
                {
                    _context.Schedules.Remove(schedule);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete schedule with.");
            }
        }
        #endregion
    }
}
