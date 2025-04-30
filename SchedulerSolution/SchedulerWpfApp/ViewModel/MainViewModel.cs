using System.Collections.ObjectModel;
using System.Windows.Input;
using SchedulerWpfApp.Helper;
using SchedulerWpfApp.Model;
using SchedulerWpfApp.Services;

namespace SchedulerWpfApp.ViewModel
{
    public class MainViewModel : ViewBaseModel
    {
        #region Fields
        private readonly IPersonService _personService;
        private ObservableCollection<Person> _people;
        private Person? _selectedPerson;
        private Person _newPerson;
        #endregion

        #region Cotrructor
        public MainViewModel(IPersonService personService)
        {
            _personService = personService;
            LoadPeopleCommand = new RelayCommand(async () => await LoadPeopleAsync());
            AddPersonCommand = new RelayCommand(async () => await AddPersonAsync(), CanAddPerson);
            UpdatePersonCommand = new RelayCommand(async () => await UpdatePersonAsync(), () => SelectedPerson != null);
            DeletePersonCommand = new RelayCommand(async () => await DeletePersonAsync(), () => SelectedPerson != null);
            NewPerson = new Person();
            LoadPeopleAsync().ConfigureAwait(false);
        }
        public ObservableCollection<Person> People
        {
            get => _people;
            set => SetProperty(ref _people, value);
        }

        public Person SelectedPerson
        {
            get => _selectedPerson;
            set => SetProperty(ref _selectedPerson, value);
        }

        public Person NewPerson
        {
            get => _newPerson;
            set => SetProperty(ref _newPerson, value);
        }
        #endregion

        #region Commands
        public ICommand LoadPeopleCommand { get; }
        public ICommand AddPersonCommand { get; }
        public ICommand UpdatePersonCommand { get; }
        public ICommand DeletePersonCommand { get; }
        #endregion

        private async Task LoadPeopleAsync()
        {
            SelectedPerson = null;
            var peopleList = await _personService.GetAllAsync();
            People = new ObservableCollection<Person>(peopleList);
        }

        /// <summary>
        /// Adds the new person to the data source, refreshes the list,
        /// and resets the NewPerson property.
        /// </summary>
        private async Task AddPersonAsync()
        {
            await _personService.AddPerson(NewPerson);
            await LoadPeopleAsync();
            NewPerson = new Person();
            (AddPersonCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        /// <summary>
        /// Updates the currently selected person in the data source
        /// and refreshes the list.
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
        /// and refreshes the list.
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
        /// </summary>
        /// <returns>True if the person has valid first and last names; otherwise, false.</returns>
        private bool CanAddPerson()
        {
            return !string.IsNullOrWhiteSpace(NewPerson?.FirstName) &&
                   !string.IsNullOrWhiteSpace(NewPerson?.LastName);
        }
    }
}
