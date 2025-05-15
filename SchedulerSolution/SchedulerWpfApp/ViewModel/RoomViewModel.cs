using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using SchedulerWpfApp.Helper;
using SchedulerWpfApp.Model;
using SchedulerWpfApp.Services;

namespace SchedulerWpfApp.ViewModel
{
    public class RoomViewModel : ViewBaseModel
    {

        private readonly IGroupNameService _groupnameService;
        private readonly IExcelPersonImporter _excelImporter;
        private readonly IExcelPersonExporter _excelExporter;


        private ObservableCollection<GroupName> _groupname;
        private Person? _selectedGroupname;
        private string _searchKeyword;
        private bool _isPersonFormOpen;
        private bool _isOpenDialog;
        private bool _isConfirmationOpen;
        // Observable collection to hold list of rooms
        public ObservableCollection<GroupName> GroupNames
        {
            get => _groupname;
            set => SetProperty(ref _groupname, value);
        }

        public ICommand AddRoomCommand { get; set; }
        public ICommand RemoveRoomCommand { get; set; }
        public ICommand LoadRoomCommand { get; }
        public ICommand ExportPersonCommand { get; }
        public ICommand ImportRoomCommand { get; }
        public ICommand DeletePersonCommand { get; }
        public ICommand SavePersonCommand { get; }
        public ICommand CancelEditPersonCommand { get; }
        public ICommand ConfirmDeleteCommand { get; }
        public ICommand CancelDeletePersonCommand { get; }

        public RoomViewModel(IGroupNameService groupnameService, IExcelPersonImporter excelImporter, IExcelPersonExporter excelExporter)
        {
            _groupnameService = groupnameService;
            _excelImporter = excelImporter;
            _excelExporter = excelExporter;

            GroupNames = new ObservableCollection<GroupName>();

            LoadRoomCommand = new RelayCommand(async () => await LoadRoomAsync());
            ImportRoomCommand = new RelayCommand(async () => await ImportRoomAsync());
            ExportPersonCommand = new RelayCommand(async () => await ExportRoomAsync());
            _ = LoadRoomAsync();

        }
        private async Task LoadRoomAsync()
        {
            try
            {
                var roomlist = await _groupnameService.GetAllAsync();
                GroupNames = new ObservableCollection<GroupName>(roomlist);
            }
            catch(Exception ex)
            {
                MessageBox.Show($"Failed to load persons: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

            }
        }
        private async Task ImportRoomAsync()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var data = _excelImporter.ReadRoomFromExcel(dialog.FileName);
                    await _groupnameService.ImportGroupNameFromExcel(data);
                    MessageBox.Show("Import successful!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadRoomAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Import failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private async Task ExportRoomAsync()
        {

        }
    }
}
