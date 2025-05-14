using System.Windows.Input;
using SchedulerWpfApp.Helper;

namespace SchedulerWpfApp.ViewModel
{
    /// <summary>
    /// The main ViewModel used to manage navigation between different sub-views (Course, Lecturer, Room).
    /// </summary>
    public class MainViewModel : ViewBaseModel
    {
        // Holds the currently displayed ViewModel in the UI
        private object _currentViewModel;

        // Factory delegates to lazily create view models
        private readonly Func<CourseViewModel> _courseViewModelFactory;
        private readonly Func<LecturerViewModel> _lecturerViewModelFactory;
        private readonly Func<RoomViewModel> _roomViewModelFactory;

        /// <summary>
        /// The current view model being shown in the main content area.
        /// Changing this will update the UI via data binding.
        /// </summary>
        public object CurrentViewModel
        {
            get => _currentViewModel;
            set { _currentViewModel = value; OnPropertyChanged(); }
        }

        // Commands bound to buttons or menu items to switch views
        public ICommand ShowCourseCommand { get; }
        public ICommand ShowTeacherCommand { get; }
        public ICommand ShowRoomCommand { get; }

        /// <summary>
        /// Initializes the MainViewModel with view model factories.
        /// Sets up commands and sets the default view to LecturerViewModel.
        /// </summary>
        public MainViewModel(Func<CourseViewModel> courseViewModelFactory,
            Func<LecturerViewModel> lecturerViewModelFactory,
            Func<RoomViewModel> roomViewModelFactory)
        {
            // Assign factory methods
            _courseViewModelFactory = courseViewModelFactory;
            _lecturerViewModelFactory = lecturerViewModelFactory;
            _roomViewModelFactory = roomViewModelFactory;

            // Initialize commands for switching views
            ShowCourseCommand = new RelayCommand(ShowCourse);
            ShowTeacherCommand = new RelayCommand(ShowTeacher);
            ShowRoomCommand = new RelayCommand(ShowRoom);

            // Set default view to LecturerViewModel
            CurrentViewModel = _lecturerViewModelFactory();
        }

        /// <summary>
        /// Switches the current view to CourseViewModel.
        /// </summary>
        private void ShowCourse() => CurrentViewModel = _courseViewModelFactory();

        /// <summary>
        /// Switches the current view to LecturerViewModel.
        /// </summary>
        private void ShowTeacher() => CurrentViewModel = _lecturerViewModelFactory();

        /// <summary>
        /// Switches the current view to RoomViewModel.
        /// </summary>
        private void ShowRoom() => CurrentViewModel = _roomViewModelFactory();
    }
}
