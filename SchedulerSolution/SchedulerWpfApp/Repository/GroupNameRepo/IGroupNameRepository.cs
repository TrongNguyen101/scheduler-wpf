using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Repository.GroupNameRepo
{
    public interface IGroupNameRepository : IBaseRepository<GroupClass>
    {
        Task<List<GroupClass>> SearchRoomsAsync(string searchTerm);
        Task<bool> CheckGroupNameIdExistsAsync(string groupName);
        Task DeleteAsync(string code);
        Task<GroupClass> GetGroupClassByIdAsync(string groupnameid);
    }
}

