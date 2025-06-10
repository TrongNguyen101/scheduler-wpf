using Microsoft.EntityFrameworkCore;
using SchedulerWpfApp.Data;
using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Repository
{
    public class RoomRepository : BaseRepository<Room>, IRoomRepository
    {
        public RoomRepository(DataContext context) : base(context) { }

        public async Task<bool> CheckRoomIdExistsAsync(string roomName)
        {
            try
            {
                var room = await _context.Rooms
                    .FirstOrDefaultAsync(r => r.RoomName.Equals(roomName, StringComparison.OrdinalIgnoreCase));
                if (room == null)
                {
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while checking room ID existence.", ex);
            }
        }

        public async Task<List<Room>> GetNumberOfRoom(int numberOfRoom)
        {
            try
            {
                return await _context.Rooms
                    .Where(r => r.TypeOfRoom == "Phòng học")
                    .OrderBy(r => r.RoomId) // hoặc bất kỳ cột nào bạn muốn sắp xếp
                    .Take(numberOfRoom)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while retrieving the number of rooms.", ex);
            }
        }

        public Task ImportRoomFromExcel(List<Room> listRoomFromExcel)
        {
            // Todo: Implement the method to import rooms from Excel
            throw new NotImplementedException();
        }

        public async Task<List<Room>> SearchRoomsAsync(string searchTerm)
        {
            try
            {
                return await _context.Rooms
                    .Where(r => r.RoomName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while searching for rooms.", ex);
            }
        }
    }
}
