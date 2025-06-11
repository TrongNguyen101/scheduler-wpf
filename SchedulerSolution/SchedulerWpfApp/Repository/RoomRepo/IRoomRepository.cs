using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Repository.RoomRepo
{
    public interface IRoomRepository : IBaseRepository<Room>
    {
        Task<List<Room>> GetNumberOfRoom(int numberOfRoom);
        Task ImportRoomFromExcel(List<Room> listRoomFromExcel);
        Task<List<Room>> SearchRoomsAsync(string searchTerm);
        Task<bool> CheckRoomIdExistsAsync(string roomName);
    }
}
