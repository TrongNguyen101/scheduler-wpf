using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using SchedulerWpfApp.Helper;
using SchedulerWpfApp.Model;
using SchedulerWpfApp.Services;

namespace SchedulerWpfApp.ViewModel
{   /// <summary>
    /// ViewModel responsible for managing lecturers: loading, importing, exporting, and deleting.
    /// </summary>
    public class LecturerViewModel : ViewBaseModel
    {
        // Dependencies injected via constructor
        private readonly IPersonService _personService;
        private readonly IExcelPersonImporter _excelImporter;
        private readonly IExcelPersonExporter _excelExporter;

        // Internal data fields
        private ObservableCollection<Person> _lecturer;
        private Person? _selectedPerson;
        private string _searchKeyword;

        /// <summary>
        /// Gets or sets the collection of people displayed in the UI.
        /// This observable collection automatically notifies the UI of changes.
        /// </summary>
        public ObservableCollection<Person> Lecturers
        {
            get => _lecturer;
            set => SetProperty(ref _lecturer, value);
        }

        // Commands exposed to the View
        public ICommand LoadPeopleCommand { get; }
        public ICommand ExportLecturerCommand { get; }
        public ICommand ImportLecturerCommand { get; }
        public ICommand AddLecturerCommand { get; }
        public ICommand EditLecturerCommand { get; }
        public ICommand DeleteLecturerCommand { get; }

        /// <summary>
        /// Constructor initializes dependencies and commands.
        /// </summary>
        public LecturerViewModel(IPersonService personService, IExcelPersonImporter excelImporter, IExcelPersonExporter excelExporter)
        {
            _personService = personService;
            _excelImporter = excelImporter;
            _excelExporter = excelExporter;

            Lecturers = new ObservableCollection<Person>();

            // Initialize commands with async methods
            LoadPeopleCommand = new RelayCommand(async () => await LoadPeopleAsync());
            ImportLecturerCommand = new RelayCommand(async () => await ImportLecturerAsync());
            ExportLecturerCommand = new RelayCommand(async () => await ExportLecturerAsync());
            AddLecturerCommand = new RelayCommand(async () => await AddLecturerAsync());
            EditLecturerCommand = new RelayCommand(async () => await EditLecturerAsync());

            // Generic command with parameter (used for deletion)
            DeleteLecturerCommand = new RelayCommandGeneric<Person>(async (person) => await DeleteLecturerAsync(person), (person) => person != null);

            // Load data immediately when ViewModel is constructed
            _ = LoadPeopleAsync();
        }

        /// <summary>
        /// Loads people from the data service and populates the People collection.
        /// </summary>
        private async Task LoadPeopleAsync()
        {
            try
            {
                var peopleList = await _personService.GetAllAsync();
                Lecturers = new ObservableCollection<Person>(peopleList);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load lecturers: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Exports the current list of lecturers to an Excel file.
        /// </summary>
        private async Task ExportLecturerAsync()
        {
            if (Lecturers == null || Lecturers.Count == 0)
            {
                MessageBox.Show("No lecturers to export.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = "Lecturers.xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // Export only non-null list
                    var personList = Lecturers.Where(p => p != null).ToList();
                    _excelExporter.ExportToExcel(personList, dialog.FileName);
                    MessageBox.Show("Export successful!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Imports lecturers from an Excel file and adds them to the data source.
        /// </summary>
        private async Task ImportLecturerAsync()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var data = _excelImporter.ReadPersonsFromExcel(dialog.FileName);
                    await _personService.ImportPersonFromExcel(data);
                    MessageBox.Show("Import successful!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadPeopleAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Import failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
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

        /// <summary>
        /// Deletes lecturer after confirmation.
        /// </summary>
        private async Task DeleteLecturerAsync(Person person)
        {
            if (person == null)
            {
                MessageBox.Show("No lecturer selected to delete.");
                return;
            }

            var confirm = MessageBox.Show(
                $"Are you sure you want to delete {person.FirstName} {person.LastName}?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm == MessageBoxResult.Yes)
            {
                try
                {
                    await _personService.DeletePerson(person.Id);
                    MessageBox.Show("Deleted successfully.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadPeopleAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting: {ex.Message}");
                }
            }
        }
    }
}
