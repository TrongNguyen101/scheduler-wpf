using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using SchedulerWpfApp.Data;
using SchedulerWpfApp.Model;
using SchedulerWpfApp.Services;

namespace SchedulerWpfApp.Tests.Services
{
    [TestFixture]
    public class CourseServiceTest
    {
        private Mock<DataContext> _mockDataContext;
        private Mock<DbSet<Subject>> _mockDbSet;
        private CourseService _courseService;
        private List<Subject> _subjectData;

        [SetUp]
        public void Setup()
        {
            _subjectData = new List<Subject>
            {
                new Subject { SubjectCode = "CS101", SubjectName = "Computer Science 101" },
                new Subject { SubjectCode = "MATH201", SubjectName = "Mathematics 201" },
                new Subject { SubjectCode = "ENG301", SubjectName = "English 301" }
            };

            // Setup mock DbSet
            _mockDbSet = CreateMockDbSet(_subjectData);
            _mockDataContext = new Mock<DataContext>();
            _mockDataContext.Setup(c => c.Subjects).Returns(_mockDbSet.Object);
            _courseService = new CourseService(_mockDataContext.Object);
        }

        [Test]
        public async Task AddSubject_ValidSubject_ShouldAddToDatabase()
        {
            // Arrange
            var newSubject = new Subject
            {
                SubjectCode = "PHY401",
                SubjectName = "Physics 401"
            };

            // Act
            await _courseService.AddSubject(newSubject);

            // Assert
            _mockDbSet.Verify(m => m.Add(It.Is<Subject>(s =>
                s.SubjectCode == "PHY401" &&
                s.SubjectName == "Physics 401")), Times.Once);

            _mockDataContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        private Mock<DbSet<Subject>> CreateMockDbSet(List<Subject> data)
        {
            var queryable = data.AsQueryable();
            var mockSet = new Mock<DbSet<Subject>>();

            // Setup cho IQueryable methods
            mockSet.As<IQueryable<Subject>>().Setup(m => m.Provider).Returns(queryable.Provider);
            mockSet.As<IQueryable<Subject>>().Setup(m => m.Expression).Returns(queryable.Expression);
            mockSet.As<IQueryable<Subject>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
            mockSet.As<IQueryable<Subject>>().Setup(m => m.GetEnumerator()).Returns(queryable.GetEnumerator());

            // Setup cho async methods
            mockSet.Setup(m => m.ToListAsync(It.IsAny<CancellationToken>()))
                   .ReturnsAsync(data);

            // Setup cho FindAsync
            mockSet.Setup(m => m.FindAsync(It.IsAny<object[]>()))
                   .Returns<object[]>(keyValues =>
                   {
                       var subjectCode = keyValues[0].ToString();
                       var subject = data.FirstOrDefault(s => s.SubjectCode == subjectCode);
                       return new ValueTask<Subject>(subject);
                   });

            // Setup cho Add method
            mockSet.Setup(m => m.Add(It.IsAny<Subject>()))
                   .Callback<Subject>(subject => data.Add(subject));

            // Setup cho Remove method
            mockSet.Setup(m => m.Remove(It.IsAny<Subject>()))
                   .Callback<Subject>(subject => data.Remove(subject));

            return mockSet;
        }

    }
}
