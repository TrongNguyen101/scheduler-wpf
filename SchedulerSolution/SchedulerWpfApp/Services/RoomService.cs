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


    }
}
