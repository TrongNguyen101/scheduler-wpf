using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using SchedulerWpfApp.Data;
using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Repository.ScheduleRepository
{
    public class ScheduleRepository : BaseRepository<Schedule>, IScheduleRepository
    {
        #region Constructors
        /// <summary>
        /// Constructor for ScheduleRepository that initializes the base repository with the provided DataContext.
        /// </summary>
        /// <param name="context"></param>
        public ScheduleRepository(DataContext context) : base(context) { }
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
                     .Include(s => s.Room)
                     .Include(s => s.Lecturer)
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
        #endregion
    }
}
