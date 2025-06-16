using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Repository.GroupNameRepo
{
    public interface IGroupNameRepository : IBaseRepository<GroupClass>
    {
        Task<List<GroupClass>> SearchRoomsAsync(string searchTerm);
        Task<bool> CheckGroupNameExistsAsync(string groupName);
        Task DeleteAsync(string code);
        Task<GroupClass> GetGroupNameAsync(string groupname);
    }
}

