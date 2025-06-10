using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SchedulerWpfApp.Data;
using SchedulerWpfApp.Model;
using SchedulerWpfApp.Repository;
namespace SchedulerWpfApp.Services
{
    /// <summary>
    /// Service class for managing room-related operations.
    /// This class provides methods to import rooms from Excel, retrieve all rooms, get a room by ID, add, update, delete rooms, search for rooms, and check if a room ID exists.
    /// </summary>
    public class RoomService : IRoomService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly DataContext _context;
        private readonly IMapper _mapper;
        public RoomService(DataContext context, IMapper mapper, IUnitOfWork unitOfWork)
        {
            _context = context;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Imports a list of rooms from an Excel file into the database.
        /// This method iterates through the provided list of rooms and adds each room to the database context.
        /// </summary>
        public async Task ImportRoomFromExcel(List<Room> listRoomFromExcel)
        {
            foreach (var room in listRoomFromExcel)
            {
                _context.Rooms.Add(room);
            }
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Retrieves all rooms from the database.
        /// This method returns a list of all rooms stored in the database.
        /// </summary>
        public async Task<List<Room>> GetAllAsync()
        {
            var rooms = await _unitOfWork.Repository<Room>().GetAllAsync();
            return rooms;
        }

        /// <summary>
        /// Retrieves number of rooms from the database.
        /// This method returns a list of all rooms stored in the database.
        /// </summary>
        public async Task<List<Room>> GetNumberOfRoom(int numberOfRoom)
        {
            return await _context.Rooms
                     .Where(r => r.TypeOfRoom == "Phòng học")
                     .OrderBy(r => r.RoomId) // hoặc bất kỳ cột nào bạn muốn sắp xếp
                     .Take(numberOfRoom)
                     .ToListAsync();
        }

        /// <summary>
        /// Retrieves a room by its ID.
        /// This method searches for a room in the database using its unique identifier.
        /// </summary>
        /// <param name="id"></param>
        public async Task<Room?> GetByIdAsync(int id)
        {
            return await _context.Rooms.FindAsync(id);
        }

        /// <summary>
        /// Adds a new room to the database.
        /// This method takes a Room object as input and adds it to the database context, then saves the changes asynchronously.
        /// </summary>
        /// <param name="room"></param>

        public async Task AddRoom(Room room)
        {
            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Retrieves a specific room by their ID
        /// </summary>
        /// <param name="id">The ID of the room to retrieve</param>
        /// <returns>The subject with the specified ID, or null if not found</returns>

        public async Task<Room?> GetByRoomCodeAsync(int roomid)
        {
            return await _context.Rooms.FindAsync(roomid);
        }

        /// <summary>
        /// Updates an existing room in the database.
        /// This method takes a Room object as input, updates the corresponding record in the database, and saves the changes asynchronously.
        /// </summary>
        /// <param name="room"></param>

        public async Task UpdateRoom(Room room)
        {
            var existingRoom = await GetByRoomCodeAsync(room.RoomId);
            if (existingRoom != null)
            {
                _mapper.Map(room, existingRoom);
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Deletes a room from the database by its ID.
        /// This method retrieves the room by its ID, removes it from the database context, and saves the changes asynchronously.
        /// </summary>

        public async Task DeleteRoom(int id)
        {
            var room = await GetByIdAsync(id);
            if (room != null)
            {
                _context.Rooms.Remove(room);
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Searches for rooms in the database based on a search term.
        /// This method filters the rooms whose names contain the specified search term, ignoring case.
        /// </summary>

        public async Task<List<Room>> SearchRoomsAsync(string searchTerm)
        {
            return await _context.Rooms
                .Where(r => r.RoomName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                .ToListAsync();
        }

        /// <summary>
        /// Checks if a room ID exists in the database.
        /// This method checks if there is any room in the database with the specified room name.
        /// </summary>
        /// <param name="roomname"></param>

        public async Task<bool> CheckRoomIdExistsAsync(string roomname)
        {
            return await _context.Rooms.AnyAsync(r => r.RoomName == roomname);
        }

    }
}
