namespace SchedulerWpfApp.Repository
{
    public interface IUnitOfWork : IAsyncDisposable
    {
        IBaseRepository<T> Repository<T>() where T : class;
        IRoomRepository RoomRepository { get; }

        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitAsync();
        Task RollbackAsync();
    }
}
