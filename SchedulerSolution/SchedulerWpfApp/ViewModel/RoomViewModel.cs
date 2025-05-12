using System.Collections.ObjectModel;
using System.Windows.Input;
using SchedulerWpfApp.Helper;

namespace SchedulerWpfApp.ViewModel
{
    public class RoomViewModel : ViewBaseModel
    {
        // Observable collection to hold list of rooms
        public ObservableCollection<string> Rooms { get; set; }

        public ICommand AddRoomCommand { get; set; }
        public ICommand RemoveRoomCommand { get; set; }

        public RoomViewModel()
        {
            Rooms = new ObservableCollection<string> { "Room 1", "Room 2", "Room 3" };
            AddRoomCommand = new RelayCommand(AddRoom);
            RemoveRoomCommand = new RelayCommand(RemoveRoom);
        }

        // Add a new room to the list
        private void AddRoom()
        {
            Rooms.Add($"Room {Rooms.Count + 1}");
        }

        // Remove a room from the list
        private void RemoveRoom()
        {
            if (Rooms.Count > 0)
                Rooms.RemoveAt(Rooms.Count - 1);
        }
    }
}
