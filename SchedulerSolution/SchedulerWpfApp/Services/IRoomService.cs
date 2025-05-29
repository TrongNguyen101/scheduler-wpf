using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Services
{
    public interface IRoomService
    {
        Task ImportRoomFromExcel(List<Room> listRoomFromExcel);
        Task<List<Room>> GetAllAsync();
        Task<Room?> GetByIdAsync(int id);
        Task AddRoom(Room room);
        Task UpdateRoom(Room room);
        Task DeleteRoom(int id);
        Task<List<Room>> SearchRoomsAsync(string searchTerm);
        Task<bool> CheckRoomIdExistsAsync(string classId);
    }
}
