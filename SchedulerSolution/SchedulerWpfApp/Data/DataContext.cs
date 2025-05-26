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
                },
                new Person
                {
                    Id = 3,
                    FirstName = "Alice",
                    LastName = "Johnson",
                    Email = "Alice.johnson@example.com",
                    Phone = "555-123-4567",
                }
             );

            modelBuilder.Entity<Subject>().HasData(
                new Subject
                {
                    SubjectCode = "WDP201",
                    SubjectName = "Web development",
                    Major = "SE",
                    TotalSessions = 1,
                    SlotsPerWeek = 20,
                    SemesterId = "SU25"
                },
                new Subject
                {
                    SubjectCode = "SEP492",
                    SubjectName = "Do an tot nghiep",
                    Major = "SE",
                    TotalSessions = 1,
                    SlotsPerWeek = 20,
                    SemesterId = "SU25"
                },
                new Subject
                {
                    SubjectCode = "HCM202",
                    SubjectName = "Tw tuong Ho Chi Minh",
                    Major = "SE",
                    TotalSessions = 1,
                    SlotsPerWeek = 20,
                    SemesterId = "SU25"
                },
                new Subject
                {
                    SubjectCode = "SWP391",
                    SubjectName = "Software Engineering",
                    Major = "SE",
                    TotalSessions = 20,
                    SlotsPerWeek = 2,
                    SemesterId = ""
                },
                new Subject
                {
                    SubjectCode = "SWT301",
                    SubjectName = "Software Testing",
                    Major = "SE",
                    TotalSessions = 20,
                    SlotsPerWeek = 2,
                    SemesterId = ""
                },
                new Subject
                {
                    SubjectCode = "SWR302",
                    SubjectName = "Software Requirement",
                    Major = "SE",
                    TotalSessions = 20,
                    SlotsPerWeek = 2,
                    SemesterId = ""
                },
                new Subject
                {
                    SubjectCode = "PRN211",
                    SubjectName = "Programming",
                    Major = "SE",
                    TotalSessions = 20,
                    SlotsPerWeek = 2,
                    SemesterId = ""
                },
                new Subject
                {
                    SubjectCode = "ENW11",
                    SubjectName = "English",
                    Major = "SE",
                    TotalSessions = 10,
                    SlotsPerWeek = 1,
                    SemesterId = ""
                }
             );

            modelBuilder.Entity<LecturerRequest>().HasData(
                new LecturerRequest
                {
                    Id = 1,
                    LecturerId = "L1",
                    DayName = "Monday",
                    Session = "A",
                    SlotTime = null,
                    SlotType = null
                },
                new LecturerRequest
                {
                    Id = 2,
                    LecturerId = "L1",
                    DayName = "Wednesday",
                    Session = "A",
                    SlotTime = null,
                    SlotType = null
                }
            );


            modelBuilder.Entity<Lecturer>().HasData(
                new Lecturer { LecturerId = "1", LecturerName = "Nguyễn Văn A", Role = null },
                new Lecturer { LecturerId = "2", LecturerName = "Trần Thị B", Role = null },
                new Lecturer { LecturerId = "L1", LecturerName = "Nguyen Van Xoai", Role = null },
                new Lecturer { LecturerId = "L2", LecturerName = "Sờ Mai", Role = null },
                new Lecturer { LecturerId = "L3", LecturerName = "Nguyen Mang Gồ", Role = null },
                new Lecturer { LecturerId = "L4", LecturerName = "Nguyen Vỉa Hè", Role = null },
                new Lecturer { LecturerId = "L5", LecturerName = "Nguyen Hoa Hong", Role = null },
                new Lecturer { LecturerId = "L6", LecturerName = "Nguyen Thi Hoa", Role = null },
                new Lecturer { LecturerId = "L7", LecturerName = "Nguyen Thi Bưởi", Role = null },
                new Lecturer { LecturerId = "L8", LecturerName = "Nguyen Thi Đào", Role = null },
                new Lecturer { LecturerId = "L9", LecturerName = "Nguyen Thi Oi", Role = null },
                new Lecturer { LecturerId = "L10", LecturerName = "Nguyen Thi Cam", Role = null },
                new Lecturer { LecturerId = "L11", LecturerName = "Nguyen Thi Mit", Role = null },
                new Lecturer { LecturerId = "L12", LecturerName = "Nguyen Thi Leo", Role = null },
                new Lecturer { LecturerId = "L13", LecturerName = "Nguyen Thi Man", Role = null },
                new Lecturer { LecturerId = "L14", LecturerName = "Nguyen Teo Em", Role = null },
                new Lecturer { LecturerId = "L15", LecturerName = "Nguyen Thi Cam", Role = null },
                new Lecturer { LecturerId = "L16", LecturerName = "Nguyen Thi Chuoi", Role = null },
                new Lecturer { LecturerId = "L17", LecturerName = "Nguyen Thi Hoa", Role = null }

            );

            modelBuilder.Entity<LecturerSubject>().HasData(
                new LecturerSubject { Id = 1, LecturerId = "L2", SubjectCode = "SWP391", LecturerName = "Sờ Mai", NumberOfClasses = 1 },
                new LecturerSubject { Id = 2, LecturerId = "L3", SubjectCode = "SWP391", LecturerName = "Nguyen Mang Gồ", NumberOfClasses = 1 },
                new LecturerSubject { Id = 3, LecturerId = "L4", SubjectCode = "SWP391", LecturerName = "Nguyen Vỉa Hè", NumberOfClasses = 1 },

                new LecturerSubject { Id = 4, LecturerId = "L5", SubjectCode = "SWT301", LecturerName = "Nguyen Hoa Hong", NumberOfClasses = 1 },
                new LecturerSubject { Id = 5, LecturerId = "L6", SubjectCode = "SWT301", LecturerName = "Nguyen Thi Hoa", NumberOfClasses = 1 },
                new LecturerSubject { Id = 6, LecturerId = "L7", SubjectCode = "SWT301", LecturerName = "Nguyen Thi Bưởi", NumberOfClasses = 1 },
                new LecturerSubject { Id = 7, LecturerId = "L8", SubjectCode = "SWT301", LecturerName = "Nguyen Thi Đào", NumberOfClasses = 1 },

                new LecturerSubject { Id = 8, LecturerId = "L9", SubjectCode = "SWR302", LecturerName = "Nguyen Thi Oi", NumberOfClasses = 1 },
                new LecturerSubject { Id = 9, LecturerId = "L10", SubjectCode = "SWR302", LecturerName = "Nguyen Thi Cam", NumberOfClasses = 1 },
                new LecturerSubject { Id = 10, LecturerId = "L11", SubjectCode = "SWR302", LecturerName = "Nguyen Thi Mit", NumberOfClasses = 1 },
                new LecturerSubject { Id = 11, LecturerId = "L12", SubjectCode = "SWR302", LecturerName = "Nguyen Thi Leo", NumberOfClasses = 1 },

                new LecturerSubject { Id = 12, LecturerId = "L13", SubjectCode = "PRN211", LecturerName = "Nguyen Thi Man", NumberOfClasses = 1 },
                new LecturerSubject { Id = 13, LecturerId = "L14", SubjectCode = "PRN211", LecturerName = "Nguyen Teo Em", NumberOfClasses = 1 },
                new LecturerSubject { Id = 14, LecturerId = "L15", SubjectCode = "PRN211", LecturerName = "Nguyen Thi Cam", NumberOfClasses = 1 },
                new LecturerSubject { Id = 15, LecturerId = "L16", SubjectCode = "PRN211", LecturerName = "Nguyen Thi Chuoi", NumberOfClasses = 1 },

                new LecturerSubject { Id = 16, LecturerId = "L17", SubjectCode = "ENW11", LecturerName = "Nguyen Thi Hoa", NumberOfClasses = 1 }

            );

            modelBuilder.Entity<GroupName>().HasData(
              new GroupName
              {
                  ClassId = "CL01",
                  Category = "Class room",
                  Major = "SE",
                  NumberOfScheduler = 5,
                  NumberOfStudents = 35
              },
              new GroupName
              {
                  ClassId = "CL02",
                  Category = "Class room",
                  Major = "MC",
                  NumberOfScheduler = 5,
                  NumberOfStudents = 35
              },
              new GroupName
              {
                  ClassId = "CL03",
                  Category = "Computer lab",
                  Major = "SE",
                  NumberOfScheduler = 5,
                  NumberOfStudents = 35
              }
           );
        }
    }
}
