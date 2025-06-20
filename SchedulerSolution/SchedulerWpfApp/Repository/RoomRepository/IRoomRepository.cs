using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Repository.RoomRepository
{
    public interface IRoomRepository : IBaseRepository<Room>
    {
        Task<List<Room>> GetNumberOfRoom(int numberOfRoom);
        Task<List<Room>> SearchRoomsAsync(string searchTerm);
        Task<bool> CheckRoomNameExistsAsync(string roomName);
    }
}
