namespace SchedulerWpfApp.Repository
{
    public interface IUnitOfWork : IAsyncDisposable
    {
        IRoomRepository RoomRepository { get; }

        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitAsync();
        Task RollbackAsync();
    }
}
