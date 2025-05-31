using System.IO;
using Microsoft.EntityFrameworkCore;
using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Data
{
    /// <summary>
    /// Database context class that provides access to the application's data.
    /// Uses Entity Framework Core with SQLite as the database provider.
    /// </summary>
    public class DataContext : DbContext
    {
        /// <summary>
        /// Default constructor for the DataContext.
        /// </summary>
        public DataContext()
        {
        }

        /// <summary>
        /// Constructor that accepts DbContextOptions for configuration.
        /// Used for dependency injection and testing scenarios.
        /// </summary>
        /// <param name="options">The options to configure the context.</param>
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
        }

        /// <summary>
        /// DbSet representing the Persons table in the database.
        /// </summary>
        public DbSet<Person> Persons { get; set; } = null!;
        public DbSet<Subject> Subjects { get; set; } = null!;
        public DbSet<Lecturer> Lecturers { get; set; } = null!;
        public DbSet<LecturerSubject> LecturerSubjects { get; set; } = null!;
        public DbSet<GroupName> GroupName { get; set; }
        public DbSet<Schedule> Schedules { get; set; }
        public DbSet<LecturerRequest> LecturerRequests { get; set; } = null!;
        public DbSet<Room> Rooms { get; set; } = null!;
        /// <summary>
        /// Configures the database connection if not already configured.
        /// Creates the necessary directories for the SQLite database file if they don't exist.
        /// </summary>
        /// <param name="optionsBuilder">The builder used to configure the context options.</param>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            try
            {
                if (!optionsBuilder.IsConfigured)
                {
                    // Get the application's base directory
                    string binDirectory = AppDomain.CurrentDomain.BaseDirectory;
                    // Navigate up three directories to the project root
                    string baseDirectory = Path.GetFullPath(Path.Combine(binDirectory, @"..\\..\\..\\"));
                    // Define the path for storing application data
                    string appDataPath = Path.Combine(baseDirectory, "AppData");

                    // Fallback logic if the directory structure is different (possibly in production)
                    if (!Directory.Exists(appDataPath))
                    {
                        baseDirectory = Path.GetFullPath(Path.Combine(binDirectory, "@..\\.."));
                        Directory.CreateDirectory(appDataPath);
                    }

                    // Define the database file path
                    string dbPath = Path.Combine(appDataPath, "app.db");
                    // Configure the context to use SQLite with the specified database file
                    optionsBuilder.UseSqlite($"Data Source={dbPath}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error configuring database connection", ex);
            }
        }
    }
}
