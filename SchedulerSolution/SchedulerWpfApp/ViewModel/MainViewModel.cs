using System.Windows.Input;
using SchedulerWpfApp.Helper;
using SchedulerWpfApp.Model;
using Syncfusion.XlsIO;

namespace SchedulerWpfApp.ViewModel
{
    public class MainViewModel : ViewBaseModel
    {
        private object _currentViewModel;
        public object CurrentViewModel
        {
            get => _currentViewModel;
            set { _currentViewModel = value; OnPropertyChanged(); }
        }

        public ICommand ShowCourseCommand { get; }
        public ICommand ShowTeacherCommand { get; }
        public ICommand ShowRoomCommand { get; }

        public MainViewModel()
        {
            ShowCourseCommand = new RelayCommand(ShowCourse);
            ShowTeacherCommand = new RelayCommand(ShowTeacher);
            ShowRoomCommand = new RelayCommand(ShowRoom);

            // Mặc định load "Quản lý môn"
            CurrentViewModel = new CourseViewModel();
        }

        private void ShowCourse()
        {
            CurrentViewModel = new CourseViewModel();
        }

        private void ShowTeacher()
        {
            CurrentViewModel = new LecturerViewModel();
        }

        private void ShowRoom()
        {
            CurrentViewModel = new RoomViewModel();
        }

        private List<Person> ReadPersonsFromExcel(string filePath)
        {
            var persons = new List<Person>();

            using ExcelEngine excelEngine = new();
            IApplication application = excelEngine.Excel;
            application.DefaultVersion = ExcelVersion.Xlsx;

            IWorkbook workbook = application.Workbooks.Open(filePath);
            IWorksheet sheet = workbook.Worksheets[0];

            int rowCount = sheet.UsedRange.LastRow;
            int colCount = sheet.UsedRange.LastColumn;

            // Đọc header
            Dictionary<string, int> headerMap = new();
            for (int c = 1; c <= colCount; c++)
            {
                string header = sheet[1, c].Value.Trim();
                headerMap[header] = c;
            }

            // Đọc từng dòng dữ liệu
            for (int r = 2; r <= rowCount; r++)
            {
                var person = new Person
                {
                    FirstName = sheet[r, headerMap["FirstName"]].Value,
                    LastName = sheet[r, headerMap["LastName"]].Value,
                    Email = sheet[r, headerMap["Email"]].Value,
                    Phone = sheet[r, headerMap["Phone"]].Value
                };

                persons.Add(person);
            }

            return persons;
        }
    }
}
