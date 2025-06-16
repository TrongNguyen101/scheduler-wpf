using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Repository.GroupNameRepository
{
    public interface IGroupNameRepository : IBaseRepository<GroupClass>
    {
        Task<List<GroupClass>> SearchRoomsAsync(string searchTerm);
        Task<bool> CheckRoomIdExistsAsync(string groupName);
        Task DeleteAsync(string code);
        Task<GroupClass> GetGroupClassByCodeAsync(string groupnameid);
    }
}

