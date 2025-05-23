using NUnit.Framework;
using SchedulerWpfApp.Data;
using SchedulerWpfApp.Model;
using SchedulerWpfApp.Services;
using SchedulerWpfApp.Tests.Services.Helper;

namespace SchedulerWpfApp.Tests.Services
{
    [TestFixture]
    public class CourseServiceTest
    {
        // In-memory DataContext for testing
        private DataContext _context;
        private CourseService _courseService;

        [SetUp]
        public void Setup()
        {
            // Set up an in-memory database context using a helper class
            _context = SetupService.CreateSetup();

            // Initialize the service with the test database context
            _courseService = new CourseService(_context);
        }

        [Test]
        public async Task GetAllAsync_Should_ReturnAllSubjects()
        {
            // Arrange - add sample subjects to the in-memory database
            _context.Subjects.AddRange(
               new Subject { SubjectCode = "ENG01", SubjectName = "English", Major = "SE", SlotsPerWeek = 1, TotalSessions = 1, SemesterId = "SU25" },
               new Subject { SubjectCode = "CS01", SubjectName = "C# Basics", Major = "SE", SlotsPerWeek = 1, TotalSessions = 1, SemesterId = "SU25" }
            );
            await _context.SaveChangesAsync();

            // Act - call the method to retrieve all subjects
            var allSubjects = await _courseService.GetAllAsync();

            // Assert - verify that two subjects were returned
            Assert.AreEqual(2, allSubjects.Count);
        }

        [Test]
        public async Task AddSubject_Should_AddSubjectToDatabase()
        {
            // Arrange - create a new subject to add
            var subject = new Subject { SubjectCode = "MLN131", SubjectName = "Math", Major = "SE", SlotsPerWeek = 1, TotalSessions = 1, SemesterId = "SU25" };

            // Act - add the subject using the service
            await _courseService.AddSubject(subject);

            // Assert - verify the subject was successfully saved in the database
            var result = await _context.Subjects.FindAsync("MLN131");

            Assert.NotNull(result);
            Assert.AreEqual("Math", result.SubjectName);
        }

    }
}
