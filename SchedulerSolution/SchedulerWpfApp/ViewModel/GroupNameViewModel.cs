using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using SchedulerWpfApp.Helper;
using SchedulerWpfApp.Model;
using SchedulerWpfApp.Services;

namespace SchedulerWpfApp.ViewModel
{
    /// <summary>
    /// ViewModel responsible for managing group names: loading, importing, exporting, adding, editing, and deleting.
    /// </summary>
    public class GroupNameViewModel : ViewBaseModel
    {
        #region Fields
        private readonly IGroupNameService _groupnameService;
        private readonly IExcelPersonImporter _excelImporter;
        private readonly IExcelPersonExporter _excelExporter;

        // declare to list the rooms
        private ObservableCollection<GroupClass> _groupname;
        // declaration used to list the entire list and support search event when deleting keyword then the list will render again
        private ObservableCollection<GroupClass> _allGroupNames;
        // properties when groupName data is displayed in popup
        private GroupClass? _selectedGroupname;
        // keyword search events
        private string _searchKeyword;
        // Open popup when clicking add or edit
        private bool _isGroupNameOpen;
        // Open dialog when click delete button 
        private bool _isOpenDialog;
        // confirm delete
        private bool _isConfirmationOpen;
        // check if it is edit or add event
        private bool _isEditing;
        // check if ClassId is edited
        private bool _IsGroupNameIdEditable = true;
        // used to set the title for the header bar of the popup when editing or adding
        public string FormTitle => SelectedGroupname?.GroupName == "" ? "Thêm lớp mới" : "Chỉnh sửa thông tin lớp";


        #endregion

        #region Constructor
        /// <summary>
        /// Observable collection to hold list of rooms
        /// </summary>
        public ObservableCollection<GroupClass> GroupNames
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
        public GroupClass SelectedGroupname
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
        public bool IsGroupNameFormOpen
        {
            get => _isGroupNameOpen;
            set => SetProperty(ref _isGroupNameOpen, value);
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
        public bool IsGroupNameIdEditable
        {
            get => _IsGroupNameIdEditable;
            set => SetProperty(ref _IsGroupNameIdEditable, value);
        }
        // declare commands that are triggered by events or view titles
        public ICommand AddGroupNameCommand { get; set; }
        public ICommand RemoveRoomCommand { get; set; }
        public ICommand LoadGroupNameCommand { get; }
        public ICommand ExportGroupNameCommand { get; }
        public ICommand ImportGroupNameCommand { get; }
        public ICommand DeleteGroupNameCommand { get; }
        public ICommand SaveGroupNameCommand { get; }
        public ICommand CancelEditGroupNameCommand { get; }
        public ICommand ConfirmDeleteGroupNameCommand { get; }
        public ICommand CancelDeleteGroupNameCommand { get; }
        public ICommand EditGroupNameCommand { get; }

        #endregion

        #region Methods
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
            GroupNames = new ObservableCollection<GroupClass>();
            // add room
            AddGroupNameCommand = new RelayCommand(async () => await AddRoomAsync());
            // load list room
            LoadGroupNameCommand = new RelayCommand(async () => await LoadRoomAsync());
            // import room by excel file 
            ImportGroupNameCommand = new RelayCommand(async () => await ImportRoomAsync());
            // export room by excel file 
            ExportGroupNameCommand = new RelayCommand(async () => await ExportRoomAsync());
            // edit room
            EditGroupNameCommand = new RelayCommandGeneric<GroupClass>(async (groupname) => await EditPersonAsync(groupname));
            // delete room
            DeleteGroupNameCommand = new RelayCommandGeneric<GroupClass>(async (groupname) => await DeletePersonAsync(groupname));
            // save add room or edit room
            SaveGroupNameCommand = new RelayCommand(async () => await SavePersonAsync());
            // cancel edit or add
            CancelEditGroupNameCommand = new RelayCommand(CancelEdit);
            // confirm delete
            ConfirmDeleteGroupNameCommand = new RelayCommand(async () => await ConfirmDeleteAsync());
            // cancel delete
            CancelDeleteGroupNameCommand = new RelayCommand(CancelDelete);
            // asynchronous processing without async await
            _ = LoadRoomAsync();

        }
        /// <summary>
        /// Loads all group names asynchronously from the service and populates the GroupNames collection.
        /// This method retrieves the list of group names from the service and assigns it to the _allGroupNames collection.
        /// </summary>
        /// 
        private async Task LoadRoomAsync()
        {
            try
            {
                var roomlist = await _groupnameService.GetAllAsync();
                // assign _allGroupNames to search and when deleting keywords, re-render the list
                _allGroupNames = new ObservableCollection<GroupClass>(roomlist);
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
                FileName = "GroupName.xlsx"
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
            SelectedGroupname = new GroupClass();
            // turn on pop up
            IsGroupNameFormOpen = true;
            // check if it is an edit event
            _isEditing = false;
            // allow adding new classid
            IsGroupNameIdEditable = true;
        }

        /// <summary>
        /// Opens the edit form for a selected room.
        /// This method sets the SelectedGroupname to the room being edited,
        /// turns on the room form, and sets the editing state.
        /// </summary>
         
        private async Task EditPersonAsync(GroupClass groupname)
        {
            if (groupname == null) return;
            SelectedGroupname = new GroupClass
            {
                GroupName = groupname.GroupName,
                CurriculumCode = groupname.CurriculumCode,
                Major = groupname.Major,
                Term = groupname.Term,
                Department = groupname.Department,
            };
            IsGroupNameFormOpen = true;
            // check event edit 
            _isEditing = true;
            // do not allow to edit classid
            IsGroupNameIdEditable = false;
        }

        /// <summary>
        /// Saves the current room data, either adding a new room or updating an existing one.
        /// This method checks if the ClassId is not empty, verifies if the room already exists,
        /// </summary>
        
        private async Task SavePersonAsync()
        {
            try
            {
                if (SelectedGroupname == null)
                {
                    MessageBox.Show("Vui lòng nhập thông tin lớp học.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    IsGroupNameFormOpen = true; // Đóng form nếu không có dữ liệu
                    return;
                }
                // check blank - Kiểm tra dữ liệu trống TRƯỚC KHI làm gì khác
                if (string.IsNullOrWhiteSpace(SelectedGroupname?.GroupName) ||
                    string.IsNullOrWhiteSpace(SelectedGroupname?.CurriculumCode) ||
                    string.IsNullOrWhiteSpace(SelectedGroupname?.Major) ||
                    string.IsNullOrWhiteSpace(SelectedGroupname?.Department)||
                    string.IsNullOrWhiteSpace(SelectedGroupname?.Term))
                {
                    MessageBox.Show("Dữ liệu không được để trống", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    IsGroupNameFormOpen = true; // Mở lại form nếu có dữ liệu trống
                    return;
                }
                // Kiểm tra số lượng âm
                //if (SelectedGroupname?.Term < 0)
                //{
                //    MessageBox.Show("Số lượng học viên và số lượng lịch học phải lớn hơn hoặc bằng 0", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                //    return;
                //}
                // check which event is edit or add
                if (_isEditing)
                {
                    // Khi edit, chỉ cần kiểm tra ClassId có tồn tại không
                    bool exists = await _groupnameService.CheckClassIdExistsAsync(SelectedGroupname.GroupName);
                    if (exists)
                    {
                        // Dữ liệu đã được validate ở trên rồi, an toàn để update
                        await _groupnameService.UpdateGroupName(SelectedGroupname);
                        MessageBox.Show("Cập nhật lớp thành công", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);

                        await LoadRoomAsync();
                        IsGroupNameFormOpen = false; // Đóng form sau khi save thành công
                        _isEditing = false;
                    }
                    else
                    {
                        MessageBox.Show("Lớp không tồn tại", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else // Add mode
                {
                    // Kiểm tra trùng ClassId
                    bool exists = await _groupnameService.CheckClassIdExistsAsync(SelectedGroupname.GroupName);
                    if (!exists)
                    {
                        await _groupnameService.AddGroupName(SelectedGroupname);
                        MessageBox.Show("Thêm lớp mới thành công", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                        await LoadRoomAsync();
                        IsGroupNameFormOpen = false; // Đóng form sau khi save thành công
                        _isEditing = false;
                    }
                    else
                    {
                        MessageBox.Show("Lớp này đã tồn tại", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lưu thất bại: {ex.Message}");
            }
        }

        /// <summary>
        /// Deletes a selected room after confirmation.
        /// This method sets the SelectedGroupname to the room to be deleted,
        /// opens the confirmation dialog, and waits for user confirmation.
        /// </summary>
        /// <param name="groupname"></param>
         
        private async Task DeletePersonAsync(GroupClass groupname)
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
                    await _groupnameService.DeleteGroupName(SelectedGroupname.GroupName);
                    MessageBox.Show("Xóa Thành Công", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
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
            IsGroupNameFormOpen = false;
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
            GroupNames = new ObservableCollection<GroupClass>(_allGroupNames);
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
                    (!string.IsNullOrEmpty(room.GroupName) && room.GroupName.ToLower().Contains(lowerKeyword)) ||
                    (!string.IsNullOrEmpty(room.CurriculumCode) && room.CurriculumCode.ToLower().Contains(lowerKeyword)) ||
                    (!string.IsNullOrEmpty(room.Major) && room.Major.ToLower().Contains(lowerKeyword))
                ).ToList();

                GroupNames = new ObservableCollection<GroupClass>(filtered);
            }
        }
    }
    #endregion
}
