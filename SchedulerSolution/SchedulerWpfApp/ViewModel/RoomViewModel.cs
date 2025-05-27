using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        private readonly IRoomService _roomService;
        private readonly IExcelRoomImport _excelRoomImport;
        private readonly IExcelRoomExporter _excelRoomExporter;
        private ObservableCollection<Room> _roomname;
        private Room? _selectedGroupname;
        public string FormTitle => SelectedRoomname?.RoomId == 0 ? "Thêm phòng mới" : "Chỉnh sửa thông tin phòng";
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


        public ObservableCollection<Room> Rooms
        {
            get => _roomname;
            set => SetProperty(ref _roomname, value);
        }

        public Room SelectedRoomname
        {
            get => _selectedGroupname;
            set
            {
                if (SetProperty(ref _selectedGroupname, value))
                {
                    OnPropertyChanged(nameof(FormTitle)); // 🔥 Notify form title update
                }
            }
        }
        // Open pop up when clicking edit or add
        public bool IsRoomFormOpen
        {
            get => _isRoomOpen;
            set => SetProperty(ref _isRoomOpen, value);
        }
        // Open dialog when click delete
        public bool IsOpenDialog
        {
            get => _isOpenDialog;
            set => SetProperty(ref _isOpenDialog, value);
        }
        // Confirm delete 
        public bool IsConfirmationOpen
        {
            get => _isConfirmationOpen;
            set => SetProperty(ref _isConfirmationOpen, value);
        }
        public bool IsClassIdEditable
        {
            get => _isClassIdEditable;
            set => SetProperty(ref _isClassIdEditable, value);
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



        public RoomViewModel(IRoomService roomService, IExcelRoomImport excelroomImporter, IExcelRoomExporter excelroomExporter)
        {
            _roomService = roomService;
            _excelRoomImport = excelroomImporter;
            _excelRoomExporter = excelroomExporter;


            ImportRoomListCommand = new RelayCommand(async () => await ImportRoomListAsync());
            ExportRoomListCommand = new RelayCommand(async () => await ExportRoomAsync());
            AddListRoomCommand = new RelayCommand(async () => await AddRoomAsync());
            EditRoomListCommand = new RelayCommandGeneric<Room>(async (room) => await EditPersonAsync(room));
            CancelEditRoomCommand = new RelayCommand(CancelEdit);
            SaveRoomCommand = new RelayCommand(async () => await SaveRoomAsync());
            ConfirmDeleteRoomCommand = new RelayCommand(async () => await ConfirmDeleteAsync());
            DeleteRoomListCommand = new RelayCommandGeneric<Room>(async (room) => await DeleteRoomAsync(room));
            CancelDeleteRoomCommand = new RelayCommand(CancelDelete);
            _ = LoadRoomAsync(); // Load the room list asynchronously when the view model is created
        }
        private async Task LoadRoomAsync()
        {
            try
            {
                var roomlist = await _roomService.GetAllAsync();
                // assign _allGroupNames to search and when deleting keywords, re-render the list
                Rooms = new ObservableCollection<Room>(roomlist);
                // call this function to render room list
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load persons: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

            }
        }

        private async Task ImportRoomListAsync()
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
                    var data = _excelRoomImport.ReadRoomListFromExcel(dialog.FileName);
                    // call ImportGroupNameFromExcel function to add new data to database

                    await _roomService.ImportPersonFromExcel(data);
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
            if (Rooms == null || Rooms.Count == 0)
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
                    var roomList = Rooms.Where(p => p != null).ToList();
                    // call ExportToExcelRoom function to export file
                    _excelRoomExporter.ExportRoomToExcel(roomList, dialog.FileName);
                    MessageBox.Show("Export successful!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }


        public async Task AddRoomAsync()
        {

            SelectedRoomname = new Room();
            IsRoomFormOpen = true;
            // check if it is an edit event
            _isEditing = false;
            // allow adding new classid
            IsClassIdEditable = true;

        }
        private async Task EditPersonAsync(Room room)
        {
            if (room == null) return;
            SelectedRoomname = room;
            IsRoomFormOpen = true;
            // check event edit 
            _isEditing = true;
            // do not allow to edit classid
            IsClassIdEditable = false;
        }



        public async Task DeleteRoomAsync(Room room)
        {
            if (room == null) return;
            SelectedRoomname = room;
            IsOpenDialog = true;
        }
        private void CancelDelete()
        {
            // set SelectedGroupname null 

            SelectedRoomname = null;
            IsOpenDialog = false;

        }
        private async Task ConfirmDeleteAsync()
        {
            try
            {
                if (SelectedRoomname != null)
                {
                    await _roomService.DeleteRoom(SelectedRoomname.RoomId);
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


        private void CancelEdit()
        {
            // set SelectedGroupname null 
            SelectedRoomname = null;
            IsRoomFormOpen = false;
        }

        public async Task SaveRoomAsync()
        {
            if (SelectedRoomname == null) return;

            // ✅ Kiểm tra đầu vào trước khi lưu
            if (string.IsNullOrWhiteSpace(SelectedRoomname.RoomName))
            {
                MessageBox.Show("Tên phòng không được để trống", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SelectedRoomname.TotalPersons <= 0)
            {
                MessageBox.Show("Sức chứa phải lớn hơn 0", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (_isEditing)
                {
                    await _roomService.UpdateRoom(SelectedRoomname);
                    MessageBox.Show("Cập nhật thông tin phòng thành công");
                }
                else
                {
                    await _roomService.AddRoom(SelectedRoomname);
                    MessageBox.Show("Thêm phòng mới thành công");
                }

                IsRoomFormOpen = false;
                SelectedRoomname = null;
                await LoadRoomAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lưu phòng: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }
}
