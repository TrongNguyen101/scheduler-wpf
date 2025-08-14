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
        public Task<bool> AddScheduleAsync(Schedule schedules)
        {
            try
            {
                _context.Schedules.Add(schedules);
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

        public async Task DeleteAllAsync(int scheduleId)
        {
            var schedule = await _context.Schedules.FindAsync(scheduleId);
            _logger.LogInformation("Marking all schedules as deleted from the database.");
            if (schedule != null)
            {
                _context.Schedules.Remove(schedule);
            }
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

        public Task DeleteAllDataAsync()
        {
            _context.Schedules.RemoveRange(_context.Schedules);
            _context.GroupName.RemoveRange(_context.GroupName);
            _context.Lecturers.RemoveRange(_context.Lecturers);
            _context.Subjects.RemoveRange(_context.Subjects);
            _context.Rooms.RemoveRange(_context.Rooms);
            _context.Curriculums.RemoveRange(_context.Curriculums);
            _context.LecturerSubjects.RemoveRange(_context.LecturerSubjects);
            _context.CurriculumSubjects.RemoveRange(_context.CurriculumSubjects);

            return Task.CompletedTask;
        }

        public async Task ResetIdentityAllTableAsync()
        {
            // SQL query to reset the auto-increment value in SQLite
            var sqlSchedule = "DELETE FROM sqlite_sequence WHERE name='Schedules';";
            var sqlGroupName = "DELETE FROM sqlite_sequence WHERE name='GroupClass';";
            var sqlLecturer = "DELETE FROM sqlite_sequence WHERE name='Lecturer';";
            var sqlSubject = "DELETE FROM sqlite_sequence WHERE name='Subject';";
            var sqlRoom = "DELETE FROM sqlite_sequence WHERE name='Room';";
            var sqlCurriculum = "DELETE FROM sqlite_sequence WHERE name='Curriculum';";
            var sqLectuerSubject = "DELETE FROM sqlite_sequence WHERE name='LecturerSubject';";
            var sqlCurriculumSubject = "DELETE FROM sqlite_sequence WHERE name='CurriculumSubject';";

            // Execute the SQL command asynchronously using EF Core
            await _context.Database.ExecuteSqlRawAsync(sqlSchedule);
            await _context.Database.ExecuteSqlRawAsync(sqlGroupName);
            await _context.Database.ExecuteSqlRawAsync(sqlLecturer);
            await _context.Database.ExecuteSqlRawAsync(sqlSubject);
            await _context.Database.ExecuteSqlRawAsync(sqlRoom);
            await _context.Database.ExecuteSqlRawAsync(sqlCurriculum);
            await _context.Database.ExecuteSqlRawAsync(sqLectuerSubject);
            await _context.Database.ExecuteSqlRawAsync(sqlCurriculumSubject);
        }
        #endregion
    }
}
