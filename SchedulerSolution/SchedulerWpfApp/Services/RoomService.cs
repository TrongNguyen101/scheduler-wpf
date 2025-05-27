using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SchedulerWpfApp.Data;
using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Services
{
    public class RoomService : IRoomService
    {
        private readonly DataContext _context;
        public RoomService(DataContext context)
        {
            _context = context;
        }
        public async Task ImportPersonFromExcel(List<Room> listRoomFromExcel)
        {
            foreach (var room in listRoomFromExcel)
            {
                _context.Rooms.Add(room);
            }
            await _context.SaveChangesAsync();
        }
        public async Task<List<Room>> GetAllAsync()
        {
            return await _context.Rooms.ToListAsync();
        }


        public async Task<Room?> GetByIdAsync(int id)
        {
            return await _context.Rooms.FindAsync(id);
        }
        public async Task AddRoom(Room room)
        {
            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();
        }
        public async Task UpdateRoom(Room room)
        {
            _context.Rooms.Update(room);
            await _context.SaveChangesAsync();
        }
        public async Task DeleteRoom(int id)
        {
            var room = await GetByIdAsync(id);
            if (room != null)
            {
                _context.Rooms.Remove(room);
                await _context.SaveChangesAsync();
            }
        }
        public async Task<List<Room>> SearchRoomsAsync(string searchTerm)
        {
            return await _context.Rooms
                .Where(r => r.RoomName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                .ToListAsync();
        }
        public async Task<bool> CheckRoomIdExistsAsync(string roomname)
        {
            return await _context.Rooms.AnyAsync(r => r.RoomName == roomname);
        }

    }
}
