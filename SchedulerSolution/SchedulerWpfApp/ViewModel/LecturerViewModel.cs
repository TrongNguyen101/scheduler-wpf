using System.Collections.ObjectModel;
using System.Windows.Input;
using SchedulerWpfApp.Helper;
using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.ViewModel
{
    public class LecturerViewModel : ViewBaseModel
    {
        private string _searchKeyword;
        public string SearchKeyword
        {
            get => _searchKeyword;
            set
            {
                if (_searchKeyword != value)
                {
                    _searchKeyword = value;
                    OnPropertyChanged(); // notify binding
                    FilterStudents();    // trigger filtering
                }
            }
        }

        private ObservableCollection<Person> _allPersons; // full data
        public ObservableCollection<Person> FilteredPersons { get; set; } = new();


        // Observable collection to hold list of teachers
        public ObservableCollection<string> Teachers { get; set; }

        public ICommand ExportStudentCommand { get; }
        public ICommand ImportStudentCommand { get; }
        public ICommand AddStudentCommand { get; }
        public ICommand EditStudentCommand { get; }


        public LecturerViewModel()
        {
            Teachers = new ObservableCollection<string> { "Teacher A", "Teacher B", "Teacher C" };
            ExportStudentCommand = new RelayCommand(async () => await ExportStudentAsync());
            ImportStudentCommand = new RelayCommand(async () => await ImportStudentAsync());
            AddStudentCommand = new RelayCommand(async () => await AddStudentAsync());
            EditStudentCommand = new RelayCommand(async () => await EditStudentAsync());
        
        }

        private async Task ExportStudentAsync()
        {
            // TODO: Xử lý import từ file Excel hoặc nguồn dữ liệu khác
        }
        private async Task ImportStudentAsync()
        {
            // TODO: Show file dialog, read file, import data
        }
        private async Task AddStudentAsync()
        {
            // TODO: Show file dialog, read file, import data

        }
        private async Task EditStudentAsync()
        {
            // TODO: Show file dialog, read file, import data

        }

        private void FilterStudents()
        {
            FilteredPersons.Clear();

            var keyword = SearchKeyword?.Trim().ToLower() ?? "";

            var result = _allPersons
                .Where(s => s.FirstName.ToLower().Contains(keyword)); // or other fields

            foreach (var student in result)
            {
                FilteredPersons.Add(student);
            }
        }
    }
}
