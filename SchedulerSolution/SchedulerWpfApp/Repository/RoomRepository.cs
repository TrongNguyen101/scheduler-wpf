using SchedulerWpfApp.Data;
using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Repository
{
    public class RoomRepository : BaseRepository<Room>, IRoomRepository
    {
        public RoomRepository(DataContext context) : base(context) { }

        public Task<bool> CheckRoomIdExistsAsync(string classId)
        {
            throw new NotImplementedException();
        }

        public Task ImportRoomFromExcel(List<Room> listRoomFromExcel)
        {
            throw new NotImplementedException();
        }

        public Task<List<Room>> SearchRoomsAsync(string searchTerm)
        {
            throw new NotImplementedException();
        }
    }
}
