using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Repository.RoomRepo
{
    public interface IRoomRepository : IBaseRepository<Room>
    {
        Task<List<Room>> GetNumberOfRoom(int numberOfRoom);
        Task<List<Room>> SearchRoomsAsync(string searchTerm);
        Task<bool> CheckRoomIdExistsAsync(string roomName);
        Task<Room> GetRoomByCodeAsync(int roomid);
    }
}
