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
            Func<RoomViewModel> roomViewModelFactory,
            CreateScheduleTree createScheduleTree)
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
            DateTime startDate = new DateTime(2025, 04, 14);
            List<Schedule> schedules = createScheduleTree.GenerateSchedules(startDate);

            var dialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = "ScheduleDemo.xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // Export only non-null list
                    ExportSchedulesToExcel(schedules, dialog.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Export failed: {ex}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

        }



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
                Debug.WriteLine($"Room: {classGroup.First().RoomNo} | Session: {classGroup.First().PartOfDay}");
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
                    var dates = weekGroup.Select(s => s.Date)
                        .Distinct()
                        .OrderBy(d => d)
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
                        string[] rowLines = new string[5];
                        rowLines[0] = slot.PadRight(10) + "| "; // Dòng đầu tiên bắt đầu bằng slot
                        rowLines[1] = "".PadRight(10) + "| ";   // Dòng thứ hai và ba để trống ở cột slot
                        rowLines[2] = "".PadRight(10) + "| ";
                        rowLines[3] = "".PadRight(10) + "| ";
                        rowLines[4] = "".PadRight(10) + "| ";

                        foreach (var date in dates)
                        {
                            var schedule = weekGroup.FirstOrDefault(s => s.Date == date && s.SlotTime == slot);
                            if (schedule != null)
                            {
                                if (schedule.SubjectCode == "No subject")
                                {
                                    rowLines[0] += "".PadRight(columnWidth) + "| ";
                                    rowLines[1] += "".PadRight(columnWidth) + "| ";
                                    rowLines[2] += "".PadRight(columnWidth) + "| ";
                                    rowLines[3] += "".PadRight(columnWidth) + "| ";
                                    rowLines[4] += "".PadRight(columnWidth) + "| ";
                                }
                                else
                                {
                                    rowLines[0] += $"Subject: {schedule.SubjectCode}".PadRight(columnWidth) + "| ";
                                    rowLines[1] += $"Lecturer: {schedule.LecturerId}".PadRight(columnWidth) + "| ";
                                    rowLines[2] += $"Slot type: {schedule.StatusSlot}".PadRight(columnWidth) + "| ";
                                    rowLines[3] += $"Session: {schedule.PartOfDay}".PadRight(columnWidth) + "| ";
                                    rowLines[4] += $"Room: {schedule.RoomNo}".PadRight(columnWidth) + "| ";

                                }
                            }
                            else
                            {
                                rowLines[0] += "".PadRight(columnWidth) + "| ";
                                rowLines[1] += "".PadRight(columnWidth) + "| ";
                                rowLines[2] += "".PadRight(columnWidth) + "| ";
                            }
                        }

                        // In 3 dòng của hàng
                        Debug.WriteLine(rowLines[0]);
                        Debug.WriteLine(rowLines[1]);
                        Debug.WriteLine(rowLines[2]);
                        Debug.WriteLine(new string('-', header.Length));
                    }

                    Debug.WriteLine(new string('=', header.Length));
                    Debug.WriteLine(""); // Thêm khoảng cách giữa các tuần
                }
                Debug.WriteLine("\n"); // Thêm khoảng cách giữa các lớp
            }
        }
        public void ExportSchedulesToExcel(List<Schedule> schedules, string filePath)
        {
            using ExcelEngine excelEngine = new();
            IApplication application = excelEngine.Excel;
            application.DefaultVersion = ExcelVersion.Xlsx;

            IWorkbook workbook = application.Workbooks.Create(1);
            IWorksheet sheet = workbook.Worksheets[0];

            // Header row
            string[] headers = new string[]
            {
        "ScheduleId", "RoomNo", "PartOfDay", "SlotTime", "StatusSlot",
        "Date", "Major", "SubjectCode", "GroupName", "LecturerId",
        "SlotTypeCode", "TypeSlot", "SessionNo"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                sheet[1, i + 1].Text = headers[i];
            }

            // Data rows
            int row = 2;
            foreach (var s in schedules)
            {
                sheet[row, 1].Number = s.ScheduleId;
                sheet[row, 2].Text = s.RoomNo ?? "";
                sheet[row, 3].Text = s.PartOfDay ?? "";
                sheet[row, 4].Text = s.SlotTime ?? "";
                sheet[row, 5].Text = s.StatusSlot ?? "";
                sheet[row, 6].Text = s.Date?.ToString("yyyy-MM-dd") ?? "";
                sheet[row, 7].Text = s.Major ?? "";
                sheet[row, 8].Text = s.SubjectCode ?? "";
                sheet[row, 9].Text = s.GroupName ?? "";
                sheet[row, 10].Text = s.LecturerId ?? "";
                sheet[row, 11].Text = s.SlotTypeCode ?? "";
                sheet[row, 12].Text = s.TypeSlot ?? "";
                sheet[row, 13].Number = s.SessionNo;

                row++;
            }

            workbook.SaveAs(filePath);
        }


        public static DateTime GetWeekStartDate(DateTime date)
        {
            int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.AddDays(-1 * diff).Date;
        }




        private static DateTime? GetWeekStartDate(DateTime? date)
        {
            if (!date.HasValue)
                return null;

            int diff = (7 + (date.Value.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.Value.AddDays(-diff).Date;
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
