using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using SchedulerWpfApp.Helper;
using SchedulerWpfApp.Model;
using SchedulerWpfApp.ServiceRefactor.GroupNameService;
using SchedulerWpfApp.ServiceRefactor.NotificationService;
namespace SchedulerWpfApp.ViewModel
{
    /// <summary>
    /// ViewModel responsible for managing group names: loading, importing, exporting, adding, editing, and deleting.
    /// </summary>
    public class GroupNameViewModel : ViewBaseModel
    {
        #region Fields
        private readonly IGroupNameService _groupnamelistService;
        private readonly INotificationService _notificationService;
        // declare to list the groupnames
        private ObservableCollection<GroupClass> _groupnamelist;
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
        private int _progressValue;
        private bool _isProgressBarOpen;
        // used to set the title for the header bar of the popup when editing or adding
        public string FormTitle => SelectedGroupname?.GroupName == "" ? "Thêm lớp mới" : "Chỉnh sửa thông tin lớp";
        public ObservableCollection<string> PartOfDayInTheFirstTerms { get; } = new() { "A", "P" }; // AM, PM 
        public ObservableCollection<string> TeachingMode { get; set; } = new()  { "ON",
            "OFF",
            "ON/OFF",
            "C-On",
            "EXE",
            "OJT"
        };
        #endregion

        #region Constructor
        /// <summary>
        /// Observable collection to hold list of groupnames
        /// </summary>
        public ObservableCollection<GroupClass> GroupNames
        {
            get => _groupnamelist;
            set => SetProperty(ref _groupnamelist, value);
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
                    Filtergroupname();
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

        public int ProgressValue
        {
            get => _progressValue;
            set { _progressValue = value; OnPropertyChanged(); }
        }

        public bool IsProgressBarOpen
        {
            get => _isProgressBarOpen;
            set => SetProperty(ref _isProgressBarOpen, value);
        }

        // declare commands that are triggered by events or view titles
        public ICommand AddGroupNameCommand { get; set; }
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
        public GroupNameViewModel(IGroupNameService groupnameService, INotificationService notificationService)
        {
            // assign variables to the corresponding Service object
            _groupnamelistService = groupnameService;
            // Initialize notification service
            _notificationService = notificationService;
            // Execute command according to each event corresponding to the processing functions
            GroupNames = new ObservableCollection<GroupClass>();
            // add groupname
            AddGroupNameCommand = new RelayCommand(async () => await AddGroupNameAsync());
            // load list groupname
            LoadGroupNameCommand = new RelayCommand(async () => await LoadGroupNameAsync());
            //import groupname by excel file
            ImportGroupNameCommand = new RelayCommand(async () => await ImportGroupNamelistAsync());
            // export groupname by excel file 
            ExportGroupNameCommand = new RelayCommand(async () => await ExportGroupNameAsync());
            // edit groupname
            EditGroupNameCommand = new RelayCommandGeneric<GroupClass>(async (groupname) => await EditGroupNameAsync(groupname));
            // delete groupname
            DeleteGroupNameCommand = new RelayCommandGeneric<GroupClass>(async (groupname) => await DeleteGroupNameAsync(groupname));
            // save add groupname or edit groupname
            SaveGroupNameCommand = new RelayCommand(async () => await SaveGroupNameAsync());
            // cancel edit or add
            CancelEditGroupNameCommand = new RelayCommand(CancelEdit);
            // confirm delete
            ConfirmDeleteGroupNameCommand = new RelayCommand(async () => await ConfirmDeleteAsync());
            // cancel delete
            CancelDeleteGroupNameCommand = new RelayCommand(CancelDelete);
            // asynchronous processing without async await
            _ = LoadGroupNameAsync();

        }
        /// <summary>
        /// Loads all group names asynchronously from the service and populates the GroupNames collection.
        /// This method retrieves the list of group names from the service and assigns it to the _allGroupNames collection.
        /// </summary>
        /// 
        private async Task LoadGroupNameAsync()
        {
            try
            {
                var groupnamelist = await _groupnamelistService.GetAllAsync();
                // assign _allGroupNames to search and when deleting keywords, re-render the list
                _allGroupNames = new ObservableCollection<GroupClass>(groupnamelist);
                // call this function to render groupname list
                ResetToAllGroupNames();
            }
            catch (Exception)
            {
                _notificationService.ShowError($"Lấy danh sách lớp học không thành công");
            }
        }

        /// <summary>
        /// Imports groupnamelist data from an Excel file using a file dialog.
        /// This method opens a file dialog to select an Excel file,
        /// </summary>
        private async Task ImportGroupNamelistAsync()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx"
            };

            var progres = new Progress<int>(percentCompleted =>
            {
                ProgressValue = percentCompleted;
            });

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var data = _groupnamelistService.ReadGroupNameFromExcel(dialog.FileName);

                    IsProgressBarOpen = true;
                    await _groupnamelistService.ImportGroupNameFromExcel(data, progres);
                    IsProgressBarOpen = false;
                    _notificationService.ShowSuccess("Nhập danh sách lớp học thành công!!");
                    await LoadGroupNameAsync();
                }
                catch (Exception ex)
                {
                    IsProgressBarOpen = false;
                    _notificationService.ShowError($"Nhập danh sách lớp học thất bại. {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Exports the current list of groupnames to an Excel file using a file dialog.
        /// This method opens a file dialog to select the save location and file name,
        /// then calls the export service to save the groupname data to an Excel file.
        /// </summary>
        private async Task ExportGroupNameAsync()
        {
            if (GroupNames == null || GroupNames.Count == 0)
            {
                _notificationService.ShowWarning("Không có lớp nào để xuất.");
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
                    var groupnameList = GroupNames.Where(groupname => groupname != null).ToList();
                    // call ExportToExcelgroupname function to export file
                    _groupnamelistService.ExportToExcelGroupName(groupnameList, dialog.FileName);
                    _notificationService.ShowSuccess("Xuất danh sách lớp học thành công!");
                }
                catch (Exception)
                {
                    _notificationService.ShowError($"Xuất danh sách lớp học thất bại.");
                }
            }
        }

        /// <summary>
        /// Opens a dialog to add a new groupname or edit an existing one.
        /// This method initializes a new GroupName object, opens the groupname form,
        /// </summary>
        private async Task AddGroupNameAsync()
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
        /// Opens the edit form for a selected groupname.
        /// This method sets the SelectedGroupname to the groupname being edited,
        /// turns on the groupname form, and sets the editing state.
        /// </summary>

        private async Task EditGroupNameAsync(GroupClass groupname)
        {
            if (groupname == null) return;
            SelectedGroupname = new GroupClass
            {
                GroupName = groupname.GroupName,
                CurriculumCode = groupname.CurriculumCode,
                Major = groupname.Major,
                Term = groupname.Term,
                Department = groupname.Department,
                TeachingMode = groupname.TeachingMode,
                PartOfDayInTheFirstTerm = groupname.PartOfDayInTheFirstTerm,
            };
            IsGroupNameFormOpen = true;
            // check event edit 
            _isEditing = true;
            // do not allow to edit classid
            IsGroupNameIdEditable = false;
        }

        /// <summary>
        /// Saves the current groupname data, either adding a new groupname or updating an existing one.
        /// This method checks if the ClassId is not empty, verifies if the groupname already exists,
        /// </summary>

        private async Task SaveGroupNameAsync()
        {
            try
            {
                if (SelectedGroupname == null)
                {
                    _notificationService.ShowWarning("Vui lòng nhập thông tin lớp học.");
                    IsGroupNameFormOpen = true; // Đóng form nếu không có dữ liệu
                    return;
                }
                // check blank - Kiểm tra dữ liệu trống TRƯỚC KHI làm gì khác
                if (string.IsNullOrWhiteSpace(SelectedGroupname?.GroupName) ||
                    string.IsNullOrWhiteSpace(SelectedGroupname?.CurriculumCode) ||
                    string.IsNullOrWhiteSpace(SelectedGroupname?.Major) ||
                    string.IsNullOrWhiteSpace(SelectedGroupname?.Department) ||
                    string.IsNullOrWhiteSpace(SelectedGroupname?.TeachingMode) ||
                    string.IsNullOrWhiteSpace(SelectedGroupname?.PartOfDayInTheFirstTerm) ||
                    SelectedGroupname.Term == 0)
                {
                    _notificationService.ShowWarning("Dữ liệu lớp học không được để trống");
                    IsGroupNameFormOpen = true; // Mở lại form nếu có dữ liệu trống
                    return;
                }
                if (SelectedGroupname.Term < 0 || SelectedGroupname.Term > 9)
                {
                    _notificationService.ShowWarning("Học kỳ phải lớn hơn 0 và bé hơn 9");
                    IsGroupNameFormOpen = true; // Mở lại form nếu học kỳ không hợp lệ
                    return;
                }
                if (_isEditing)
                {
                    // Khi edit, chỉ cần kiểm tra ClassId có tồn tại không
                    bool exists = await _groupnamelistService.CheckGroupNameExistsAsync(SelectedGroupname.GroupName);
                    var existingLecture = _allGroupNames.FirstOrDefault(s => s.GroupName == SelectedGroupname.GroupName);

                    if (exists)
                    {
                        existingLecture.CurriculumCode = SelectedGroupname.CurriculumCode;
                        existingLecture.Major = SelectedGroupname.Major;
                        existingLecture.Department = SelectedGroupname.Department;
                        existingLecture.Term = SelectedGroupname.Term;
                        existingLecture.TeachingMode = SelectedGroupname.TeachingMode;
                        existingLecture.PartOfDayInTheFirstTerm = SelectedGroupname.PartOfDayInTheFirstTerm;
                        // Dữ liệu đã được validate ở trên rồi, an toàn để update
                        await _groupnamelistService.UpdateGroupName(existingLecture);
                        _notificationService.ShowSuccess("Cập nhật lớp thành công");
                        await LoadGroupNameAsync();
                        IsGroupNameFormOpen = false; // Đóng form sau khi save thành công
                        _isEditing = false;
                    }
                    else
                    {
                        _notificationService.ShowWarning("Lớp không tồn tại");
                    }
                }
                else // Add mode
                {
                    // Kiểm tra trùng ClassId
                    bool exists = await _groupnamelistService.CheckGroupNameExistsAsync(SelectedGroupname.GroupName);
                    if (!exists)
                    {
                        await _groupnamelistService.AddGroupName(SelectedGroupname);
                        _notificationService.ShowSuccess("Thêm lớp mới thành công");
                        await LoadGroupNameAsync();
                        IsGroupNameFormOpen = false; // Đóng form sau khi save thành công
                        _isEditing = false;
                    }
                    else
                    {
                        _notificationService.ShowWarning("Lớp này đã tồn tại.");
                    }
                }
            }
            catch (Exception)
            {
                _notificationService.ShowError($"Lưu thất bại.");
            }
        }

        /// <summary>
        /// Deletes a selected groupname after confirmation.
        /// This method sets the SelectedGroupname to the groupname to be deleted,
        /// opens the confirmation dialog, and waits for user confirmation.
        /// </summary>
        /// <param name="groupname"></param>

        private async Task DeleteGroupNameAsync(GroupClass groupname)
        {

            if (groupname == null) return;
            SelectedGroupname = groupname;
            IsOpenDialog = true;
        }

        /// <summary>
        /// Confirms the deletion of the selected groupname.
        /// This method checks if a groupname is selected,
        /// attempts to delete it using the service, and reloads the groupname list.
        /// </summary>

        private async Task ConfirmDeleteAsync()
        {
            try
            {
                if (SelectedGroupname != null)
                {
                    await _groupnamelistService.DeleteGroupName(SelectedGroupname.GroupName);
                    _notificationService.ShowSuccess("Xóa lớp thành công");
                    await LoadGroupNameAsync();
                    IsOpenDialog = false;
                }
                else
                {
                    _notificationService.ShowWarning("Vui lòng chọn lớp cần xóa trước khi xác nhận xóa.");
                }
            }
            catch (Exception)
            {
                _notificationService.ShowError($"Xóa lớp thất bại.");
            }
        }

        /// <summary>
        /// Cancels the current edit or add operation and closes the groupname form.
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
        private void Filtergroupname()
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
                var filtered = _allGroupNames.Where(groupname =>
                    (!string.IsNullOrEmpty(groupname.GroupName) && groupname.GroupName.ToLower().Contains(lowerKeyword)) ||
                    (!string.IsNullOrEmpty(groupname.CurriculumCode) && groupname.CurriculumCode.ToLower().Contains(lowerKeyword)) ||
                    (!string.IsNullOrEmpty(groupname.Major) && groupname.Major.ToLower().Contains(lowerKeyword))
                ).ToList();

                GroupNames = new ObservableCollection<GroupClass>(filtered);
            }
        }
    }
    #endregion
}
