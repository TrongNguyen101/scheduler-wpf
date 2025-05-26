using Microsoft.EntityFrameworkCore;
using SchedulerWpfApp.Data;
using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Services.LecturerSubjectServices
{
    public class ImplementLecturerSubjectServices : InterfaceLecturerSubjectServices
    {
        private readonly DataContext _context;


        public ImplementLecturerSubjectServices(DataContext context)
        {
            _context = context;
        }

        public async Task<List<LecturerSubject>> GetAllLecturerSubjectAsync()
        {
            return await _context.LecturerSubjects.ToListAsync();
        }
    }
}
