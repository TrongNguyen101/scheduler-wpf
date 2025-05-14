using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using SchedulerWpfApp.Helper;
using SchedulerWpfApp.Model;
using SchedulerWpfApp.Services;
using Syncfusion.XlsIO;

namespace SchedulerWpfApp.ViewModel
{
    public class LecturerViewModel : ViewBaseModel
    {
        private string _searchKeyword;
        private readonly IPersonService _personService;
        private ObservableCollection<Person> _lecturer;
        private Person? _selectedPerson;

        public ObservableCollection<Person> FilteredPersons { get; set; } = new();
        public string SearchKeyword
        {
            get => _searchKeyword;
            set
            {
                if (_searchKeyword != value)
                {
                    _searchKeyword = value;
                    OnPropertyChanged(); // notify binding
                    //FilterLecturers();    // trigger filtering
                }
            }
        }

        /// <summary>
        /// Gets or sets the collection of people displayed in the UI.
        /// This observable collection automatically notifies the UI of changes.
        /// </summary>
        public ObservableCollection<Person> Lecturers
        {
            get => _lecturer;
            set => SetProperty(ref _lecturer, value);
        }

        /// <summary>
        /// Gets or sets the currently selected person in the UI.
        /// Used for edit, update, and delete operations.
        /// </summary>
        public Person SelectedPerson
        {
            get => _selectedPerson;
            set => SetProperty(ref _selectedPerson, value);
        }

        public ICommand LoadPeopleCommand { get; }
        public ICommand ExportLecturerCommand { get; }
        public ICommand ImportLecturerCommand { get; }
        public ICommand AddLecturerCommand { get; }
        public ICommand EditLecturerCommand { get; }

        public LecturerViewModel(IPersonService personService)
        {
            _personService = personService;
            LoadPeopleCommand = new RelayCommand(async () => await LoadPeopleAsync());
            ExportLecturerCommand = new RelayCommand(async () => await ExportLecturerAsync());
            ImportLecturerCommand = new RelayCommand(async () => await ImportLecturerAsync());
            AddLecturerCommand = new RelayCommand(async () => await AddLecturerAsync());
            EditLecturerCommand = new RelayCommand(async () => await EditLecturerAsync());
            _ = LoadPeopleAsync();
        }

        /// <summary>
        /// Loads people from the data service and populates the People collection.
        /// Clears any selected person to avoid reference issues.
        /// </summary>
        private async Task LoadPeopleAsync()
        {
            try
            {
                SelectedPerson = null;
                var peopleList = await _personService.GetAllAsync();
                Lecturers = new ObservableCollection<Person>(peopleList);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"LoadPeopleAsync error: {ex.Message}");
            }
        }

        private async Task ExportLecturerAsync()
        {
            // TODO: Xử lý import từ file Excel hoặc nguồn dữ liệu khác
        }
        private async Task ImportLecturerAsync()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                var data = ReadPersonsFromExcel(dialog.FileName);
                await _personService.ImportPersonFromExcel(data);
                MessageBox.Show("Import completed.");
                await LoadPeopleAsync();
            }
        }
        private async Task AddLecturerAsync()
        {
            // TODO: Show file dialog, read file, import data

        }
        private async Task EditLecturerAsync()
        {
            // TODO: Show file dialog, read file, import data

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
