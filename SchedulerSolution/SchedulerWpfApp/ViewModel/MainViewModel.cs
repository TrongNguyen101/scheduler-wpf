using System.Collections.ObjectModel;
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
            _people = new ObservableCollection<Person>();
            _newPerson = new Person();
        }
        #endregion
    }
}
