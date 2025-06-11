using SchedulerWpfApp.Repository.GroupName;
using SchedulerWpfApp.Repository.RoomRepo;

namespace SchedulerWpfApp.Repository
{
    public interface IUnitOfWork : IAsyncDisposable
    {
        IBaseRepository<T> Repository<T>() where T : class;
        IRoomRepository RoomRepository { get; }
         // cmt dòng này lại
       IGroupNameRepository GroupNameRepository { get; }

        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitAsync();
        Task RollbackAsync();
    }
}
