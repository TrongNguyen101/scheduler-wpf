using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Microsoft.Data.Sqlite;
using Microsoft.Win32;
using SchedulerWpfApp.Helper;
using SchedulerWpfApp.Model;
using SchedulerWpfApp.Services;
using SchedulerWpfApp.Views;
using Syncfusion.XlsIO;

namespace SchedulerWpfApp.ViewModel
{
    public class MainViewModel : ViewBaseModel
    {
        #region Fields
        private readonly IPersonService _personService;
        private ObservableCollection<Person> _people;
        private Person? _selectedPerson;
        private Person _newPerson;
        private bool _isAddPersonFormVisible;
        #endregion

        #region Cotrructor
        public MainViewModel(IPersonService personService)
        {
            _personService = personService;
            LoadPeopleCommand = new RelayCommand(async () => await LoadPeopleAsync());
            AddPersonCommand = new RelayCommand(async () => await AddPersonAsync(), CanAddPerson);
            UpdatePersonCommand = new RelayCommand(async () => await UpdatePersonAsync(), () => SelectedPerson != null);
            DeletePersonCommand = new RelayCommand(async () => await DeletePersonAsync(), () => SelectedPerson != null);
            ImportExcelCommand = new RelayCommand(async () => await ImportExcelAsync());
            ShowAddPersonFormCommand = new RelayCommand(ShowAddPersonForm);
            NewPerson = new Person();
            LoadPeopleAsync().ConfigureAwait(false);
        }
        #endregion

        #region Setters and Getters Properties
        /// <summary>
        /// Gets or sets the collection of people displayed in the UI.
        /// This observable collection automatically notifies the UI of changes.
        /// </summary>
        public ObservableCollection<Person> People
        {
            get => _people;
            set => SetProperty(ref _people, value);
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

        /// <summary>
        /// Gets or sets the new person being created.
        /// Used for data binding in the add person form.
        /// </summary>
        public Person NewPerson
        {
            get => _newPerson;
            set => SetProperty(ref _newPerson, value);
        }

        /// <summary>
        /// Gets or sets whether the add person form is visible.
        /// Controls the visibility of the add person UI elements.
        /// </summary>
        public bool IsAddPersonFormVisible
        {
            get => _isAddPersonFormVisible;
            set => SetProperty(ref _isAddPersonFormVisible, value);
        }
        #endregion

        #region Commands
        /// <summary>
        /// Command that triggers loading people from the data service.
        /// Bound to UI elements that refresh the people list.
        /// </summary>
        public ICommand LoadPeopleCommand { get; }

        /// <summary>
        /// Command that adds a new person to the data service.
        /// Only enabled when the new person has valid first and last names.
        /// </summary>
        public ICommand AddPersonCommand { get; }

        /// <summary>
        /// Command that updates the currently selected person.
        /// Only enabled when a person is selected in the UI.
        /// </summary>
        public ICommand UpdatePersonCommand { get; }

        /// <summary>
        /// Command that deletes the currently selected person.
        /// Only enabled when a person is selected in the UI.
        /// </summary>
        public ICommand DeletePersonCommand { get; }

        /// <summary>
        /// Command that displays the add person form dialog.
        /// Creates a new Person instance for data binding.
        /// </summary>
        public ICommand ShowAddPersonFormCommand { get; }

        public ICommand ImportExcelCommand { get; }

        #endregion

        #region Methods
        private async Task ImportExcelAsync()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx"
            };
            if (dialog.ShowDialog() == true)
            {
                var data = ReadPersonsFromExcel(dialog.FileName);
                await _personService.ImportPersonFromExcel(data);
                await LoadPeopleAsync();
            }
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

        /// <summary>
        /// Loads people from the data service and populates the People collection.
        /// Clears any selected person to avoid reference issues.
        /// </summary>
        private async Task LoadPeopleAsync()
        {
            SelectedPerson = null;
            var peopleList = await _personService.GetAllAsync();
            People = new ObservableCollection<Person>(peopleList);
        }

        /// <summary>
        /// Adds the new person to the data source, refreshes the list,
        /// and resets the NewPerson property to prepare for another entry.
        /// Updates the command state to reflect new validation status.
        /// </summary>
        private async Task AddPersonAsync()
        {
            await _personService.AddPerson(NewPerson);
            await LoadPeopleAsync();
            NewPerson = new Person();
            (AddPersonCommand as RelayCommand)?.RaiseCanExecuteChanged();
            var currentWindow = Application.Current.Windows.OfType<AddPersonWindow>().FirstOrDefault();
            if(currentWindow != null)
            {
                currentWindow.Close();
            }
        }

        /// <summary>
        /// Updates the currently selected person in the data source
        /// and refreshes the list to show the latest data.
        /// Only processes the update if a person is selected.
        /// </summary>
        private async Task UpdatePersonAsync()
        {
            if (SelectedPerson != null)
            {
                await _personService.UpdatePerson(SelectedPerson);
                await LoadPeopleAsync();
            }
        }

        /// <summary>
        /// Deletes the currently selected person from the data source
        /// and refreshes the list to reflect the removal.
        /// Only processes the deletion if a person is selected.
        /// </summary>
        private async Task DeletePersonAsync()
        {
            if (SelectedPerson != null)
            {
                await _personService.DeletePerson(SelectedPerson.Id);
                await LoadPeopleAsync();
            }
        }

        /// <summary>
        /// Determines whether a person can be added based on the
        /// validation that first name and last name are not empty.
        /// This method controls the enabled state of the AddPerson command.
        /// </summary>
        /// <returns>True if the person has valid first and last names; otherwise, false.</returns>
        private bool CanAddPerson()
        {
            return !string.IsNullOrWhiteSpace(NewPerson?.FirstName) &&
                   !string.IsNullOrWhiteSpace(NewPerson?.LastName);
        }

        /// <summary>
        /// Creates a new person instance and displays the add person dialog.
        /// Sets the data context of the dialog to this view model to enable data binding.
        /// </summary>
        private void ShowAddPersonForm()
        {
            NewPerson = new Person();
            var addPersonWindow = new AddPersonWindow
            {
                DataContext = this
            };
            addPersonWindow.ShowDialog();
        }
        #endregion
    }
}
