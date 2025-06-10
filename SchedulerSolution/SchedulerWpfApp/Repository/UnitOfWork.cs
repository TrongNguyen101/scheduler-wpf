using Microsoft.EntityFrameworkCore.Storage;
using SchedulerWpfApp.Data;

namespace SchedulerWpfApp.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly DataContext _context;
        private IDbContextTransaction? _transaction;
        private readonly Dictionary<Type, object> _repositories = new();

        public IRoomRepository RoomRepository { get; }

        public UnitOfWork(DataContext context, IRoomRepository roomRepository)
        {
            _context = context;
            RoomRepository = roomRepository;
        }

        public IBaseRepository<T> Repository<T>() where T : class
        {
            var type = typeof(T);
            if (!_repositories.ContainsKey(type))
            {
                var repo = new BaseRepository<T>(_context);
                _repositories.Add(type, repo);
            }
            return (IBaseRepository<T>)_repositories[type];
        }

        public async Task BeginTransactionAsync()
        {
            _transaction ??= await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitAsync()
        {
            try
            {
                await _context.SaveChangesAsync();
                if (_transaction != null)
                    await _transaction.CommitAsync();
            }
            catch
            {
                await RollbackAsync();
                throw;
            }
            finally
            {
                if (_transaction != null)
                    await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async ValueTask DisposeAsync()
        {
            if (_transaction != null)
                await _transaction.DisposeAsync();

            await _context.DisposeAsync();
        }
    }
}
