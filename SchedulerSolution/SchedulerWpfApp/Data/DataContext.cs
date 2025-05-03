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

        /// <summary>
        /// Configures the database model creating relationships, constraints, and other configurations.
        /// Currently empty, but can be extended to define entity relationships and configurations.
        /// </summary>
        /// <param name="modelBuilder">The builder used to configure the model.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Person>().HasData(
                new Person
                {
                    Id = 1,
                    FirstName = "John",
                    LastName = "Doe",
                    Email = "john.doe@example.com",
                    Phone = "123-456-7890",
                },
                new Person
                {
                    Id = 2,
                    FirstName = "Jane",
                    LastName = "Smith",
                    Email = "Jane.smith@example.com",
                    Phone = "987-654-3210",
                }
             );
        }
    }
}
