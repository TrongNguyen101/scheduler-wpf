
using Microsoft.Win32;
using SchedulerWpfApp.Algorithm;
using SchedulerWpfApp.Helper;
using SchedulerWpfApp.Model;
using Syncfusion.XlsIO;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace SchedulerWpfApp.ViewModel
{
    public class CreateScheduleViewModel : ViewBaseModel
    {
        private readonly CreateScheduleTree _createScheduleTree;

        public ICommand CreateScheduleCommand { get; }


        public CreateScheduleViewModel(CreateScheduleTree createScheduleTree)
        {
            _createScheduleTree = createScheduleTree;

            CreateScheduleCommand = new RelayCommand(async () => await CreateScheduleDemo());

        }

        private async Task CreateScheduleDemo()
        {           
            DateTime startDate = new DateTime(2025, 04, 14);
            var schedules = await _createScheduleTree.GenerateSchedules(startDate);

            PrintTimetableGroupByWeek(schedules);

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
    }
}
