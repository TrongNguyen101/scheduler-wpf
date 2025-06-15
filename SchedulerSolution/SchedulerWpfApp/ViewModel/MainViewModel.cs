using System.Windows.Input;
using SchedulerWpfApp.Helper;
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
        private readonly Func<GroupNameViewModel> _GroupNameViewModelFactory;
        private readonly Func<CreateScheduleViewModel> _createScheduleViewModelFactory;
        private readonly Func<LectureViewModel> _lectureViewModelFactory;
        private readonly Func<RoomViewModel> _roomViewModelFactory;
        private readonly Func<LectureSubjectViewModel> _lectureSubjectViewModelFactory;

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
        public ICommand ShowLectureCommand { get; }
        public ICommand ShowRoomlistCommand { get; }
        public ICommand ShowLectureSubjectCommand { get; }

        /// <summary>
        /// Initializes the MainViewModel with view model factories.
        /// Sets up commands and sets the default view to PersonViewModel.
        /// </summary>
        public MainViewModel(Func<SubjectViewModel> courseViewModelFactory,
            Func<RoomViewModel> roomViewModelFactory,
            Func<CreateScheduleViewModel> createScheduleViewModelFactory,
            Func<LectureViewModel> lectureViewModelFactory,
            Func<GroupNameViewModel> groupNameViewModelFactory,
            Func<LectureSubjectViewModel> lectureSubjectViewModelFactory)
        {
            // Assign factory methods
            _subjectViewModelFactory = courseViewModelFactory;
            _GroupNameViewModelFactory = groupNameViewModelFactory;
            _createScheduleViewModelFactory = createScheduleViewModelFactory;
            _lectureViewModelFactory = lectureViewModelFactory;
            _roomViewModelFactory = roomViewModelFactory;
            _lectureSubjectViewModelFactory = lectureSubjectViewModelFactory;

            // Initialize commands for switching views
            ShowCourseCommand = new RelayCommand(ShowCourse);
            ShowRoomCommand = new RelayCommand(ShowRoom);
            ShowCreateScheduleCommand = new RelayCommand(ShowCreateSchedule);
            ShowLectureCommand = new RelayCommand(ShowLecture);
            ShowRoomlistCommand = new RelayCommand(ShowRoomlist);
            ShowLectureSubjectCommand = new RelayCommand(ShowLectureSubject);
            // Set default view to PersonViewModel
            CurrentViewModel = _GroupNameViewModelFactory();
        }

        /// <summary>
        /// Switches the current view to CourseViewModel.
        /// </summary>
        private void ShowCourse() => CurrentViewModel = _subjectViewModelFactory();

        /// <summary>
        /// Switches the current view to RoomViewModel.
        /// </summary>
        private void ShowRoom() => CurrentViewModel = _GroupNameViewModelFactory();
        private void ShowCreateSchedule() => CurrentViewModel = _createScheduleViewModelFactory();
        private void ShowLecture() => CurrentViewModel = _lectureViewModelFactory();
        private void ShowLectureSubject () =>
            CurrentViewModel = _lectureSubjectViewModelFactory();
        private void ShowRoomlist() =>
            CurrentViewModel = _roomViewModelFactory();
    }
}
