using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using SchedulerWpfApp.Helper;
using SchedulerWpfApp.Model;
using SchedulerWpfApp.ServiceRefactor.NotificationService;
using SchedulerWpfApp.ServiceRefactor.RoomService;

namespace SchedulerWpfApp.ViewModel
{
    /// <summary>
    /// ViewModel responsible for managing rooms: loading, importing, exporting, adding, editing, and deleting rooms.
    /// </summary>
    public class RoomViewModel : ViewBaseModel
    {
        #region Fields
        private readonly IRoomService _roomService;
        private readonly INotificationService _notificationService;
        private ObservableCollection<Room> _roomlist;
        private Room? _selectedRoom;
        public string FormTitle => SelectedRoom?.RoomId == 0 ? "Thêm phòng mới" : "Chỉnh sửa thông tin phòng";
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
        private bool _isRoomNameEditable = true;
        private int _progressValue;
        private bool _isProgressBarOpen;
        private ObservableCollection<Room> _allRoom;
        #endregion

        #region Contrucstor
        /// <summary>
        /// Observable collection of Room objects representing the list of rooms.
        /// This collection is used to bind to the UI and update dynamically when rooms are added, edited, or deleted.
        /// </summary>
        public ObservableCollection<Room> Rooms
        {
            get => _roomlist;
            set => SetProperty(ref _roomlist, value);
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
                    FilterRoom();
                }
            }
        }

        /// <summary>
        /// Gets or sets the selected Room object.
        /// This property is used to bind the selected room in the UI, allowing for editing or deletion.
        /// </summary>
        public Room SelectedRoom
        {
            get => _selectedRoom;
            set
            {
                if (SetProperty(ref _selectedRoom, value))
                {
                    OnPropertyChanged(nameof(FormTitle)); //Notify form title update
                }
            }
        }

        /// <summary>
        /// Gets or sets the search keyword for filtering rooms.
        /// This property is used to bind the search input in the UI, allowing users to filter the room list based on their input.
        /// </summary>
        public bool IsRoomFormOpen
        {
            get => _isRoomOpen;
            set => SetProperty(ref _isRoomOpen, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the dialog for adding or editing a room is open.
        /// This property is used to control the visibility of the room form in the UI.
        /// </summary>
        public bool IsOpenDialog
        {
            get => _isOpenDialog;
            set => SetProperty(ref _isOpenDialog, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the confirmation dialog for deleting a room is open.
        /// This property is used to control the visibility of the confirmation dialog in the UI.
        /// </summary>
        public bool IsConfirmationOpen
        {
            get => _isConfirmationOpen;
            set => SetProperty(ref _isConfirmationOpen, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the ClassId field is editable.
        /// This property is used to control whether the ClassId can be modified in the room form.
        /// </summary>
        public bool IsRoomNameEditable
        {
            get => _isRoomNameEditable;
            set => SetProperty(ref _isRoomNameEditable, value);
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

        public ICommand ImportRoomListCommand { get; }
        public ICommand ExportRoomListCommand { get; }
        public ICommand EditRoomListCommand { get; }
        public ICommand DeleteRoomListCommand { get; }
        public ICommand AddListRoomCommand { get; }
        public ICommand SaveRoomCommand { get; }
        public ICommand CancelEditRoomCommand { get; }
        public ICommand ConfirmDeleteRoomCommand { get; }
        public ICommand CancelDeleteRoomCommand { get; }
        public List<string> StatusOptions { get; set; } = new List<string> { "available", "unavailable" };
        #endregion

        #region Methods
        /// <summary>
        /// ViewModel constructor that initializes the RoomViewModel with the necessary services.
        /// This constructor sets up the commands for importing, exporting, adding, editing, and deleting rooms.
        /// It also loads the initial list of rooms asynchronously.
        /// </summary>
        /// <param name="roomService"></param>
        /// <param name="excelroomImporter"></param>
        /// <param name="excelroomExporter"></param>
        public RoomViewModel(IRoomService roomService, INotificationService notificationService)
        {
            _roomService = roomService;
            _notificationService = notificationService;
            // Initialize commands for various actions related to room management
            ImportRoomListCommand = new RelayCommand(async () => await ImportRoomListAsync());
            ExportRoomListCommand = new RelayCommand(async () => await ExportRoomAsync());
            AddListRoomCommand = new RelayCommand(async () => await AddRoomAsync());
            EditRoomListCommand = new RelayCommandGeneric<Room>(async (room) => await EditRoomAsync(room));
            CancelEditRoomCommand = new RelayCommand(CancelEdit);
            SaveRoomCommand = new RelayCommand(async () => await SaveRoomAsync());
            ConfirmDeleteRoomCommand = new RelayCommand(async () => await ConfirmDeleteAsync());
            DeleteRoomListCommand = new RelayCommandGeneric<Room>(async (room) => await DeleteRoomAsync(room));
            CancelDeleteRoomCommand = new RelayCommand(CancelDelete);
            SelectedRoom = new Room();

            _ = LoadRoomAsync(); // Load the room list asynchronously when the view model is created
        }

        /// <summary>
        /// Asynchronously loads the list of rooms from the room service.
        /// This method retrieves all rooms and populates the Rooms collection, which is bound to the UI.
        /// It also handles any exceptions that may occur during the loading process and displays an error message if necessary.
        /// </summary>
        private async Task LoadRoomAsync()
        {
            try
            {
                var roomlist = await _roomService.GetAllAsync();
                // assign _allRoom to search and when deleting keywords, re-render the list
                _allRoom = new ObservableCollection<Room>(roomlist);
                ResetToAllRoom();
            }
            catch (Exception)
            {
                _notificationService.ShowError($"Lấy danh sách phòng học không thành công.");
            }
        }
        /// <summary>
        /// Asynchronously imports a list of rooms from an Excel file.
        /// This method opens a file dialog to select an Excel file, reads the room data from the file using the ExcelRoomImport service,
        /// </summary>
        private async Task ImportRoomListAsync()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx"
            };
            var progress = new Progress<int>(percentCompleted =>
            {
                ProgressValue = percentCompleted;
            });

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var data = _roomService.ReadRoomListFromExcel(dialog.FileName);
                    IsProgressBarOpen = true;
                    await _roomService.ImportRoomFromExcel(data, progress);
                    IsProgressBarOpen = false;
                    _notificationService.ShowSuccess("Nhập danh sách phòng học thành công!");
                    await LoadRoomAsync();
                }
                catch (Exception)
                {
                    IsProgressBarOpen = false;
                    _notificationService.ShowError($"Nhập danh sách phòng học không thành công.");
                }
            }
        }

        /// <summary>
        /// Asynchronously exports the list of rooms to an Excel file.
        /// This method opens a save file dialog to specify the file name and location for the exported Excel file,
        /// </summary>
        private async Task ExportRoomAsync()
        {
            if (Rooms == null || Rooms.Count == 0)
            {
                _notificationService.ShowWarning("Không có phòng học để xuất.");
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
                    //Filter the Rooms list to remove null elements
                    var roomList = Rooms.Where(p => p != null).ToList();
                    // call ExportToExcelRoom function to export file
                    _roomService.ExportRoomToExcel(roomList, dialog.FileName);
                    _notificationService.ShowSuccess("Xuất danh sách phòng học thành công!");
                }
                catch (Exception)
                {
                    _notificationService.ShowError($"Xuất danh sách phòng học không thành công.");
                }
            }
        }

        /// <summary>
        /// Asynchronously adds a new room.
        /// This method initializes a new Room object, opens the room form for input,
        /// sets the editing state to false, and allows the ClassId to be editable.
        /// </summary>
        public async Task AddRoomAsync()
        {
            IsRoomFormOpen = true;
            // check if it is an edit event
            _isEditing = false;
            // allow adding new classid
            IsRoomNameEditable = true;
        }

        /// <summary>
        /// Asynchronously edits an existing room.
        /// This method sets the selected room to the one being edited, opens the room form for input,
        /// sets the editing state to true, and prevents the ClassId from being edited.
        /// </summary>
        /// <param name="room"></param>
        private async Task EditRoomAsync(Room room)
        {
            if (room == null) return;
            SelectedRoom = new Room
            {
                RoomId = room.RoomId,
                RoomName = room.RoomName,
                TypeOfRoom = room.TypeOfRoom,
                TotalPersons = room.TotalPersons,
                Floor = room.Floor,
                Building = room.Building,
                Status = room.Status
            };
            IsRoomFormOpen = true;
            // check event edit 
            _isEditing = true;
            // do not allow to edit classid
            IsRoomNameEditable = false;
        }

        /// <summary>
        /// Asynchronously deletes a room.
        /// This method sets the selected room to the one being deleted, opens a confirmation dialog,
        /// and allows the user to confirm or cancel the deletion.
        /// </summary>
        public async Task DeleteRoomAsync(Room room)
        {
            if (room == null) return;
            SelectedRoom = new Room
            {
                RoomId = room.RoomId,
                RoomName = room.RoomName,
                TypeOfRoom = room.TypeOfRoom,
                TotalPersons = room.TotalPersons
            };
            IsOpenDialog = true;
        }

        /// <summary>
        /// Cancels the deletion of a room.
        /// This method sets the selected room to null and closes the confirmation dialog.
        /// </summary>
        private void CancelDelete()
        {
            // set SelectedRooms null 
            SelectedRoom = null;
            IsOpenDialog = false;
        }

        /// <summary>
        /// Confirms the deletion of a room.
        /// This method checks if a room is selected, attempts to delete it using the room service,
        /// displays a success message if the deletion is successful, and reloads the room list.
        /// </summary>
        private async Task ConfirmDeleteAsync()
        {
            try
            {
                if (SelectedRoom != null)
                {
                    await _roomService.DeleteRoom(SelectedRoom.RoomId);
                    _notificationService.ShowSuccess("Xóa phòng thành công!");
                    await LoadRoomAsync();
                    IsOpenDialog = false;
                }
                else
                {
                    _notificationService.ShowWarning("Vui lòng chọn phòng trước khi xóa");
                }
            }
            catch (Exception)
            {
                _notificationService.ShowError($"Xóa phòng không thành công.");
            }
        }

        /// <summary>
        /// Cancels the edit operation for a room.
        /// This method sets the selected room to null and closes the room form.
        /// It is typically called when the user decides not to save changes made in the room form.
        /// </summary>
        private void CancelEdit()
        {
            // set SelectedRoom null 
            SelectedRoom = new Room();
            IsRoomFormOpen = false;
        }

        /// <summary>
        /// Asynchronously saves the current room.
        /// This method checks if the room name and total room are valid before saving.
        /// If the room is being edited, it updates the existing room; otherwise, it adds a new room.
        /// </summary>
        public async Task SaveRoomAsync()
        {
            if (SelectedRoom == null) return;
            // ✅ Kiểm tra đầu vào trước khi lưu
            if (string.IsNullOrWhiteSpace(SelectedRoom.RoomName) ||
                string.IsNullOrWhiteSpace(SelectedRoom.TypeOfRoom) ||
                string.IsNullOrWhiteSpace(SelectedRoom.Building) ||
                string.IsNullOrWhiteSpace(SelectedRoom.Status) ||
                SelectedRoom.TotalPersons == 0 ||
                SelectedRoom.Floor == 0)
            {
                _notificationService.ShowWarning("Dữ liệu phòng học không được để trống");
                IsRoomFormOpen = true;
                return;
            }
            else if (SelectedRoom.TotalPersons < 0 || SelectedRoom.TotalPersons > 50)
            {
                _notificationService.ShowWarning("Số người trong phòng không vượt quá 50 người và không được nhỏ hơn bằng 0");
                IsRoomFormOpen = true;
                return;
            }
            else if (SelectedRoom.Floor < 0)
            {
                _notificationService.ShowWarning("Số tầng không được nhỏ hơn hoặc bằng 0");
                IsRoomFormOpen = true;
                return;
            }
            try
            {
                if (_isEditing)
                {
                    await _roomService.UpdateRoom(SelectedRoom);
                    _notificationService.ShowSuccess("Cập nhật thông tin phòng thành công!");
                }
                else
                {
                    var exists = await _roomService.CheckRoomNameExistsAsync(SelectedRoom.RoomName);
                    if (exists)
                    {
                        _notificationService.ShowWarning("Phòng đã tồn tại. Vui lòng điền dữ liệu khác.");
                        IsRoomFormOpen = true;
                        return;
                    }
                    await _roomService.AddRoom(SelectedRoom);
                    _notificationService.ShowSuccess("Thêm phòng mới thành công!");
                }
                IsRoomFormOpen = false;
                SelectedRoom = new Room();
                await LoadRoomAsync();
            }
            catch (Exception)
            {
                _notificationService.ShowError($"Lỗi khi lưu phòng");
            }
        }

        /// <summary>
        /// Filters the Rooms collection based on the search keyword.
        /// If the search keyword is empty, it resets to show all Rooms.
        /// </summary>
        private void FilterRoom()
        {
            if (string.IsNullOrWhiteSpace(SearchKeyword))
            {
                // If search keyword is empty, reset to all Rooms
                ResetToAllRoom();
            }
            else
            {
                // enter keyword from box
                var lowerKeyword = SearchKeyword.ToLower();
                // can search by RoomCode
                var filtered = _allRoom.Where(Room =>
                (!string.IsNullOrEmpty(Room.RoomName) && Room.RoomName.ToLower().Contains(lowerKeyword)) ||
                (!string.IsNullOrEmpty(Room.Building) && Room.Building.ToLower().Contains(lowerKeyword)) ||
                (int.TryParse(SearchKeyword, out int floorKeyword) && Room.Floor == floorKeyword)
                ).ToList();

                Rooms = new ObservableCollection<Room>(filtered);
            }
        }

        /// <summary>
        /// Reset Rooms
        /// </summary>
        private void ResetToAllRoom()
        {
            Rooms = new ObservableCollection<Room>(_allRoom);
        }
    }
    #endregion
}
