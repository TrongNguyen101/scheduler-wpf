using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Repository.GroupNameRepository
{
    public interface IGroupNameRepository : IBaseRepository<GroupClass>
    {
        Task<List<GroupClass>> SearchGroupNameAsync(string searchTerm);
        Task<bool> CheckGroupNameExistsAsync(string groupName);
        Task DeleteAsync(string code);
        Task<GroupClass> GetGroupNameAsync(string groupname);

        Task<List<string>> GetAllMajorAsync();
    }
}

