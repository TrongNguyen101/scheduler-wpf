
using SchedulerWpfApp.Algorithm;
using SchedulerWpfApp.Helper;
using SchedulerWpfApp.Model;
using SchedulerWpfApp.Services.ScheduleServices;
using Syncfusion.XlsIO;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Windows.Input;

namespace SchedulerWpfApp.ViewModel
{
    public class CreateScheduleViewModel : ViewBaseModel
    {
        private readonly CreateScheduleTree _createScheduleTree;
        private readonly InterfaceScheduleServices _implementScheduleServices;

        public ObservableCollection<DateTime> WeekDays { get; set; } = new();
        public ObservableCollection<int> Slots { get; set; } = new() { 1, 2, 3, 4 };
        public ObservableCollection<int> Years { get; set; } = new(Enumerable.Range(2020, 10));
        public ObservableCollection<string> Weeks { get; set; } = new();
        public ObservableCollection<string> GroupNames { get; set; } = new();
        public ObservableCollection<SlotRowViewModel> SlotRows { get; set; } = new();
        public ObservableCollection<TimetableCellViewModel> TimetableCells { get; set; } = new();
        private ObservableCollection<Schedule> AllSchedules { get; set; } = new();

        private int _selectedYear;
        public int SelectedYear
        {
            get => _selectedYear;
            set { _selectedYear = value; OnPropertyChanged(); GenerateWeeks(); }
        }

        private string _selectedWeek;
        public string SelectedWeek
        {
            get => _selectedWeek;
            set { _selectedWeek = value; OnPropertyChanged(); FilterSchedules(); }
        }

        private string _selectedGroupName;
        public string SelectedGroupName
        {
            get => _selectedGroupName;
            set { _selectedGroupName = value; OnPropertyChanged(); FilterSchedules(); }
        }

        public ICommand CreateScheduleCommand { get; }

        public CreateScheduleViewModel(CreateScheduleTree createScheduleTree, InterfaceScheduleServices implementScheduleServices)
        {
            _createScheduleTree = createScheduleTree;
            _implementScheduleServices = implementScheduleServices;

            SelectedYear = DateTime.Now.Year;
            CreateScheduleCommand = new RelayCommand(async () => await CreateScheduleDemo());

            LoadMockSchedules();
            InitCurrentWeekDays();
            FilterSchedules();
        }

        private void InitCurrentWeekDays()
        {
            WeekDays.Clear();
            DateTime today = DateTime.Today;
            int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
            DateTime monday = today.AddDays(-diff);

            for (int i = 0; i < 7; i++)
            {
                WeekDays.Add(monday.AddDays(i));
            }
        }

        private void GenerateWeeks()
        {
            Weeks.Clear();
            for (int i = 1; i <= 52; i++)
            {
                DateTime start = FirstDateOfWeek(SelectedYear, i);
                DateTime end = start.AddDays(6);
                Weeks.Add($"{start:dd/MM} - {end:dd/MM}");
            }
        }

        private DateTime FirstDateOfWeek(int year, int weekOfYear)
        {
            DateTime jan1 = new(year, 1, 1);
            int daysOffset = DayOfWeek.Monday - jan1.DayOfWeek;
            DateTime firstMonday = jan1.AddDays(daysOffset);
            return firstMonday.AddDays((weekOfYear - 1) * 7);
        }

        private void FilterSchedules()
        {
            TimetableCells.Clear();
            SlotRows.Clear();

            if (string.IsNullOrEmpty(SelectedGroupName) || string.IsNullOrEmpty(SelectedWeek))
            {
                GenerateTimetableCellsAndSlotRows(new List<Schedule>());
                return;
            }

            var (start, end) = ParseSelectedWeekToDates();

            WeekDays.Clear();
            for (int i = 0; i < 7; i++) WeekDays.Add(start.AddDays(i));

            var filtered = AllSchedules.Where(s => s.GroupName == SelectedGroupName && s.Date >= start && s.Date <= end).ToList();
            GenerateTimetableCellsAndSlotRows(filtered);
        }

        private (DateTime start, DateTime end) ParseSelectedWeekToDates()
        {
            var parts = SelectedWeek.Split(" - ");
            DateTime start = DateTime.ParseExact(parts[0], "dd/MM", CultureInfo.InvariantCulture).AddYears(SelectedYear - DateTime.Now.Year);
            DateTime end = DateTime.ParseExact(parts[1], "dd/MM", CultureInfo.InvariantCulture).AddYears(SelectedYear - DateTime.Now.Year);
            return (start, end);
        }

        private async void LoadMockSchedules()
        {
            var schedules = await _implementScheduleServices.GetAllAsync();
            AllSchedules = new ObservableCollection<Schedule>(schedules);
            SetupDataSchedule(schedules);
        }

        private void SetupDataSchedule(List<Schedule> schedules)
        {
            if (schedules?.Any() == true)
            {
                GroupNames = new ObservableCollection<string>(schedules.Select(s => s.GroupName).Distinct().OrderBy(name => name));
            }
        }

        private async Task CreateScheduleDemo()
        {
            DateTime startDate = new DateTime(2025, 04, 14);
            var schedules = await _createScheduleTree.GenerateSchedules(startDate);

            PrintTimetableGroupByWeek(schedules);

            //var dialog = new SaveFileDialog
            //{
            //    Filter = "Excel Files (*.xlsx)|*.xlsx",
            //    FileName = "ScheduleDemo.xlsx"
            //};

            //if (dialog.ShowDialog() == true)
            //{
            //    try
            //    {
            //        // Export only non-null list
            //        ExportSchedulesToExcel(schedules, dialog.FileName);
            //    }
            //    catch (Exception ex)
            //    {
            //        MessageBox.Show($"Export failed: {ex}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            //    }
            //}
        }

        private void ExportSchedulesToExcel(List<Schedule> schedules, string filePath)
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
                        string[] rowLines = new string[7];
                        rowLines[0] = slot.PadRight(10) + "| "; // Dòng đầu tiên bắt đầu bằng slot
                        rowLines[1] = "".PadRight(10) + "| ";   // Dòng thứ hai và ba để trống ở cột slot
                        rowLines[2] = "".PadRight(10) + "| ";
                        rowLines[3] = "".PadRight(10) + "| ";
                        rowLines[4] = "".PadRight(10) + "| ";
                        rowLines[5] = "".PadRight(10) + "| ";
                        rowLines[6] = "".PadRight(10) + "| ";

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
                                }
                                else
                                {
                                    rowLines[0] += $"Subject: {schedule.SubjectCode}".PadRight(columnWidth) + "| ";
                                    rowLines[1] += $"Lecturer: {schedule.LecturerId}".PadRight(columnWidth) + "| ";
                                    rowLines[2] += $"Slot type: {schedule.StatusSlot}".PadRight(columnWidth) + "| ";
                                    rowLines[3] += $"Session: {schedule.PartOfDay}".PadRight(columnWidth) + "| ";
                                    rowLines[4] += $"Room: {schedule.RoomNo}".PadRight(columnWidth) + "| ";
                                    rowLines[5] += $"Slot code: {schedule.SlotTypeCode}".PadRight(columnWidth) + "| ";
                                    rowLines[6] += $"Session No: {schedule.SessionNo}".PadRight(columnWidth) + "| ";

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
                        Debug.WriteLine(rowLines[3]);
                        Debug.WriteLine(rowLines[4]);
                        Debug.WriteLine(rowLines[5]);
                        Debug.WriteLine(rowLines[6]);



                        Debug.WriteLine(new string('-', header.Length));
                    }

                    Debug.WriteLine(new string('=', header.Length));
                    Debug.WriteLine(""); // Thêm khoảng cách giữa các tuần
                }
                Debug.WriteLine("\n"); // Thêm khoảng cách giữa các lớp
            }
        }

        private static DateTime? GetWeekStartDate(DateTime? date)
        {
            if (!date.HasValue)
                return null;

            int diff = (7 + (date.Value.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.Value.AddDays(-diff).Date;
        }

        private void GenerateTimetableCellsAndSlotRows(List<Schedule> filtered)
        {
            TimetableCells.Clear();
            SlotRows.Clear();

            foreach (var slot in Slots)
            {
                string slotStr = $"slot {slot}";
                var cells = new ObservableCollection<TimetableCellViewModel>();

                foreach (var day in WeekDays)
                {
                    var match = filtered.FirstOrDefault(s => s.Date?.Date == day.Date && s.SlotTime == slotStr);

                    cells.Add(new TimetableCellViewModel
                    {
                        DayOfWeek = day,
                        SlotNumber = slotStr,
                        Schedule = match
                    });

                    TimetableCells.Add(new TimetableCellViewModel
                    {
                        DayOfWeek = day,
                        SlotNumber = slotStr,
                        Schedule = match
                    });
                }

                SlotRows.Add(new SlotRowViewModel
                {
                    SlotNumber = slot,
                    Cells = cells
                });
            }
        }

        public static string GetSlotTimeRange(int slotNumber)
        {
            DateTime startTime = new DateTime(1, 1, 1, 7, 0, 0); // 7:00 AM
            TimeSpan duration = TimeSpan.FromMinutes(135);
            DateTime slotStart = startTime.AddMinutes((slotNumber - 1) * duration.TotalMinutes);
            DateTime slotEnd = slotStart.Add(duration);

            return $"{slotStart:HH\\:mm} - {slotEnd:HH\\:mm}";
        }

    }

    public class TimetableCellViewModel
    {
        public DateTime DayOfWeek { get; set; }
        public string SlotNumber { get; set; }
        public Schedule? Schedule { get; set; }
    }

    public class SlotRowViewModel
    {
        public int SlotNumber { get; set; }
        public ObservableCollection<TimetableCellViewModel> Cells { get; set; } = new();

        public string SlotTimeRange
        {
            get
            {
                return CreateScheduleViewModel.GetSlotTimeRange(SlotNumber);
            }
        }
    }
}
