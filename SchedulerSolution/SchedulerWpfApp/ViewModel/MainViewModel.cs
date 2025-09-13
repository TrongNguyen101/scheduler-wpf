using System.Windows.Input;
using SchedulerWpfApp.Helper;
using SchedulerWpfApp.Algorithm;
using SchedulerWpfApp.ServiceRefactor.Auth;

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

        private TypeTab _currentTab;
        // Factory delegates to lazily create view models
        private readonly Func<SubjectViewModel> _subjectViewModelFactory;
        private readonly Func<GroupNameViewModel> _GroupNameViewModelFactory;
        private readonly Func<CreateScheduleViewModel> _createScheduleViewModelFactory;
        private readonly Func<LectureViewModel> _lectureViewModelFactory;
        private readonly Func<RoomViewModel> _roomViewModelFactory;
        private readonly Func<LecturerSubjectViewModel> _lecturerSubjectViewModelFactory;
        private readonly Func<CurriculumViewModel> _curriculumViewModelFactory;
        private readonly Func<CurriculumSubjectViewModel> _curriculumSubjectViewModelFactory;
        private readonly Func<LoginViewModel> _loginViewModelFactory;
        private readonly AuthState _authState;
        private readonly AuthService _authService;

        public LoginViewModel LoginVm { get; }

        /// <summary>
        /// The current view model being shown in the main content area.
        /// Changing this will update the UI via data binding.
        /// </summary>
        public object CurrentViewModel
        {
            get => _currentViewModel;
            set { _currentViewModel = value; OnPropertyChanged(); }
        }

        public TypeTab CurrentTab
        {
            get => _currentTab;
            set
            {
                _currentTab = value;
                OnPropertyChanged();
            }
        }

        // Commands bound to buttons or menu items to switch views
        public ICommand ShowCourseCommand { get; }
        public ICommand ShowRoomCommand { get; }
        public ICommand ShowCreateScheduleCommand { get; }
        public ICommand ShowLectureCommand { get; }
        public ICommand ShowRoomlistCommand { get; }
        public ICommand ShowLectureSubjectCommand { get; }
        public ICommand ShowCurriculumCommand { get; }
        public ICommand ShowCurriculumSubjectCommand { get; }

        /// <summary>
        /// Initializes the MainViewModel with view model factories.
        /// Sets up commands and sets the default view to GroupNamViewModel.
        /// </summary>
        public MainViewModel(Func<SubjectViewModel> courseViewModelFactory,
            Func<RoomViewModel> roomViewModelFactory,
            Func<CreateScheduleViewModel> createScheduleViewModelFactory,
            Func<LectureViewModel> lectureViewModelFactory,
            Func<GroupNameViewModel> groupNameViewModelFactory,
            Func<LecturerSubjectViewModel> lectureSubjectViewModelFactory,
            Func<CurriculumViewModel> curriculumViewModelFactory,
            Func<CurriculumSubjectViewModel> curriculumSubjectViewModelFactory,
            Func<LoginViewModel> loginViewModelFactory,
            AuthState authState,
            AuthService authService)
        {
            // Assign factory methods
            _subjectViewModelFactory = courseViewModelFactory;
            _GroupNameViewModelFactory = groupNameViewModelFactory;
            _createScheduleViewModelFactory = createScheduleViewModelFactory;
            _lectureViewModelFactory = lectureViewModelFactory;
            _roomViewModelFactory = roomViewModelFactory;
            _lecturerSubjectViewModelFactory = lectureSubjectViewModelFactory;
            _curriculumViewModelFactory = curriculumViewModelFactory;
            _curriculumSubjectViewModelFactory = curriculumSubjectViewModelFactory;
            _loginViewModelFactory = loginViewModelFactory;
            _authState = authState;
            _authService = authService;
            LoginVm = _loginViewModelFactory();

            // Initialize commands for switching views
            ShowCourseCommand = new RelayCommand(ShowCourse);
            ShowRoomCommand = new RelayCommand(ShowRoom);
            ShowCreateScheduleCommand = new RelayCommand(ShowCreateSchedule);
            ShowLectureCommand = new RelayCommand(ShowLecture);
            ShowRoomlistCommand = new RelayCommand(ShowRoomlist);
            ShowLectureSubjectCommand = new RelayCommand(ShowLectureSubject);
            ShowCurriculumCommand = new RelayCommand(ShowCurriculumList);
            ShowCurriculumSubjectCommand = new RelayCommand(ShowCurriculumSubjectList);

            // Navigate based on auth state and listen for changes
            _authState.Changed += OnAuthChanged;
            UpdateViewForAuth();
            RefreshAuthBindings();

            LogoutCommand = new RelayCommand(() =>
            {
                _authService.Logout();
            });
        }

        private void OnAuthChanged()
        {
            UpdateViewForAuth();
            RefreshAuthBindings();
        }

        private void UpdateViewForAuth()
        {
            if (_authState.IsAuthenticated)
            {
                CurrentViewModel = _GroupNameViewModelFactory();
                CurrentTab = TypeTab.GroupName;
            }
            // When not authenticated, shell is hidden; Login overlay is shown via LoginVm
        }

        public string CurrentUsername => _authState?.CurrentUser?.username ?? string.Empty;
        public bool IsAuthenticated => _authState?.IsAuthenticated ?? false;

        public ICommand LogoutCommand { get; }

        private void RefreshAuthBindings()
        {
            OnPropertyChanged(nameof(CurrentUsername));
            OnPropertyChanged(nameof(IsAuthenticated));
        }

        /// <summary>
        /// Switches the current view to CourseViewModel.
        /// </summary>
        private void ShowCourse()
        {
            CurrentViewModel = _subjectViewModelFactory();
            CurrentTab = TypeTab.Subject;
        }

        /// <summary>
        /// Switches the current view to RoomViewModel.
        /// </summary>
        private void ShowRoom()
        {
            CurrentViewModel = _GroupNameViewModelFactory();
            CurrentTab = TypeTab.GroupName;
        }

        private void ShowCreateSchedule()
        {
            CurrentViewModel = _createScheduleViewModelFactory();
            CurrentTab = TypeTab.CreateSchedule;
        }

        private void ShowLecture()
        {
            CurrentViewModel = _lectureViewModelFactory();
            CurrentTab = TypeTab.Lecture;
        }
        private void ShowLectureSubject()
        {
            CurrentViewModel = _lecturerSubjectViewModelFactory();
            CurrentTab = TypeTab.LectureSubject;
        }

        private void ShowRoomlist()
        {
            CurrentViewModel = _roomViewModelFactory();
            CurrentTab = TypeTab.RoomList;
        }

        private void ShowCurriculumList()
        {
            CurrentViewModel = _curriculumViewModelFactory();
            CurrentTab = TypeTab.Curriculum;
        }

        private void ShowCurriculumSubjectList()
        {
            CurrentViewModel = _curriculumSubjectViewModelFactory();
            CurrentTab = TypeTab.CurriculumSubject;
        }
    }
}
