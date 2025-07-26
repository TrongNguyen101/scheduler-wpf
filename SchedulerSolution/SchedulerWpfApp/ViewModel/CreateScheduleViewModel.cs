using Microsoft.Win32;
using SchedulerWpfApp.Algorithm;
using SchedulerWpfApp.Helper;
using SchedulerWpfApp.Model;
using SchedulerWpfApp.ServiceRefactor.LecturerSubjectServices;
using SchedulerWpfApp.ServiceRefactor.RoomService;
using SchedulerWpfApp.ServiceRefactor.GroupNameService;
using SchedulerWpfApp.ServiceRefactor.ScheduleServices;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Input;
using SchedulerWpfApp.Algorithm.DTO;
using SchedulerWpfApp.ServiceRefactor.NotificationService;

namespace SchedulerWpfApp.ViewModel
{
    public class CreateScheduleViewModel : ViewBaseModel
    {
        #region Fields
        private readonly CreateScheduleTree _createScheduleTree;
        private readonly IScheduleServices _implementScheduleServices;
        private readonly IRoomService _roomService;
        private readonly ILecturerSubjectServices _lecturerSubjectServices;
        private readonly IGroupNameService _groupNameService;
        private readonly INotificationService _notificationService;

        private int _selectedYear;
        private string _selectedWeek;
        private string _selectedGroupName;
        private string _selectedRoomName;
        private string _selectedLecturer;
        private ObservableCollection<string> _groupNames;
        private bool _isEditScheduleFormOpen = false; // Flag to track if the cell is being edited
        private Schedule _editingSchedule;
        private ObservableCollection<Room> _listRooms;
        private ObservableCollection<LecturerSubject> _lecturerSubjects; // All rooms loaded from the service
        private Room _selectedRoom;
        private LecturerSubject _selectedLecturerSubject;
        private ObservableCollection<string> _rooms;
        private ObservableCollection<string> _lecturers; // List of lecturers to display in the timetable

        private bool _isScheduleFormOpen;
        private ObservableCollection<string> _listMajors;
        private string _selectedMajorFirstCombo;
        private string _selectedMajorSecondCombo;

        private ObservableCollection<string> _listMajorGroupA;
        private ObservableCollection<string> _listMajorGroupB;

        private ObservableCollection<string> _allMajorsBackup;
        private DateTime _selectedDate = DateTime.Now;
        #endregion

        #region Constructor
        public ObservableCollection<DateTime> WeekDays { get; set; } = new();
        public ObservableCollection<int> Slots { get; set; } = new() { 1, 2, 3, 4, 5, 6, 7, 8 }; // List of available time slots in a day
        public ObservableCollection<int> Years { get; set; } = new(Enumerable.Range(DateTime.Now.Year - 2, 5)); // List of years from 2 years ago to next 2 years
        public ObservableCollection<string> Weeks { get; set; } = new(); // List of weeks in "dd/MM - dd/MM" format
        public ObservableCollection<string> GroupNames { get => _groupNames; set => SetProperty(ref _groupNames, value); }// List of group names to filter schedules
        public ObservableCollection<string> Rooms { get => _rooms; set => SetProperty(ref _rooms, value); } // List of rooms to filter schedules
        public ObservableCollection<string> Lecturers
        {
            get => _lecturers;
            set => SetProperty(ref _lecturers, value); // List of lecturers to filter schedules
        }
        public ObservableCollection<SlotRowViewModel> SlotRows { get; set; } = new(); // List of slot rows for the timetable
        private ObservableCollection<Schedule> AllSchedules { get; set; } = new(); // All schedules loaded from the service
        public ObservableCollection<string> SlotStatusList { get; set; } = new() { "ON", "OFF" };
        public ObservableCollection<Room> ListRooms
        {
            get => _listRooms;
            set => SetProperty(ref _listRooms, value);
        }

        public ObservableCollection<LecturerSubject> LecturerSubjects
        {
            get => _lecturerSubjects;
            set => SetProperty(ref _lecturerSubjects, value);
        }

        public enum DisplayMode
        {
            CLASS,
            ROOM,
            LECTURER
        }

        private DisplayMode _currentDisplayMode = DisplayMode.CLASS;

        public DisplayMode CurrentDisplayMode
        {
            get => _currentDisplayMode;
            set
            {
                SetProperty(ref _currentDisplayMode, value); // Set the current display mode and notify property change
                FilterSchedules();
            }
        }

        public int SelectedYear
        {
            get => _selectedYear;
            set { SetProperty(ref _selectedYear, value); GenerateWeeks(); } // Regenerate week list based on the selected year
        }

        public string SelectedWeek
        {
            get => _selectedWeek;
            set { SetProperty(ref _selectedWeek, value); FilterSchedules(); } // Filter schedules based on the selected week
        }

        public string SelectedGroupName
        {
            get => _selectedGroupName;
            set { SetProperty(ref _selectedGroupName, value); FilterSchedules(); } // Filter schedules based on the selected group name
        }

        public bool IsEditScheduleFormOpen
        {
            get => _isEditScheduleFormOpen;
            set => SetProperty(ref _isEditScheduleFormOpen, value);
        }

        public Schedule EditingSchedule
        {
            get => _editingSchedule;
            set
            {
                SetProperty(ref _editingSchedule, value);
                IsEditScheduleFormOpen = value != null; // Open edit form if a schedule is being edited
                if (value != null)
                {
                    // Load the rooms for the selected schedule
                    _ = GetAllRooms();
                    _ = GetAllLecturerBySubjectCode(value.SubjectCode); // Load lecturers for the selected subject code
                    SelectedRoom = ListRooms.FirstOrDefault(r => r.RoomName == EditingSchedule?.RoomName);
                    SelectedLecturerSubject = LecturerSubjects.FirstOrDefault(l => l.LecturerId == EditingSchedule?.LecturerId);
                }
            }
        }

        public Room SelectedRoom
        {
            get => _selectedRoom;
            set
            {
                SetProperty(ref _selectedRoom, value);
                if (EditingSchedule != null && value != null)
                {
                    EditingSchedule.RoomName = value.RoomName;
                    EditingSchedule.RoomId = value.RoomId;
                }
            }
        }

        public LecturerSubject SelectedLecturerSubject
        {
            get => _selectedLecturerSubject;
            set
            {
                SetProperty(ref _selectedLecturerSubject, value);
                if (EditingSchedule != null && value != null)
                {
                    EditingSchedule.LecturerName = value.LecturerName;
                    EditingSchedule.LecturerId = value.LecturerId;
                }
            }
        }

        public string SelectedRoomName
        {
            get => _selectedRoomName;
            set { SetProperty(ref _selectedRoomName, value); FilterSchedules(); } // Filter schedules based on the selected room
        }

        public string SelectedLecturer
        {
            get => _selectedLecturer;
            set { SetProperty(ref _selectedLecturer, value); FilterSchedules(); } // Filter schedules based on the selected lecturer
        }

        public bool IsScheduleFormOpen
        {
            get => _isScheduleFormOpen;
            set => SetProperty(ref _isScheduleFormOpen, value);
        }

        public ObservableCollection<string> ListMajors
        {
            get => _listMajors;
            set => SetProperty(ref _listMajors, value);
        }

        public ObservableCollection<string> ListMajorGroupA
        {
            get => _listMajorGroupA;
            set => SetProperty(ref _listMajorGroupA, value);
        }

        public ObservableCollection<string> ListMajorGroupB
        {
            get => _listMajorGroupB;
            set => SetProperty(ref _listMajorGroupB, value);
        }

        public string SelectedMajorFirstCombo
        {
            get => _selectedMajorFirstCombo;
            set
            {
                if (SetProperty(ref _selectedMajorFirstCombo, value) && !string.IsNullOrEmpty(value))
                {
                    AddMajorToA(value);
                }
            }
        }

        public string SelectedMajorSecondCombo
        {
            get => _selectedMajorSecondCombo;
            set
            {
                if (SetProperty(ref _selectedMajorSecondCombo, value) && !string.IsNullOrEmpty(value))
                {
                    AddMajorToB(value);
                }
            }
        }

        public DateTime SelectedDate
        {
            get => _selectedDate;
            set => SetProperty(ref _selectedDate, value);
        }

        public ICommand CreateScheduleCommand { get; }
        public ICommand ExportExcelCommand { get; }
        public ICommand UpdateScheduleCommand { get; }
        public ICommand CancelEditScheduleCommand { get; }
        public ICommand SetDisplayModeCommand { get; }

        public ICommand OpenPopupCreateCommand { get; }
        public ICommand CancelCreateScheduleCommand { get; }

        public ObservableCollection<string> FilteredMajorFirst { get; set; } = new();
        public ObservableCollection<string> FilteredMajorSecond { get; set; } = new();

        public ObservableCollection<string> FilteredSubjectFullOnl { get; set; } = new();
        public ObservableCollection<string> FilteredSubjectFullOff { get; set; } = new();

        public ICommand RemoveMajorFirstItemCommand => new RelayCommandGeneric<string>(major =>
        {
            ListMajorGroupA.Remove(major);
            AddMajorSecondToAvailable(major);
            RefreshFilteredMajors();
        });

        public ICommand RemoveMajorSecondItemCommand => new RelayCommandGeneric<string>(major =>
        {
            ListMajorGroupB.Remove(major);
            AddMajorSecondToAvailable(major);
            RefreshFilteredMajors();
        });

        public CreateScheduleViewModel(CreateScheduleTree createScheduleTree, IScheduleServices implementScheduleServices, IRoomService roomService, ILecturerSubjectServices lecturerSubjectServices, IGroupNameService groupNameService, INotificationService notificationService)
        {
            _createScheduleTree = createScheduleTree;
            _implementScheduleServices = implementScheduleServices;
            _roomService = roomService;
            _lecturerSubjectServices = lecturerSubjectServices;
            _groupNameService = groupNameService;
            _notificationService = notificationService;

            SelectedYear = DateTime.Now.Year; // Default to current year
            CreateScheduleCommand = new RelayCommand(async () => await CreateScheduleDemo());
            ExportExcelCommand = new RelayCommand(async () => await ExportSchedulesToExcel());
            UpdateScheduleCommand = new RelayCommand(async () => await UpdateSchedule());
            CancelEditScheduleCommand = new RelayCommand(() => CancelEditSchedule());

            SetDisplayModeCommand = new RelayCommandGeneric<string>(ChangeDisplayMode);
            OpenPopupCreateCommand = new RelayCommand(OpenScheduleForm); // Command to open the schedule creation popup
            CancelCreateScheduleCommand = new RelayCommand(CancelScheduleForm); // Command to close the schedule creation popup

            ListMajorGroupA = new ObservableCollection<string>();
            ListMajorGroupB = new ObservableCollection<string>();
            _allMajorsBackup = new ObservableCollection<string>();

            LoadMockSchedules(); // Load initial schedules from the service
            InitCurrentWeekDays(); // Initialize current week days
            FilterSchedules(); // Filter schedules based on initial selections
            InitListMajors();
        }
        #endregion

        #region Methods
        private void ChangeDisplayMode(string mode)
        {
            if (mode == "CLASS") CurrentDisplayMode = DisplayMode.CLASS;
            else if (mode == "ROOM") CurrentDisplayMode = DisplayMode.ROOM;
            else CurrentDisplayMode = DisplayMode.LECTURER; // Change the display mode based on the selected option
        }

        /// <summary>
        /// Initializes the ListMajors collection with all available majors from the service.
        /// </summary>
        private async void InitListMajors()
        {
            var allMajors = await _groupNameService.GetAllMajorAsync();
            ListMajors = new ObservableCollection<string>(allMajors);

            _allMajorsBackup = new ObservableCollection<string>(allMajors);
        }

        private void OpenScheduleForm()
        {
            // Reset major selections when opening form
            RestoreAllMajors();
            IsScheduleFormOpen = true;
        }

        private void CancelScheduleForm()
        {
            // Reset major selections when canceling
            RestoreAllMajors();
            IsScheduleFormOpen = false;
        }

        private void AddMajorToA(string major)
        {
            if (!ListMajorGroupA.Contains(major))
            {
                ListMajorGroupA.Add(major);
                ListMajors.Remove(major);
                SelectedMajorFirstCombo = null;
                RefreshFilteredMajors();
            }
        }

        private void AddMajorToB(string major)
        {
            if (!ListMajorGroupB.Contains(major))
            {
                ListMajorGroupB.Add(major);
                ListMajors.Remove(major);
                SelectedMajorSecondCombo = null;
                RefreshFilteredMajors();
            }
        }

        private void RefreshFilteredMajors()
        {
            FilteredMajorFirst = new ObservableCollection<string>(
                ListMajors.Where(m => !ListMajorGroupA.Contains(m))  // Loại bỏ items đã chọn trong First
            );
            FilteredMajorSecond = new ObservableCollection<string>(
                ListMajors.Where(m => !ListMajorGroupB.Contains(m)) // Loại bỏ items đã chọn trong Second
            );

            OnPropertyChanged(nameof(FilteredMajorFirst));
            OnPropertyChanged(nameof(FilteredMajorSecond));
        }

        private void RestoreAllMajors()
        {
            ListMajorGroupA.Clear();
            ListMajorGroupB.Clear();
            ListMajors = new ObservableCollection<string>(_allMajorsBackup);
            RefreshFilteredMajors();
        }

        private void AddMajorSecondToAvailable(string major)
        {
            // Kiểm tra xem major đã tồn tại chưa
            if (!ListMajors.Contains(major))
            {
                var sortedList = ListMajors.ToList();
                sortedList.Add(major);
                sortedList.Sort();

                var index = sortedList.IndexOf(major);
                ListMajors.Insert(index, major);
            }
        }

        /// <summary>
        /// Initializes the WeekDays collection with the current week's dates starting from Monday.
        /// </summary>
        private void InitCurrentWeekDays()
        {
            WeekDays.Clear();
            DateTime today = DateTime.Today; // Get today's date
            int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7; // Calculate the difference to the last Monday
            DateTime monday = today.AddDays(-diff); // Get the last Monday date

            for (int i = 0; i < 7; i++)
            {
                WeekDays.Add(monday.AddDays(i)); // Add each day of the week starting from Monday
            }
        }

        /// <summary>
        /// Generates the list of weeks for the selected year in "dd/MM - dd/MM" format.
        /// Generate weeks for the selected year.
        /// </summary>
        private void GenerateWeeks()
        {
            Weeks.Clear();
            // One year has 52 weeks, so we generate weeks from 1 to 52
            for (int i = 1; i <= 52; i++)
            {
                DateTime start = FirstDateOfWeek(SelectedYear, i);
                DateTime end = start.AddDays(6);
                Weeks.Add($"{start:dd/MM} - {end:dd/MM}");
            }
        }

        /// <summary>
        /// Calculates the first date of the specified week in the given year.
        /// </summary>
        /// <param name="year"></param>
        /// <param name="weekOfYear"></param>
        /// <returns>First date of the specified week</returns>
        private DateTime FirstDateOfWeek(int year, int weekOfYear)
        {
            DateTime jan1 = new(year, 1, 1); // January 1st of the specified year
            int daysOffset = DayOfWeek.Monday - jan1.DayOfWeek; // Calculate the offset to the first Monday of the year
            DateTime firstMonday = jan1.AddDays(daysOffset); // Get the first Monday of the year
            return firstMonday.AddDays((weekOfYear - 1) * 7); // Calculate the first date of the specified week
        }

        /// <summary>
        /// Filters the schedules based on the selected group name and week, and updates the SlotRows collection.
        /// </summary>
        private void FilterSchedules()
        {
            SlotRows.Clear();

            if (string.IsNullOrEmpty(SelectedWeek) || (CurrentDisplayMode == DisplayMode.CLASS && string.IsNullOrEmpty(SelectedGroupName)) || (CurrentDisplayMode == DisplayMode.ROOM && string.IsNullOrEmpty(SelectedRoomName)) || (CurrentDisplayMode == DisplayMode.LECTURER && string.IsNullOrEmpty(SelectedLecturer)))
            {
                // If no group or week is selected, create empty rows
                GenerateTimetableCellsAndSlotRows(new List<Schedule>());
                return;
            }

            var (start, end) = ParseSelectedWeekToDates();

            WeekDays.Clear();
            for (int i = 0; i < 7; i++) WeekDays.Add(start.AddDays(i)); // Add each day of the week starting from the start date

            var filtered = new List<Schedule>();
            if (CurrentDisplayMode == DisplayMode.ROOM)
                filtered = AllSchedules.Where(s => s.RoomName == SelectedRoomName && s.StatusSlot == ScheduleConstants.StatusSlotIsOffline && s.Date >= start && s.Date <= end).ToList(); // Filter schedules by group name and date range
            else if (CurrentDisplayMode == DisplayMode.CLASS)
                filtered = AllSchedules.Where(s => s.GroupName == SelectedGroupName && s.Date >= start && s.Date <= end).ToList(); // Filter schedules by group name and date range
            else filtered = AllSchedules.Where(s => s.LecturerAccount == SelectedLecturer && s.Date >= start && s.Date <= end).ToList(); // Filter schedules by lecturer and date range
            GenerateTimetableCellsAndSlotRows(filtered); // Generate timetable cells and slot rows based on the filtered schedules
        }

        /// <summary>
        /// Parses the selected week string in "dd/MM - dd/MM" format to a tuple of start and end dates.
        /// </summary>
        /// <returns>Start and end date</returns>
        private (DateTime start, DateTime end) ParseSelectedWeekToDates()
        {
            var parts = SelectedWeek.Split(" - "); // Split the selected week string into start and end parts
            DateTime start = DateTime.ParseExact(parts[0], "dd/MM", CultureInfo.InvariantCulture).AddYears(SelectedYear - DateTime.Now.Year);
            DateTime end = DateTime.ParseExact(parts[1], "dd/MM", CultureInfo.InvariantCulture).AddYears(SelectedYear - DateTime.Now.Year);
            return (start, end);
        }

        /// <summary>
        /// Loads mock schedules from the service and populates the AllSchedules and GroupNames collections.
        /// </summary>
        private async void LoadMockSchedules()
        {
            var schedules = await _implementScheduleServices.GetAllAsync(); // Fetch all schedules from the service
            AllSchedules = new ObservableCollection<Schedule>(schedules); // Store all schedules in the AllSchedules collection

            if (schedules?.Any() == true)
            {
                GroupNames = new ObservableCollection<string>(schedules.Select(s => s.GroupName).Distinct().OrderBy(name => name)); // Get distinct group names from the schedules
                Rooms = new ObservableCollection<string>(schedules.Select(s => s.RoomName).Distinct().OrderBy(name => name)); // Get distinct room names from the schedules
                Lecturers = new ObservableCollection<string>(schedules.Select(s => s.LecturerAccount).Distinct().OrderBy(name => name)); // Get distinct lecturer IDs from the schedules
            }
        }

        /// <summary>
        /// Generates a demo schedule starting from a specific date and exports it to an Excel file.
        /// </summary>
        private async Task CreateScheduleDemo()
        {
            DateTime startDate = new DateTime(2025, 01, 06);

            //if (ListMajorGroupA.Count <= 0 || ListMajorGroupB.Count <= 0)
            //{
            //    MessageBox.Show("Vui lòng chọn đầy đủ thông tin trước khi tạo lịch.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            //    IsScheduleFormOpen = true;
            //}
            //else
            //{
            //var schedules = await _createScheduleTree.GenerateSchedules(SelectedDate, ListMajorGroupA.ToList(), ListMajorGroupB.ToList());
            var schedules = await _createScheduleTree.GenerateSchedules();

            LoadMockSchedules(); // Reload schedules after generating new ones
                                 // PrintTimetableGroupByWeek(schedules); // Print the timetable grouped by week for debugging purposes
            if (schedules == null || !schedules.Any())
                _notificationService.ShowInfo("Không có lịch nào được tạo.");
            else
                _notificationService.ShowSuccess("Tạo lịch thành công.");
            //}
        }

        /// <summary>
        /// Exports the list of schedules to an Excel file at the specified file path.
        /// </summary>
        private async Task ExportSchedulesToExcel()
        {
            var schedules = await _implementScheduleServices.GetAllAsync(); // Get the current list of schedules to export

            if (schedules == null || !schedules.Any())
            {
                _notificationService.ShowInfo("Không có lịch nào để xuất.");
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = "Schedules.xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    _implementScheduleServices.ExportToExcel(schedules, dialog.FileName); // Export schedules to the selected Excel file
                }
                catch (Exception ex)
                {
                    _notificationService.ShowError("Có lỗi trong quá trình xuất lịch học");
                }
            }
        }

        /// <summary>
        /// Print timetable grouped by week for each class, including detailed information for each slot.
        /// </summary>
        /// <param name="schedules"></param>
        public static void PrintTimetableGroupByWeek(List<Schedule> schedules)
        {
            if (schedules == null || !schedules.Any())
            {
                Debug.WriteLine("No schedules available.");
                return;
            }

            // Nhóm theo ClassId
            var groupedByClass = schedules.GroupBy(s => s.GroupName).OrderBy(g => g.Key);

            foreach (var classGroup in groupedByClass)
            {
                Debug.WriteLine($"TIMETABLE FOR CLASS: {classGroup.Key}");
                Debug.WriteLine($"Room: {classGroup.First().RoomName} | Session: {classGroup.First().PartOfDay}");
                Debug.WriteLine("=============================================================");

                // Nhóm theo tuần (dựa trên ngày bắt đầu tuần)
                var groupedByWeek = classGroup
                    .GroupBy(s => GetWeekStartDate(s.Date))
                    .OrderBy(g => g.Key);
                int weekCount = 0;
                foreach (var weekGroup in groupedByWeek)
                {
                    weekCount++;
                    DateTime weekStart = weekGroup.Key ?? DateTime.MinValue;
                    DateTime weekEnd = weekStart.AddDays(6);

                    Debug.WriteLine($"Week {weekCount}: {weekStart:dd/MM/yyyy} - {weekEnd:dd/MM/yyyy}");

                    // Lấy danh sách ngày trong tuần này
                    //var dates = weekGroup.Select(s => s.Date)
                    //    .Distinct()
                    //    .OrderBy(d => d)
                    //    .ToList();

                    // MỚI: Lấy đủ 7 ngày từ thứ Hai đến Chủ nhật
                    var dates = Enumerable.Range(0, 7)
                        .Select(offset => weekStart.AddDays(offset))
                        .ToList();

                    // Danh sách slot duy nhất
                    var slots = weekGroup.Select(s => s.SlotTime).Distinct().OrderBy(s => s).ToList();

                    // Độ rộng cột cố định
                    const int columnWidth = 40;

                    // Tạo tiêu đề cột (ngày và thứ)
                    StringBuilder header = new StringBuilder();
                    header.Append("Slot      ".PadRight(10) + "| ");
                    foreach (var date in dates)
                    {
                        header.Append($"{date:dd/MM/yyyy} ({date:ddd})".PadRight(columnWidth) + "| ");
                    }
                    Debug.WriteLine(header.ToString());
                    Debug.WriteLine(new string('-', header.Length));

                    // In từng hàng (slot)
                    foreach (var slot in slots)
                    {
                        // Mỗi slot có 3 dòng (Subject, Lecturer, Status)
                        string[] rowLines = new string[8];
                        rowLines[0] = $"Slot {slot}".PadRight(10) + "| "; // Dòng đầu tiên bắt đầu bằng slot
                        rowLines[1] = "".PadRight(10) + "| ";   // Dòng thứ hai và ba để trống ở cột slot
                        rowLines[2] = "".PadRight(10) + "| ";
                        rowLines[3] = "".PadRight(10) + "| ";
                        rowLines[4] = "".PadRight(10) + "| ";
                        rowLines[5] = "".PadRight(10) + "| ";
                        rowLines[6] = "".PadRight(10) + "| ";
                        rowLines[7] = "".PadRight(10) + "| ";

                        foreach (var date in dates)
                        {
                            var schedule = weekGroup.FirstOrDefault(s => s.Date == date && s.SlotTime == slot);
                            if (schedule != null)
                            {
                                if (schedule.SubjectCode == "")
                                {
                                    rowLines[0] += "".PadRight(columnWidth) + "| ";
                                    rowLines[1] += "".PadRight(columnWidth) + "| ";
                                    rowLines[2] += "".PadRight(columnWidth) + "| ";
                                    rowLines[3] += "".PadRight(columnWidth) + "| ";
                                    rowLines[4] += "".PadRight(columnWidth) + "| ";
                                    rowLines[5] += "".PadRight(columnWidth) + "| ";
                                    rowLines[6] += "".PadRight(columnWidth) + "| ";
                                    rowLines[7] += "".PadRight(columnWidth) + "| ";
                                }
                                else
                                {
                                    rowLines[0] += $"Subject: {schedule.SubjectCode}".PadRight(columnWidth) + "| ";
                                    rowLines[1] += $"Lecturer: {schedule.LecturerId}".PadRight(columnWidth) + "| ";
                                    rowLines[2] += $"Slot type: {schedule.StatusSlot}".PadRight(columnWidth) + "| ";
                                    rowLines[3] += $"Session: {schedule.PartOfDay}".PadRight(columnWidth) + "| ";
                                    rowLines[4] += $"Room: {schedule.RoomName}".PadRight(columnWidth) + "| ";
                                    rowLines[5] += $"Slot code: {schedule.SlotTypeCode}".PadRight(columnWidth) + "| ";
                                    rowLines[6] += $"Session No: {schedule.SessionNo}".PadRight(columnWidth) + "| ";
                                    rowLines[7] += $"Class: {schedule.GroupName}".PadRight(columnWidth) + "| ";
                                }
                            }
                            else
                            {
                                rowLines[0] += "".PadRight(columnWidth) + "| ";
                                rowLines[1] += "".PadRight(columnWidth) + "| ";
                                rowLines[2] += "".PadRight(columnWidth) + "| ";
                                rowLines[3] += "".PadRight(columnWidth) + "| ";
                                rowLines[4] += "".PadRight(columnWidth) + "| ";
                                rowLines[5] += "".PadRight(columnWidth) + "| ";
                                rowLines[6] += "".PadRight(columnWidth) + "| ";
                                rowLines[7] += "".PadRight(columnWidth) + "| ";
                            }
                        }

                        // In 3 dòng của hàng
                        Debug.WriteLine(rowLines[0]);
                        Debug.WriteLine(rowLines[1]);
                        Debug.WriteLine(rowLines[2]);
                        Debug.WriteLine(rowLines[3]);
                        Debug.WriteLine(rowLines[4]);
                        Debug.WriteLine(rowLines[5]);
                        Debug.WriteLine(rowLines[6]);
                        Debug.WriteLine(rowLines[7]);

                        Debug.WriteLine(new string('-', header.Length));
                    }

                    Debug.WriteLine(new string('=', header.Length));
                    Debug.WriteLine(""); // Thêm khoảng cách giữa các tuần
                }
                Debug.WriteLine("\n"); // Thêm khoảng cách giữa các lớp
            }
        }

        /// <summary>
        /// Calculates the start date of the week for a given date.
        /// </summary>
        /// <param name="date"></param>
        /// <returns>Start date of week</returns>

        private static DateTime? GetWeekStartDate(DateTime? date)
        {
            if (!date.HasValue)
                return null;

            int diff = (7 + (date.Value.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.Value.AddDays(-diff).Date;
        }

        public static DateTime GetWeekStartDate(DateTime date)
        {
            int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.AddDays(-1 * diff).Date;
        }

        /// <summary>
        /// Generates timetable cells and slot rows based on the filtered schedules.
        /// </summary>
        /// <param name="filtered"></param>
        private void GenerateTimetableCellsAndSlotRows(List<Schedule> filtered)
        {
            SlotRows.Clear();

            foreach (var slot in Slots)
            {
                var cells = new ObservableCollection<TimetableCellViewModel>();

                foreach (var day in WeekDays)
                {
                    var match = filtered.FirstOrDefault(s => s.Date?.Date == day.Date && s.SlotTime == slot); // Check if there is a schedule for this day and slot
                    string slotTime = "";
                    if (match != null)
                    {
                        slotTime = CalculatorTime(slot, match.TypeSlot); // Calculate time for new slot
                    }

                    cells.Add(new TimetableCellViewModel
                    {
                        DayOfWeek = day,
                        SlotNumber = slot,
                        SlotTime = slotTime,
                        Schedule = match,
                        ParentViewModel = this // Set the parent view model for drag-and-drop functionality
                    });
                }

                SlotRows.Add(new SlotRowViewModel
                {
                    SlotNumber = slot,
                    Cells = cells
                });
            }
        }

        /// <summary>
        /// Calculator start time and end time for each slot
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="typeSlot"></param>
        /// <returns></returns>
        private string CalculatorTime(int slot, string typeSlot)
        {
            int minutesPerSlot = 135; // Default slot duration of NewSlot
            int minutesPerBreak = 60; // Default break duration of NewSlot

            if (typeSlot == "OldSlot")
            {
                minutesPerSlot = 90; // Old slots have a different duration
                minutesPerBreak = 30; // Shorter break for old slots
            }

            // Calculate the start and end times based on the slot number
            double startHour = ((slot - 1) * minutesPerSlot) + ((slot - 1) * 15) + (minutesPerSlot == 135 && slot >= 3 ? minutesPerBreak : slot >= 4 ? minutesPerBreak : 0);
            // Start hour of slot
            DateTime startDate = new DateTime(2025, 1, 1, 7, 0, 0).AddMinutes(startHour);

            double endHour = startHour + minutesPerSlot;
            //End hour of slot
            DateTime endDate = new DateTime(2025, 1, 1, 7, 0, 0).AddMinutes(endHour);

            return $"{startDate:HH:mm} - {endDate:HH:mm}"; // Return the formatted time range
        }

        /// <summary>
        /// Handles the drag and drop operation between timetable cells
        /// </summary>
        /// <param name="sourceCell">Source cell where drag started</param>
        /// <param name="targetCell">Target cell where item was dropped</param>
        /// <param name="droppedSchedule">The schedule being moved</param>
        public async Task HandleScheduleDrop(TimetableCellViewModel sourceCell, TimetableCellViewModel targetCell, Schedule droppedSchedule)
        {
            try
            {
                // Validate the drop operation
                if (!ValidateScheduleMove(sourceCell, targetCell, droppedSchedule))
                {
                    _notificationService.ShowError("Đã bị trùng lịch. Không thể di chuyển slot này.");
                    return;
                }
                if (targetCell.Schedule != null)
                {
                    MessageBoxResult result = MessageBox.Show($@"Bạn có muốn chuyển đổi slot của môn
                    {droppedSchedule.SubjectCode} ngày {sourceCell.DayOfWeek:dd / MM / yyyy} {sourceCell.SlotNumber}
                    với môn {targetCell.Schedule.SubjectCode} ngày {targetCell.DayOfWeek:dd / MM / yyyy} {targetCell.SlotNumber} không?", "Xác nhận", MessageBoxButton.YesNo
                        , MessageBoxImage.Question, MessageBoxResult.Yes);

                    if (result == MessageBoxResult.No)
                    {
                        return; // User chose not to swap, exit the method
                    }
                    else
                    {
                        await HandelSwapSchedule(sourceCell, targetCell, droppedSchedule); // Swap schedules
                    }
                }
                else
                {
                    await HandelSwapSchedule(sourceCell, targetCell, droppedSchedule); // Swap schedules
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Có lỗi khi di chuyển lịch. Vui lòng thử lại."); // Show error notification
            }
        }

        /// <summary>
        /// Handles the swapping of schedules between two timetable cells.
        /// </summary>
        /// <param name="sourceCell"></param>
        /// <param name="targetCell"></param>
        /// <param name="droppedSchedule"></param>
        /// <returns></returns>
        public async Task HandelSwapSchedule(TimetableCellViewModel sourceCell, TimetableCellViewModel targetCell, Schedule droppedSchedule)
        {
            try
            {
                // Store the target cell's current schedule (for swapping)
                var targetSchedule = targetCell.Schedule;

                // Update the schedule dates and times
                var updatedSourceSchedule = CreateUpdatedSchedule(droppedSchedule, targetCell.DayOfWeek, targetCell.SlotNumber);

                // Update UI
                targetCell.Schedule = updatedSourceSchedule;

                int slotTargetCellInt = targetCell.SlotNumber;
                targetCell.SlotTime = CalculatorTime(slotTargetCellInt, updatedSourceSchedule.TypeSlot);
                sourceCell.Schedule = targetSchedule; // This might be null (empty slot) or another schedule

                // Update the schedule in the underlying data if targetSchedule is not null
                if (targetSchedule != null)
                {
                    var updatedTargetSchedule = CreateUpdatedSchedule(targetSchedule, sourceCell.DayOfWeek, sourceCell.SlotNumber);
                    sourceCell.Schedule = updatedTargetSchedule;
                    int slotSourceCellInt = sourceCell.SlotNumber;
                    sourceCell.SlotTime = CalculatorTime(slotSourceCellInt, updatedTargetSchedule.TypeSlot);

                    // Update in AllSchedules collection
                    var targetIndex = AllSchedules.IndexOf(targetSchedule);
                    if (targetIndex >= 0)
                    {
                        AllSchedules[targetIndex] = updatedTargetSchedule;
                    }

                    // Optionally save changes to database/service
                    await _implementScheduleServices.UpdateScheduleAsync(updatedTargetSchedule);
                }

                // Update the moved schedule in AllSchedules collection
                var sourceIndex = AllSchedules.IndexOf(droppedSchedule);
                if (sourceIndex >= 0)
                {
                    AllSchedules[sourceIndex] = updatedSourceSchedule;
                }

                // Optionally save changes to database/service
                await _implementScheduleServices.UpdateScheduleAsync(updatedSourceSchedule);
            }
            catch (Exception ex)
            {
                _notificationService.ShowError("Có lỗi khi hoán đổi lịch. Vui lòng thử lại."); // Show error notification
            }
        }

        /// <summary>
        /// Checks if there is a conflict with the room for the given schedule, date, and slot time.
        /// </summary>
        /// <param name="schedule"></param>
        /// <param name="date"></param>
        /// <param name="slotTime"></param>
        /// <returns></returns>
        private bool CheckConflictRoom(Schedule schedule, DateTime date, int slotTime)
        {
            return AllSchedules.Any(s => s.RoomId == schedule.RoomId &&
                                         s.Date == date &&
                                         s.SlotTime == slotTime &&
                                         s.StatusSlot == "OFF" &&
                                         s.ScheduleId != schedule.ScheduleId);
        }

        /// <summary>
        /// Checks if there is a conflict with the lecturer for the given schedule, date, and slot time.
        /// </summary>
        /// <param name="schedule"></param>
        /// <param name="date"></param>
        /// <param name="slotTime"></param>
        /// <returns></returns>
        private bool CheckConflictLecturer(Schedule schedule, DateTime date, int slotTime)
        {
            return AllSchedules.Any(s => s.LecturerId == schedule.LecturerId &&
                                         s.Date == date &&
                                         s.SlotTime == slotTime &&
                                         s.ScheduleId != schedule.ScheduleId);
        }
        /// <summary>
        /// Validates if a schedule can be moved to the target cell
        /// </summary>
        private bool ValidateScheduleMove(TimetableCellViewModel sourceCell, TimetableCellViewModel targetCell, Schedule schedule)
        {
            // Basic validation
            if (sourceCell == targetCell)
                return false;

            if (schedule.StatusSlot == "OFF")
            {
                var checkRoom = CheckConflictRoom(schedule, targetCell.DayOfWeek.Date, targetCell.SlotNumber);

                if (checkRoom)
                {
                    return false;
                }
            }

            if (targetCell.Schedule != null)
            {
                var targetSchedule = targetCell.Schedule;
                if (targetSchedule.StatusSlot == "OFF")
                {
                    var checkRoom = CheckConflictRoom(targetSchedule, sourceCell.DayOfWeek.Date, sourceCell.SlotNumber);

                    if (checkRoom)
                    {
                        return false;
                    }
                }

                var conflictingTargetSchedule = CheckConflictLecturer(targetSchedule, sourceCell.DayOfWeek.Date, sourceCell.SlotNumber);

                if (conflictingTargetSchedule)
                {
                    return false; // Lecturer conflict
                }
            }

            var conflictingSchedule = CheckConflictLecturer(schedule, targetCell.DayOfWeek.Date, targetCell.SlotNumber);

            if (conflictingSchedule)
            {
                return false; // Lecturer conflict
            }

            // Add more validation rules as needed:
            return true;
        }

        /// <summary>
        /// Creates a new schedule with updated date and slot time
        /// </summary>
        private Schedule CreateUpdatedSchedule(Schedule originalSchedule, DateTime newDate, int newSlotTime)
        {
            return new Schedule
            {
                ScheduleId = originalSchedule.ScheduleId,
                RoomId = originalSchedule.RoomId,
                RoomName = originalSchedule.RoomName,
                PartOfDay = originalSchedule.PartOfDay,
                SlotTime = newSlotTime,
                StatusSlot = originalSchedule.StatusSlot,
                Date = newDate,
                Major = originalSchedule.Major,
                SubjectCode = originalSchedule.SubjectCode,
                GroupName = originalSchedule.GroupName,
                LecturerId = originalSchedule.LecturerId,
                LecturerName = originalSchedule.LecturerName,
                SlotTypeCode = originalSchedule.SlotTypeCode,
                TypeSlot = originalSchedule.TypeSlot,
                SessionNo = originalSchedule.SessionNo,
                //Lecturer = originalSchedule.Lecturer,
                //Room = originalSchedule.Room
            };
        }

        /// <summary>
        /// Updates the selected schedule with new values and saves it to the service.
        /// </summary>
        /// <returns></returns>
        private async Task UpdateSchedule()
        {
            try
            {
                if (EditingSchedule == null)
                {
                    _notificationService.ShowWarning("Không có lịch nào để cập nhật.");
                    return;
                }
                else
                {
                    // Validate the selected room before updating
                    if (EditingSchedule.StatusSlot == "OFF")
                    {
                        var checkRoom = CheckConflictRoom(EditingSchedule, EditingSchedule.Date ?? new DateTime(), EditingSchedule.SlotTime ?? 0);

                        if (checkRoom)
                        {
                            _notificationService.ShowWarning("Phòng học này đã bị trùng lịch.");
                            return;
                        }
                    }

                    // Validate the selected lecturer before updating
                    var checkLecturer = AllSchedules.Any(schedule => schedule.LecturerId == EditingSchedule.LecturerId &&
                           schedule.Date == EditingSchedule.Date &&
                           schedule.SlotTime == EditingSchedule.SlotTime &&
                           schedule.ScheduleId != EditingSchedule.ScheduleId);
                    if (checkLecturer)
                    {
                        _notificationService.ShowWarning("Giảng viên này đã bị trùng lịch.");
                        return;
                    }
                }
                bool result = await _implementScheduleServices.UpdateScheduleAsync(EditingSchedule);

                if (result)
                {
                    var existing = AllSchedules.FirstOrDefault(s => s.ScheduleId == EditingSchedule.ScheduleId);
                    if (existing != null)
                    {
                        int index = AllSchedules.IndexOf(existing);
                        if (index >= 0)
                        {
                            AllSchedules[index] = EditingSchedule; // Gán lại để UI nhận biết
                        }
                    }

                    FilterSchedules(); // Refresh the filtered schedules
                    _notificationService.ShowSuccess("Cập nhật lịch thành công.");
                }
                else throw new Exception("Cập nhật lịch không thành công. Vui lòng thử lại sau.");

                IsEditScheduleFormOpen = false;
            }
            catch (Exception ex)
            {
                _notificationService.ShowError("Có lỗi trong quá trình cập nhật lịch học. Vui lòng thử lại.");
            }
        }

        /// <summary>
        /// Cancels the edit operation and closes the edit schedule form.
        /// </summary>
        private void CancelEditSchedule()
        {
            IsEditScheduleFormOpen = false; // Close the edit form
            EditingSchedule = null; // Clear the editing schedule
        }

        /// <summary>
        /// Fetches all rooms from the service and populates the ListRooms collection.
        /// </summary>
        /// <returns></returns>
        private async Task GetAllRooms()
        {
            try
            {
                ListRooms = new ObservableCollection<Room>(await _roomService.GetAllAsync());
            }
            catch (Exception ex)
            {
                _notificationService.ShowError("Lỗi khi tải danh sách phòng."); // Show error notification
            }
        }

        /// <summary>
        /// Fetches all lecturers by subject code from the service and populates the LecturerSubjects collection.
        /// </summary>
        /// <param name="SubjectCode"></param>
        /// <returns></returns>
        private async Task GetAllLecturerBySubjectCode(string SubjectCode)
        {
            try
            {
                LecturerSubjects = new ObservableCollection<LecturerSubject>(await _lecturerSubjectServices.GetBySubjectCodeAsync(SubjectCode));
            }
            catch (Exception ex)
            {
                _notificationService.ShowError("Lỗi khi tải danh sách giảng viên."); // Show error notification
            }
        }
        #endregion
    }
}
