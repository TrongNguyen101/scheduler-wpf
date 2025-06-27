using Microsoft.Win32;
using SchedulerWpfApp.Algorithm;
using SchedulerWpfApp.Helper;
using SchedulerWpfApp.Model;
using SchedulerWpfApp.ServiceRefactor.ScheduleServices;
using Syncfusion.XlsIO;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace SchedulerWpfApp.ViewModel
{
    public class CreateScheduleViewModel : ViewBaseModel
    {
        #region Fields
        private readonly CreateScheduleTree _createScheduleTree;
        private readonly IScheduleServices _implementScheduleServices;
        private int _selectedYear;
        private string _selectedWeek;
        private string _selectedGroupName;
        private ObservableCollection<string> _groupNames;
        #endregion

        #region Constructor
        public ObservableCollection<DateTime> WeekDays { get; set; } = new();
        public ObservableCollection<int> Slots { get; set; } = new() { 1, 2, 3, 4, 5, 6, 7, 8 }; // List of available time slots in a day
        public ObservableCollection<int> Years { get; set; } = new(Enumerable.Range(DateTime.Now.Year - 2, 5)); // List of years from 2 years ago to next 2 years
        public ObservableCollection<string> Weeks { get; set; } = new(); // List of weeks in "dd/MM - dd/MM" format
        public ObservableCollection<string> GroupNames { get => _groupNames; set => SetProperty(ref _groupNames, value); }// List of group names to filter schedules
        public ObservableCollection<SlotRowViewModel> SlotRows { get; set; } = new(); // List of slot rows for the timetable
        private ObservableCollection<Schedule> AllSchedules { get; set; } = new(); // All schedules loaded from the service

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

        public ICommand CreateScheduleCommand { get; }
        public ICommand ExportExcelCommand { get; }

        public CreateScheduleViewModel(CreateScheduleTree createScheduleTree, IScheduleServices implementScheduleServices)
        {
            _createScheduleTree = createScheduleTree;
            _implementScheduleServices = implementScheduleServices;

            SelectedYear = DateTime.Now.Year; // Default to current year
            CreateScheduleCommand = new RelayCommand(async () => await CreateScheduleDemo());
            ExportExcelCommand = new RelayCommand(async () => await ExportSchedulesToExcel());

            LoadMockSchedules(); // Load initial schedules from the service
            InitCurrentWeekDays(); // Initialize current week days
            FilterSchedules(); // Filter schedules based on initial selections
        }
        #endregion

        #region Methods
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

            if (string.IsNullOrEmpty(SelectedGroupName) || string.IsNullOrEmpty(SelectedWeek))
            {
                // If no group or week is selected, create empty rows
                GenerateTimetableCellsAndSlotRows(new List<Schedule>());
                return;
            }

            var (start, end) = ParseSelectedWeekToDates();

            WeekDays.Clear();
            for (int i = 0; i < 7; i++) WeekDays.Add(start.AddDays(i)); // Add each day of the week starting from the start date

            var filtered = AllSchedules.Where(s => s.GroupName == SelectedGroupName && s.Date >= start && s.Date <= end).ToList(); // Filter schedules by group name and date range
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
            }
        }

        /// <summary>
        /// Generates a demo schedule starting from a specific date and exports it to an Excel file.
        /// </summary>
        private async Task CreateScheduleDemo()
        {
            DateTime startDate = new DateTime(2025, 01, 06);

            var schedules = await _createScheduleTree.GenerateSchedules(startDate);

            LoadMockSchedules(); // Reload schedules after generating new ones
            //PrintTimetableGroupByWeek(schedules); // Print the timetable grouped by week for debugging purposes

            if (schedules == null || !schedules.Any())
                MessageBox.Show("Không có lịch nào được tạo.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            else
                MessageBox.Show("Tạo lịch thành công.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Exports the list of schedules to an Excel file at the specified file path.
        /// </summary>
        private async Task ExportSchedulesToExcel()
        {
            var schedules = await _implementScheduleServices.GetAllAsync(); // Get the current list of schedules to export

            if (schedules == null || !schedules.Any())
            {
                MessageBox.Show("Không có lịch nào để xuất.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
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
                    MessageBox.Show($"Export thất bại: {ex}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
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

                    cells.Add(new TimetableCellViewModel
                    {
                        DayOfWeek = day,
                        SlotNumber = slot,
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
                    MessageBox.Show("Đã bị trùng lịch. Không thể duy chuyển slot này.",
                                  "Duy chuyển thất bại", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                MessageBox.Show($"Error moving schedule: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
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
                sourceCell.Schedule = targetSchedule; // This might be null (empty slot) or another schedule

                // Update the schedule in the underlying data if targetSchedule is not null
                if (targetSchedule != null)
                {
                    var updatedTargetSchedule = CreateUpdatedSchedule(targetSchedule, sourceCell.DayOfWeek, sourceCell.SlotNumber);
                    sourceCell.Schedule = updatedTargetSchedule;

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
                MessageBox.Show($"Error swapping schedule: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Validates if a schedule can be moved to the target cell
        /// </summary>
        private bool ValidateScheduleMove(TimetableCellViewModel sourceCell, TimetableCellViewModel targetCell, Schedule schedule)
        {
            // Basic validation
            if (sourceCell == targetCell)
                return false;

            // Check for lecturer conflicts (same lecturer can't be in two places at same time)
            var conflictingSchedule = AllSchedules.FirstOrDefault(s =>
                s.LecturerId == schedule.LecturerId &&
                s.Date == targetCell.DayOfWeek.Date &&
                s.SlotTime == targetCell.SlotNumber &&
                s.ScheduleId != schedule.ScheduleId);

            if (conflictingSchedule != null && targetCell.Schedule == null)
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
                SessionNo = originalSchedule.SessionNo
            };
        }
        #endregion
    }
}
