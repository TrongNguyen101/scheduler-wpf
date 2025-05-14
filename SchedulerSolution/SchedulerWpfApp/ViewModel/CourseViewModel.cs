using System.Collections.ObjectModel;
using System.Windows.Input;
using SchedulerWpfApp.Helper;

namespace SchedulerWpfApp.ViewModel
{
    public class CourseViewModel : ViewBaseModel
    {
        // Observable collection to hold list of courses
        public ObservableCollection<string> Courses { get; set; }

        public ICommand AddCourseCommand { get; set; }
        public ICommand RemoveCourseCommand { get; set; }

        public CourseViewModel()
        {
            Courses = new ObservableCollection<string> { "Math", "English", "Science" };
            AddCourseCommand = new RelayCommand(AddCourse);
            RemoveCourseCommand = new RelayCommand(RemoveCourse);
        }

        // Add a new course to the list
        private void AddCourse()
        {
            Courses.Add($"Course {Courses.Count + 1}");
        }

        // Remove a course from the list
        private void RemoveCourse()
        {
            if (Courses.Count > 0)
                Courses.RemoveAt(Courses.Count - 1);
        }
    }
}
