using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Repository
{
    public interface IRoomRepository : IBaseRepository<Room>
    {
        Task ImportRoomFromExcel(List<Room> listRoomFromExcel);
        Task<List<Room>> SearchRoomsAsync(string searchTerm);
        Task<bool> CheckRoomIdExistsAsync(string classId);
    }
}
