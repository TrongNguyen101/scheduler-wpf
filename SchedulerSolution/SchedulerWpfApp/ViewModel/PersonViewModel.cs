using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using SchedulerWpfApp.Helper;
using SchedulerWpfApp.Model;
using SchedulerWpfApp.Services;

namespace SchedulerWpfApp.ViewModel
{   /// <summary>
    /// ViewModel responsible for managing persons: loading, importing, exporting, and deleting.
    /// </summary>
    public class PersonViewModel : ViewBaseModel
    {
        // Dependencies injected via constructor
        private readonly IPersonService _personService;
        private readonly IExcelPersonImporter _excelImporter;
        private readonly IExcelPersonExporter _excelExporter;

        // Internal data fields
        private ObservableCollection<Person> _person;
        private Person? _selectedPerson;
        private string _searchKeyword;

        /// <summary>
        /// Gets or sets the collection of people displayed in the UI.
        /// This observable collection automatically notifies the UI of changes.
        /// </summary>
        public ObservableCollection<Person> Persons
        {
            get => _person;
            set => SetProperty(ref _person, value);
        }

        // Commands exposed to the View
        public ICommand LoadPeopleCommand { get; }
        public ICommand ExportPersonCommand { get; }
        public ICommand ImportPersonCommand { get; }
        public ICommand AddPersonCommand { get; }
        public ICommand EditPersonCommand { get; }
        public ICommand DeletePersonCommand { get; }

        /// <summary>
        /// Constructor initializes dependencies and commands.
        /// </summary>
        public PersonViewModel(IPersonService personService, IExcelPersonImporter excelImporter, IExcelPersonExporter excelExporter)
        {
            _personService = personService;
            _excelImporter = excelImporter;
            _excelExporter = excelExporter;

            Persons = new ObservableCollection<Person>();

            // Initialize commands with async methods
            LoadPeopleCommand = new RelayCommand(async () => await LoadPeopleAsync());
            ImportPersonCommand = new RelayCommand(async () => await ImportPersonAsync());
            ExportPersonCommand = new RelayCommand(async () => await ExportPersonAsync());
            AddPersonCommand = new RelayCommand(async () => await AddPersonAsync());
            EditPersonCommand = new RelayCommand(async () => await EditPersonAsync());

            // Generic command with parameter (used for deletion)
            DeletePersonCommand = new RelayCommandGeneric<Person>(async (person) => await DeletePersonAsync(person), (person) => person != null);

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
                Persons = new ObservableCollection<Person>(peopleList);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load persons: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Exports the current list of persons to an Excel file.
        /// </summary>
        private async Task ExportPersonAsync()
        {
            if (Persons == null || Persons.Count == 0)
            {
                MessageBox.Show("No persons to export.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = "Persons.xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // Export only non-null list
                    var personList = Persons.Where(p => p != null).ToList();
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
        /// Imports persons from an Excel file and adds them to the data source.
        /// </summary>
        private async Task ImportPersonAsync()
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

        private async Task AddPersonAsync()
        {
            // TODO: Show file dialog, read file, import data

        }
        private async Task EditPersonAsync()
        {
            // TODO: Show file dialog, read file, import data

        }

        /// <summary>
        /// Deletes person after confirmation.
        /// </summary>
        private async Task DeletePersonAsync(Person person)
        {
            if (person == null)
            {
                MessageBox.Show("No person selected to delete.");
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
