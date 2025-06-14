using SchedulerWpfApp.Model;
namespace SchedulerWpfApp.ServiceRefactor.RoomService
{
    public interface IRoomService
    {
        Task ImportRoomFromExcel(List<Room> listRoomFromExcel);
        Task<List<Room>> GetAllAsync();
        Task<List<Room>> GetNumberOfRoom(int numberOfRoom);
        Task<Room?> GetByIdAsync(int id);
        Task AddRoom(Room room);
        Task UpdateRoom(Room room);
        Task DeleteRoom(int id);
        Task<List<Room>> SearchRoomsAsync(string searchTerm);
        Task<bool> CheckRoomIdExistsAsync(string classId);
        void ExportRoomToExcel(List<Room> rooms, string filePath);
        List<Room> ReadRoomListFromExcel(string filePath);
    }
}
