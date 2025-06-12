using SchedulerWpfApp.Data;
using Microsoft.EntityFrameworkCore;
using SchedulerWpfApp.Model;
namespace SchedulerWpfApp.Repository.GroupName
{
    public class GroupNameRepository : BaseRepository<GroupClass>, IGroupNameRepository
    {
        public GroupNameRepository(DataContext context) : base(context) { }
        public async Task<List<GroupClass>> GetNumberOfGroupClass(string nameOfGroupClass)
        {
           throw new NotImplementedException();
        }
        public async Task<List<GroupClass>> SearchRoomsAsync(string searchTerm)
        {
            try
            {
                return await _context.GroupName
                    .Where(r => r.GroupName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while searching for GroupName.", ex);
            }
        }
        public async Task<bool> CheckRoomIdExistsAsync(string groupName)
        {
            try
            {
                var room = await _context.GroupName
                    .FirstOrDefaultAsync(r => r.GroupName.Equals(groupName, StringComparison.OrdinalIgnoreCase));
                if (room == null)
                {
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while checking GroupName ID existence.", ex);
            }
        }
        public async Task<GroupClass?> GetGroupClassByCodeAsync(string groupnameid)
        {
            return await _context.GroupName.FirstOrDefaultAsync(s => s.GroupName == groupnameid);
        }
        public async Task DeleteAsync(string code)
        {
            var groupClass = await GetGroupClassByCodeAsync(code);
            if (groupClass != null)
            {
                _context.GroupName.Remove(groupClass);
                await _context.SaveChangesAsync();
            }
        }
    }
}
