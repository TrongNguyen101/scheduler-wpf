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
        private bool _isPersonFormOpen;
        private bool _isOpenDialog;
        private bool _isConfirmationOpen;


        /// <summary>
        /// Gets or sets the collection of people displayed in the UI.
        /// This observable collection automatically notifies the UI of changes.
        /// </summary>
        public ObservableCollection<Person> Persons
        {
            get => _person;
            set => SetProperty(ref _person, value);
        }
        public Person SelectedPerson
        {
            get => _selectedPerson;
            set => SetProperty(ref _selectedPerson, value);
        }
        public bool IsPersonFormOpen
        {
            get => _isPersonFormOpen;
            set => SetProperty(ref _isPersonFormOpen, value);
        }
        public bool IsOpenDialog
        {
            get => _isOpenDialog;
            set => SetProperty(ref _isOpenDialog, value);
        }
        public bool IsConfirmationOpen
        {
            get => _isConfirmationOpen;
            set => SetProperty(ref _isConfirmationOpen, value);
        }

        // Commands exposed to the View
        public ICommand LoadPeopleCommand { get; }
        public ICommand ExportPersonCommand { get; }
        public ICommand ImportPersonCommand { get; }
        public ICommand AddPersonCommand { get; }
        public ICommand EditPersonCommand { get; }
        public ICommand DeletePersonCommand { get; }
        public ICommand SavePersonCommand { get; }
        public ICommand CancelEditPersonCommand { get; }
        public ICommand ConfirmDeleteCommand { get; }
        public ICommand CancelDeletePersonCommand { get; }

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
            EditPersonCommand = new RelayCommandGeneric<Person>(async (person) => await EditPersonAsync(person), (person) => person != null);

            // Generic command with parameter (used for deletion)
            DeletePersonCommand = new RelayCommandGeneric<Person>(async (person) => await DeletePersonAsync(person), (person) => person != null);

            SavePersonCommand = new RelayCommand(async () => await SavePersonAsync());
            CancelEditPersonCommand = new RelayCommand(CancelEdit);
            ConfirmDeleteCommand = new RelayCommand(async () => await ConfirmDeleteAsync());
            CancelDeletePersonCommand = new RelayCommand(CancelDelete);

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
            SelectedPerson = new Person(); // Khởi tạo object trống cho form
            IsPersonFormOpen = true;
        }
        private async Task EditPersonAsync(Person person)
        {
            if (person == null) return;

            SelectedPerson = new Person
            {
                Id = person.Id,
                FirstName = person.FirstName,
                LastName = person.LastName,
                Email = person.Email,
                Phone = person.Phone
            };

            IsPersonFormOpen = true;
        }

        /// <summary>
        /// Deletes person after confirmation.
        /// </summary>
        private async Task DeletePersonAsync(Person person)

        {

            IsOpenDialog = true;

        }


        public async Task SavePersonAsync()
        {

        }
        public async Task ConfirmDeleteAsync()
        {

        }
        public void CancelEdit()
        {
            IsPersonFormOpen = false;
            SelectedPerson = null;

        }
        public void CancelDelete()
        {
            IsOpenDialog = false;

        }
    }
}
