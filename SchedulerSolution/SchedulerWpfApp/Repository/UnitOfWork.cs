using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using SchedulerWpfApp.Data;
using SchedulerWpfApp.Repository.GroupName;
using SchedulerWpfApp.Repository.LectureSubjectRepo;
using SchedulerWpfApp.Repository.RoomRepo;

namespace SchedulerWpfApp.Repository
{
    /// <summary>
    /// Implementation of the Unit of Work pattern that manages database transactions and repositories
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        #region Fields
        private readonly IServiceProvider _serviceProvider;
        private readonly DataContext _context;
        private IDbContextTransaction? _transaction;
        private readonly Dictionary<Type, object> _repositories = new();

        /// <summary>
        /// Gets the room repository instance
        /// </summary>
        public IRoomRepository RoomRepository { get; }
        // cmt dòng này lại
        public IGroupNameRepository GroupNameRepository { get; }
        public ILectureSubjectRepository LectureSubjectRepository { get; } 

        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the UnitOfWork class
        /// </summary>
        /// <param name="context">The database context</param>
        /// <param name="roomRepository">The room repository implementation</param>
        public UnitOfWork(DataContext context, IRoomRepository roomRepository, IGroupNameRepository groupNameRepository, ILectureSubjectRepository lectureSubjectRepository, IServiceProvider serviceProvider)
        {
            _context = context;
            RoomRepository = roomRepository;
            _serviceProvider = serviceProvider;
             // cmt dòng này lại
            GroupNameRepository = groupNameRepository;
            LectureSubjectRepository = lectureSubjectRepository;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Gets or creates a repository for the specified entity type
        /// </summary>
        /// <typeparam name="T">The entity type for the repository</typeparam>
        /// <returns>An implementation of IBaseRepository for the specified entity type</returns>
        public IBaseRepository<T> Repository<T>() where T : class
        {
            var type = typeof(T);
            if (!_repositories.ContainsKey(type))
            {
                // Resolve repository từ DI container
                var repo = _serviceProvider.GetRequiredService<IBaseRepository<T>>();
                _repositories.Add(type, repo);
            }
            return (IBaseRepository<T>)_repositories[type];
        }

        /// <summary>
        /// Begins a new database transaction if one doesn't already exist
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task BeginTransactionAsync()
        {
            _transaction ??= await _context.Database.BeginTransactionAsync();
        }

        /// <summary>
        /// Commits changes to the database and the active transaction
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
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

        /// <summary>
        /// Rolls back the active transaction
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task RollbackAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        /// <summary>
        /// Saves all changes made in this context to the database
        /// </summary>
        /// <returns>The number of state entries written to the database</returns>
        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Disposes the current transaction and context
        /// </summary>
        /// <returns>A task representing the asynchronous dispose operation</returns>
        public async ValueTask DisposeAsync()
        {
            if (_transaction != null)
                await _transaction.DisposeAsync();

            await _context.DisposeAsync();
        }
        #endregion
    }
}
