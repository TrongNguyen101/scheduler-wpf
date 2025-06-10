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
        public DbSet<GroupClass> GroupName { get; set; }
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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure the index for CurriculumCode and SubjectCode in the CurriculumSubject entity
            modelBuilder.Entity<CurriculumSubject>()
                   .HasIndex(c => new { c.CurriculumCode, c.SubjectCode })
                   .HasDatabaseName("IX_CurriculumCode_SubjectCode");

            // Configure the index of CurriculumCode in the GroupClass entity
            modelBuilder.Entity<GroupClass>()
                    .HasIndex(g => new { g.CurriculumCode })
                    .HasDatabaseName("IX_CurriculumCode");

            // Create a composite index for LecturerId and SubjectCode
            modelBuilder.Entity<LecturerSubject>()
                    .HasIndex(ls => new { ls.LecturerId, ls.SubjectCode })  // Composite index for LecturerId and SubjectCode
                    .HasDatabaseName("IX_LecturerId_SubjectCode");  // Name of the index

            // Configure the index for RoomName in the Room entity
            modelBuilder.Entity<Room>()
                    .HasIndex(r => r.RoomName)
                    .HasDatabaseName("IX_RoomName");

            modelBuilder.Entity<Schedule>(entity =>
            {
                // Relation: Schedule → Subject (Many-to-One)
                entity.HasOne(ss => ss.Subject)
                      .WithMany(s => s.Schedules)
                      .HasForeignKey(ss => ss.SubjectCode)
                      .OnDelete(DeleteBehavior.Restrict);

                // Relation: Schedule → Lecturer (Many-to-One)
                entity.HasOne(ss => ss.Lecturer)
                      .WithMany(l => l.Schedules)
                      .HasForeignKey(ss => ss.LecturerId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Relation: Schedule → Room (Many-to-One)
                entity.HasOne(ss => ss.Room)
                      .WithMany(r => r.Schedules)
                      .HasForeignKey(ss => ss.RoomId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Relation: Schedule → StudentClass (Many-to-One)
                entity.HasOne(ss => ss.GroupClass)
                      .WithMany(sc => sc.Schedules)
                      .HasForeignKey(ss => ss.GroupName)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<LecturerSubject>(entity =>
            {
                // Relation: LecturerSubject → Subject (Many-to-One)
                entity.HasOne(ls => ls.Subject)
                      .WithMany(s => s.LecturerSubjects)
                      .HasForeignKey(ls => ls.SubjectCode)
                      .OnDelete(DeleteBehavior.Restrict);
                // Relation: LecturerSubject → Lecturer (Many-to-One)
                entity.HasOne(ls => ls.Lecturer)
                      .WithMany(l => l.LecturerSubjects)
                      .HasForeignKey(ls => ls.LecturerId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<LecturerRequest>(LecturerRequest =>
            {
                // Relation: LecturerRequest → Lecturer (Many-to-One)
                LecturerRequest.HasOne(lr => lr.Lecturer)
                               .WithMany(l => l.LecturerRequests)
                               .HasForeignKey(lr => lr.LecturerId)
                               .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<CurriculumSubject>(entity =>
            {
                // Relation: CurriculumSubject → Subject (Many-to-One)
                entity.HasOne(cs => cs.Subject)
                            .WithMany(s => s.CurriculumSubjects)
                            .HasForeignKey(cs => cs.SubjectCode)
                            .OnDelete(DeleteBehavior.Restrict);
                // Relation: CurriculumSubject → Curriculum (Many-to-One)
                entity.HasOne(cs => cs.Curriculum)
                            .WithMany(c => c.CurriculumSubjects)
                            .HasForeignKey(cs => cs.CurriculumCode)
                            .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
