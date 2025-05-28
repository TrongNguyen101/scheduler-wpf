using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using SchedulerWpfApp.Helper;
using SchedulerWpfApp.Model;
using SchedulerWpfApp.Services;

namespace SchedulerWpfApp.ViewModel
{
    public class GroupNameViewModel : ViewBaseModel
    {
        private readonly IGroupNameService _groupnameService;
        private readonly IExcelPersonImporter _excelImporter;
        private readonly IExcelPersonExporter _excelExporter;

        // declare to list the rooms
        private ObservableCollection<GroupName> _groupname;
        // declaration used to list the entire list and support search event when deleting keyword then the list will render again
        private ObservableCollection<GroupName> _allGroupNames;
        // properties when room data is displayed in popup
        private GroupName? _selectedGroupname;
        // keyword search events
        private string _searchKeyword;
        // Open popup when clicking add or edit
        private bool _isRoomOpen;
        // Open dialog when click delete button 
        private bool _isOpenDialog;
        // confirm delete
        private bool _isConfirmationOpen;
        // check if it is edit or add event
        private bool _isEditing;
        // check if ClassId is edited
        private bool _isClassIdEditable = true;
        // used to set the title for the header bar of the popup when editing or adding
        public string FormTitle => SelectedGroupname?.ClassId == "" ? "Thêm lớp mới" : "Chỉnh sửa thông tin lớp";
        /// <summary>
        /// Observable collection to hold list of rooms
        /// </summary>
        public ObservableCollection<GroupName> GroupNames
        {
            get => _groupname;
            set => SetProperty(ref _groupname, value);
        }
        /// <summary>
        /// Search keyword, triggers filtering when updated
        /// </summary>
        public string SearchKeyword
        {
            get => _searchKeyword;
            set
            {
                if (SetProperty(ref _searchKeyword, value))
                {
                    // use function fillter list by keyword
                    FilterRooms();
                }
            }
        }

        /// <summary>
        /// Selected group name, triggers form title update when changed
        /// </summary>
        public GroupName SelectedGroupname
        {
            get => _selectedGroupname;
            set
            {
                if (SetProperty(ref _selectedGroupname, value))
                {
                    OnPropertyChanged(nameof(FormTitle)); //Notify form title update
                }
            }
        }
        /// <summary>
        /// Open pop up when clicking edit or add
        /// </summary>
        public bool IsRoomFormOpen
        {
            get => _isRoomOpen;
            set => SetProperty(ref _isRoomOpen, value);
        }
        /// <summary>
        /// Open dialog when click delete
        /// </summary>
        public bool IsOpenDialog
        {
            get => _isOpenDialog;
            set => SetProperty(ref _isOpenDialog, value);
        }
        /// <summary>
        /// Confirm delete 
        /// </summary>
        public bool IsConfirmationOpen
        {
            get => _isConfirmationOpen;
            set => SetProperty(ref _isConfirmationOpen, value);
        }
        /// <summary>
        /// Cancel edit
        /// </summary>
        public bool IsClassIdEditable
        {
            get => _isClassIdEditable;
            set => SetProperty(ref _isClassIdEditable, value);
        }
        // declare commands that are triggered by events or view titles
        public ICommand AddClassRoomCommand { get; set; }
        public ICommand RemoveRoomCommand { get; set; }
        public ICommand LoadRoomCommand { get; }
        public ICommand ExportRoomCommand { get; }
        public ICommand ImportRoomCommand { get; }
        public ICommand DeleteRoomCommand { get; }
        public ICommand SaveRoomCommand { get; }
        public ICommand CancelEditRoomCommand { get; }
        public ICommand ConfirmDeleteRoomCommand { get; }
        public ICommand CancelDeleteRoomCommand { get; }
        public ICommand EditRoomCommand { get; }
        /// <summary>
        ///  Constructor initializes dependencies and commands.
        /// Initializes the GroupNameViewModel with services for managing group names and importing/exporting data.
        /// </summary>
        public GroupNameViewModel(IGroupNameService groupnameService, IExcelPersonImporter excelImporter, IExcelPersonExporter excelExporter)
        {
            // assign variables to the corresponding Service object
            _groupnameService = groupnameService;
            _excelImporter = excelImporter;
            _excelExporter = excelExporter;
            // Execute command according to each event corresponding to the processing functions
            GroupNames = new ObservableCollection<GroupName>();
            // add room
            AddClassRoomCommand = new RelayCommand(async () => await AddRoomAsync());
            // load list room
            LoadRoomCommand = new RelayCommand(async () => await LoadRoomAsync());
            // import room by excel file 
            ImportRoomCommand = new RelayCommand(async () => await ImportRoomAsync());
            // export room by excel file 
            ExportRoomCommand = new RelayCommand(async () => await ExportRoomAsync());
            // edit room
            EditRoomCommand = new RelayCommandGeneric<GroupName>(async (groupname) => await EditPersonAsync(groupname));
            // delete room
            DeleteRoomCommand = new RelayCommandGeneric<GroupName>(async (groupname) => await DeletePersonAsync(groupname));
            // save add room or edit room
            SaveRoomCommand = new RelayCommand(async () => await SavePersonAsync());
            // cancel edit or add
            CancelEditRoomCommand = new RelayCommand(CancelEdit);
            // confirm delete
            ConfirmDeleteRoomCommand = new RelayCommand(async () => await ConfirmDeleteAsync());
            // cancel delete
            CancelDeleteRoomCommand = new RelayCommand(CancelDelete);
            // asynchronous processing without async await
            _ = LoadRoomAsync();

        }
        /// <summary>
        /// Loads all group names asynchronously from the service and populates the GroupNames collection.
        /// This method retrieves the list of group names from the service and assigns it to the _allGroupNames collection.
        /// </summary>
        private async Task LoadRoomAsync()
        {
            try
            {
                var roomlist = await _groupnameService.GetAllAsync();
                // assign _allGroupNames to search and when deleting keywords, re-render the list
                _allGroupNames = new ObservableCollection<GroupName>(roomlist);
                // call this function to render room list
                ResetToAllGroupNames();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load persons: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Imports room data from an Excel file using a file dialog.
        /// This method opens a file dialog to select an Excel file,
        /// </summary>
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
                    // call ReadRoomFromExcel function to process file and read file when importing
                    var data = _excelImporter.ReadRoomFromExcel(dialog.FileName);
                    // call ImportGroupNameFromExcel function to add new data to database

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

        /// <summary>
        /// Exports the current list of rooms to an Excel file using a file dialog.
        /// This method opens a file dialog to select the save location and file name,
        /// then calls the export service to save the room data to an Excel file.
        /// </summary>
        private async Task ExportRoomAsync()
        {
            if (GroupNames == null || GroupNames.Count == 0)
            {
                MessageBox.Show("No persons to export.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = "Room.xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    //Filter the GroupNames list to remove null elements
                    var roomList = GroupNames.Where(p => p != null).ToList();
                    // call ExportToExcelRoom function to export file
                    _excelExporter.ExportToExcelRoom(roomList, dialog.FileName);
                    MessageBox.Show("Export successful!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Opens a dialog to add a new room or edit an existing one.
        /// This method initializes a new GroupName object, opens the room form,
        /// </summary>
        private async Task AddRoomAsync()
        {
            SelectedGroupname = new GroupName();
            // turn on pop up
            IsRoomFormOpen = true;
            // check if it is an edit event
            _isEditing = false;
            // allow adding new classid
            IsClassIdEditable = true;
        }

        /// <summary>
        /// Saves the current room data, either adding a new room or updating an existing one.
        /// This method checks if the ClassId is not empty, verifies if the room already exists,
        /// </summary>
        private async Task SavePersonAsync()
        {
            try
            {
                // check blank
                if (string.IsNullOrWhiteSpace(SelectedGroupname?.ClassId))
                {
                    MessageBox.Show("Mã lớp không được để trống");
                    return;
                }
                // check which event is edit or add
                if (_isEditing)
                {
                    // check duplicate classid
                    bool exists = await _groupnameService.CheckClassIdExistsAsync(SelectedGroupname.ClassId);
                    if (exists)
                    {
                        // call UpdateGroupName to update information
                        await _groupnameService.UpdateGroupName(SelectedGroupname);
                        MessageBox.Show("Cập nhật lớp thành công");
                        await LoadRoomAsync();
                        _isEditing = false;
                    }
                    else
                    {
                        MessageBox.Show("Room không tồn tại");
                    }
                }
                // if it is an add event
                else
                {
                    // check duplicate classid
                    bool exists = await _groupnameService.CheckClassIdExistsAsync(SelectedGroupname.ClassId);
                    if (!exists)
                    {
                        // If not duplicate, call function AddGroupName to add
                        await _groupnameService.AddGroupName(SelectedGroupname);
                        GroupNames.Add(SelectedGroupname);
                        MessageBox.Show("Thêm lớp mới thành công");
                        await LoadRoomAsync();
                        _isEditing = false;
                    }
                    else
                    {
                        MessageBox.Show("Lớp này đã tồn tại");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lưu thất bại: {ex.Message}");
            }
        }

        /// <summary>
        /// Opens the edit form for a selected room.
        /// This method sets the SelectedGroupname to the room being edited,
        /// turns on the room form, and sets the editing state.
        /// </summary>
        private async Task EditPersonAsync(GroupName groupname)
        {
            if (groupname == null) return;
            SelectedGroupname = groupname;
            IsRoomFormOpen = true;
            // check event edit 
            _isEditing = true;
            // do not allow to edit classid
            IsClassIdEditable = false;
        }

        /// <summary>
        /// Deletes a selected room after confirmation.
        /// This method sets the SelectedGroupname to the room to be deleted,
        /// opens the confirmation dialog, and waits for user confirmation.
        /// </summary>
        /// <param name="groupname"></param>
        private async Task DeletePersonAsync(GroupName groupname)
        {

            if (groupname == null) return;
            SelectedGroupname = groupname;
            IsOpenDialog = true;
        }

        /// <summary>
        /// Confirms the deletion of the selected room.
        /// This method checks if a room is selected,
        /// attempts to delete it using the service, and reloads the room list.
        /// </summary>
        private async Task ConfirmDeleteAsync()
        {
            try
            {
                if (SelectedGroupname != null)
                {
                    await _groupnameService.DeleteGroupName(SelectedGroupname.ClassId);
                    MessageBox.Show("Xóa Thành Công");
                    await LoadRoomAsync();
                    IsOpenDialog = false;
                }
                else
                {
                    MessageBox.Show("Vui lòng chọn trước khi xóa");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Xóa thất bại: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// Cancels the current edit or add operation and closes the room form.
        /// This method sets the SelectedGroupname to null,
        /// </summary>
        private void CancelEdit()
        {
            // set SelectedGroupname null 
            SelectedGroupname = null;
            IsRoomFormOpen = false;
        }
        /// <summary>
        /// Cancels the delete operation and closes the confirmation dialog.
        /// This method sets the SelectedGroupname to null and closes the dialog.
        /// </summary>

        private void CancelDelete()
        {
            // set SelectedGroupname null 

            SelectedGroupname = null;
            IsOpenDialog = false;
        }

        /// <summary>
        /// Resets the GroupNames collection to include all group names.
        /// This method assigns the _allGroupNames collection to the GroupNames property,
        /// </summary>
        private void ResetToAllGroupNames()
        {
            GroupNames = new ObservableCollection<GroupName>(_allGroupNames);
        }

        /// <summary>
        /// Filters the GroupNames collection based on the search keyword.
        /// If the search keyword is empty, it resets to show all group names.
        /// </summary>
        private void FilterRooms()
        {
            if (string.IsNullOrWhiteSpace(SearchKeyword))
            {
                ResetToAllGroupNames();
            }
            else
            {
                // enter keyword from box
                var lowerKeyword = SearchKeyword.ToLower();
                // can search by ClassId, Category, Major
                var filtered = _allGroupNames.Where(room =>
                    (!string.IsNullOrEmpty(room.ClassId) && room.ClassId.ToLower().Contains(lowerKeyword)) ||
                    (!string.IsNullOrEmpty(room.Category) && room.Category.ToLower().Contains(lowerKeyword)) ||
                    (!string.IsNullOrEmpty(room.Major) && room.Major.ToLower().Contains(lowerKeyword))
                ).ToList();

                GroupNames = new ObservableCollection<GroupName>(filtered);
            }
        }
    }
}
