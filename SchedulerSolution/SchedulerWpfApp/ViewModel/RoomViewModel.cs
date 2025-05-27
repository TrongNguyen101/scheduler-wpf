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



        public ObservableCollection<Room> Rooms
        {
            get => _roomname;
            set => SetProperty(ref _roomname, value);
        }
        public ICommand ImportRoomListCommand { get; }
        public ICommand ExportRoomListCommand { get; }


        public RoomViewModel(IRoomService roomService, IExcelRoomImport excelroomImporter, IExcelRoomExporter excelroomExporter)
        {
            _roomService = roomService;
            _excelRoomImport = excelroomImporter;
            _excelRoomExporter = excelroomExporter;


            ImportRoomListCommand = new RelayCommand(async () => await ImportRoomListAsync());
            ExportRoomListCommand = new RelayCommand(async () => await ExportRoomAsync());

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


    }
}
