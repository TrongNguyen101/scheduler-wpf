using Microsoft.EntityFrameworkCore;
using SchedulerWpfApp.Data;
using SchedulerWpfApp.Model;
namespace SchedulerWpfApp.Repository.RoomRepository
{
    /// <summary>
    /// Repository for handling Room entity database operations.
    /// Implements the IRoomRepository interface.
    /// </summary>
    public class RoomRepository : BaseRepository<Room>, IRoomRepository
    {
        #region Constructors
        /// <summary>
        /// Initializes a new instance of the RoomRepository class.
        /// </summary>
        /// <param name="context">The database context.</param>
        public RoomRepository(DataContext context) : base(context) { }
        #endregion

        #region Methods
        /// <summary>
        /// Checks if a room with the specified name exists in the database.
        /// </summary>
        /// <param name="roomName">The name of the room to check.</param>
        /// <returns>True if the room exists, otherwise false.</returns>
        /// <exception cref="Exception">Thrown when an error occurs while checking room existence.</exception>
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

        /// <summary>
        /// Retrieves a specific number of rooms of type "Phòng học" (classroom).
        /// </summary>
        /// <param name="numberOfRoom">The number of rooms to retrieve.</param>
        /// <returns>A list of rooms ordered by RoomId.</returns>
        /// <exception cref="Exception">Thrown when an error occurs while retrieving rooms.</exception>
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

        /// <summary>
        /// Searches for rooms whose names contain the specified search term.
        /// </summary>
        /// <param name="searchTerm">The search term to look for in room names.</param>
        /// <returns>A list of rooms matching the search criteria.</returns>
        /// <exception cref="Exception">Thrown when an error occurs while searching for rooms.</exception>
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

        /// <summary>
        /// Retrieves a room based on its unique RoomId.
        /// </summary>
        /// <param name="roomid"></param>
        /// <returns></returns>
        public async Task<Room?> GetRoomByCodeAsync(int roomid)
        {
            return await _context.Rooms.FirstOrDefaultAsync(s => s.RoomId == roomid);
        }
        #endregion
    }
}
