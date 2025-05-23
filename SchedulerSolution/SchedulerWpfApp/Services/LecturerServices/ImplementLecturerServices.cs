using Microsoft.EntityFrameworkCore;
using SchedulerWpfApp.Data;
using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Services.LecturerSubjectServices
{
    public class ImplementLecturerServices : InterfaceLecturerServices
    {
        private readonly DataContext _context;


        public ImplementLecturerServices(DataContext context)
        {
            _context = context;
        }

        public async Task<List<Lecturer>> GetAllLecturerAsync()
        {
            return await _context.Lecturers.ToListAsync();
        }
    }
}
