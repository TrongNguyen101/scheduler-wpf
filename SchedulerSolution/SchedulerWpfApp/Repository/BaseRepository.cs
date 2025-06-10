using Microsoft.EntityFrameworkCore;
using SchedulerWpfApp.Data;

namespace SchedulerWpfApp.Repository
{
    /// <summary>
    /// Base generic repository that provides common data access operations for entities.
    /// Implements the <see cref="IBaseRepository{T}"/> interface.
    /// </summary>
    /// <typeparam name="T">The entity type this repository works with.</typeparam>
    public class BaseRepository<T> : IBaseRepository<T> where T : class
    {
        #region Fields
        /// <summary>
        /// The database context used to perform operations.
        /// </summary>
        protected readonly DataContext _context;

        /// <summary>
        /// The DbSet representing the entity collection in the database.
        /// </summary>
        protected readonly DbSet<T> _dbSet;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseRepository{T}"/> class.
        /// </summary>
        /// <param name="context">The database context to use for operations.</param>
        public BaseRepository(DataContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }
        #endregion

        #region Methods
        /// <summary>
        /// Retrieves all entities of type <typeparamref name="T"/> from the database.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of all entities.</returns>
        public virtual async Task<List<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        /// <summary>
        /// Retrieves an entity of type <typeparamref name="T"/> with the specified ID.
        /// </summary>
        /// <param name="id">The ID of the entity to retrieve.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the entity found, or null if no entity was found.</returns>
        public virtual async Task<T?> GetByIdAsync(int id)
        {
            return await _dbSet.FindAsync(id);
        }

        /// <summary>
        /// Adds a new entity to the database context.
        /// </summary>
        /// <param name="entity">The entity to add.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public virtual async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
        }

        /// <summary>
        /// Updates an existing entity in the database context.
        /// </summary>
        /// <param name="entity">The entity to update.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public virtual async Task UpdateAsync(T entity)
        {
            _dbSet.Update(entity);
        }

        /// <summary>
        /// Deletes an entity with the specified ID from the database context.
        /// </summary>
        /// <param name="id">The ID of the entity to delete.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public virtual async Task DeleteAsync(int id)
        {
            var entity = await _dbSet.FindAsync(id);
            if (entity != null)
                _dbSet.Remove(entity);
        }
    }
    #endregion
}
