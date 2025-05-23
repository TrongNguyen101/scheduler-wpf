using System.Collections.ObjectModel;
using Moq;
using NUnit.Framework;
using SchedulerWpfApp.Model;
using SchedulerWpfApp.Services;
using SchedulerWpfApp.ViewModel;

namespace SchedulerWpfApp.Tests
{
    [TestFixture]
    public class CourseViewModelTest
    {
        // Mocks for service dependencies
        private Mock<ICourseService> _mockCourseService;
        private Mock<IExcelSubjectExporter> _mockExporter;
        private Mock<IExcelSubjectImporter> _mockImporter;
        private CourseViewModel _viewModel;

        [SetUp]
        public void Setup()
        {
            // Initialize mocks
            _mockCourseService = new Mock<ICourseService>();
            _mockExporter = new Mock<IExcelSubjectExporter>();
            _mockImporter = new Mock<IExcelSubjectImporter>();

            // Set up the course service to return an empty list when call LoadSubjectAsync in CourseViewModel constructor
            // If comment then show popup error
            _mockCourseService
                            .Setup(service => service.GetAllAsync())
                            .ReturnsAsync(new List<Subject>());

            // Instantiate the ViewModel with the mocked services
            _viewModel = new CourseViewModel(
                _mockCourseService.Object,
                _mockExporter.Object,
                _mockImporter.Object
            );
        }

        [Test]
        public void SearchKeyword_Should_FilterSubjects()
        {
            // Arrange: create a list of test subjects
            var subjects = new List<Subject>
            {
                new Subject { SubjectCode = "MATH01", SubjectName = "Mathematics", Major = "SE" },
                new Subject { SubjectCode = "CS101", SubjectName = "Computer Science", Major = "SE" },
                new Subject { SubjectCode = "ENG01", SubjectName = "English", Major = "EN" }
            };

            // Assign the subject list to the ViewModel
            _viewModel.Subjects = new ObservableCollection<Subject>(subjects);

            // Set the private _allSubjects field using reflection for internal filtering
            typeof(CourseViewModel)
                .GetField("_allSubjects", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(_viewModel, new ObservableCollection<Subject>(subjects));

            // Act - update the search keyword to filter subjects
            _viewModel.SearchKeyword = "math";

            // Assert - only one subject should match the keyword "math" with list subjects example above
            Assert.AreEqual(1, _viewModel.Subjects.Count);
            Assert.AreEqual("MATH01", _viewModel.Subjects[0].SubjectCode);
        }
    }
}
