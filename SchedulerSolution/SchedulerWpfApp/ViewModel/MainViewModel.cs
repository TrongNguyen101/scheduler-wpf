using System.Text;
using System.Windows.Input;
using SchedulerWpfApp.Helper;
using SchedulerWpfApp.Model;
using System.Diagnostics;
using Syncfusion.XlsIO;
using Microsoft.Win32;
using System.Windows;
using SchedulerWpfApp.Algorithm;

namespace SchedulerWpfApp.ViewModel
{
    /// <summary>
    /// The main ViewModel used to manage navigation between different sub-views (Course, Person, Room).
    /// </summary>
    public class MainViewModel : ViewBaseModel
    {
        // Holds the currently displayed ViewModel in the UI
        private object _currentViewModel;

        private readonly CreateScheduleTree _scheduleTree;

        // Factory delegates to lazily create view models
        private readonly Func<SubjectViewModel> _subjectViewModelFactory;
        private readonly Func<PersonViewModel> _personViewModelFactory;
        private readonly Func<RoomViewModel> _roomViewModelFactory;
        private readonly Func<CreateScheduleViewModel> _createScheduleViewModelFactory;

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
        public ICommand ShowCreateScheduleCommand { get; }

        /// <summary>
        /// Initializes the MainViewModel with view model factories.
        /// Sets up commands and sets the default view to PersonViewModel.
        /// </summary>
        public MainViewModel(Func<SubjectViewModel> courseViewModelFactory,
            Func<PersonViewModel> personViewModelFactory,
            Func<RoomViewModel> roomViewModelFactory,
            Func<CreateScheduleViewModel> createScheduleViewModelFactory)
        {
            // Assign factory methods
            _subjectViewModelFactory = courseViewModelFactory;
            _personViewModelFactory = personViewModelFactory;
            _roomViewModelFactory = roomViewModelFactory;
            _createScheduleViewModelFactory = createScheduleViewModelFactory;

            // Initialize commands for switching views
            ShowCourseCommand = new RelayCommand(ShowCourse);
            ShowPersonCommand = new RelayCommand(ShowPerson);
            ShowRoomCommand = new RelayCommand(ShowRoom);
            ShowCreateScheduleCommand = new RelayCommand(ShowCreateSchedule);

            // Set default view to PersonViewModel
            CurrentViewModel = _personViewModelFactory();
        }

        /// <summary>
        /// Switches the current view to CourseViewModel.
        /// </summary>
        private void ShowCourse() => CurrentViewModel = _subjectViewModelFactory();

        /// <summary>
        /// Switches the current view to PersonViewModel.
        /// </summary>
        private void ShowPerson() => CurrentViewModel = _personViewModelFactory();

        /// <summary>
        /// Switches the current view to RoomViewModel.
        /// </summary>
        private void ShowRoom() => CurrentViewModel = _roomViewModelFactory();

        private void ShowCreateSchedule() => CurrentViewModel = _createScheduleViewModelFactory();
    }
}
