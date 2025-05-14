using System.Windows.Input;
using SchedulerWpfApp.Helper;

namespace SchedulerWpfApp.ViewModel
{
    /// <summary>
    /// The main ViewModel used to manage navigation between different sub-views (Course, Person, Room).
    /// </summary>
    public class MainViewModel : ViewBaseModel
    {
        // Holds the currently displayed ViewModel in the UI
        private object _currentViewModel;

        // Factory delegates to lazily create view models
        private readonly Func<CourseViewModel> _courseViewModelFactory;
        private readonly Func<PersonViewModel> _personViewModelFactory;
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
        public ICommand ShowPersonCommand { get; }
        public ICommand ShowRoomCommand { get; }

        /// <summary>
        /// Initializes the MainViewModel with view model factories.
        /// Sets up commands and sets the default view to PersonViewModel.
        /// </summary>
        public MainViewModel(Func<CourseViewModel> courseViewModelFactory,
            Func<PersonViewModel> personViewModelFactory,
            Func<RoomViewModel> roomViewModelFactory)
        {
            // Assign factory methods
            _courseViewModelFactory = courseViewModelFactory;
            _personViewModelFactory = personViewModelFactory;
            _roomViewModelFactory = roomViewModelFactory;

            // Initialize commands for switching views
            ShowCourseCommand = new RelayCommand(ShowCourse);
            ShowPersonCommand = new RelayCommand(ShowPerson);
            ShowRoomCommand = new RelayCommand(ShowRoom);

            // Set default view to PersonViewModel
            CurrentViewModel = _personViewModelFactory();
        }

        /// <summary>
        /// Switches the current view to CourseViewModel.
        /// </summary>
        private void ShowCourse() => CurrentViewModel = _courseViewModelFactory();

        /// <summary>
        /// Switches the current view to PersonViewModel.
        /// </summary>
        private void ShowPerson() => CurrentViewModel = _personViewModelFactory();

        /// <summary>
        /// Switches the current view to RoomViewModel.
        /// </summary>
        private void ShowRoom() => CurrentViewModel = _roomViewModelFactory();
    }
}
