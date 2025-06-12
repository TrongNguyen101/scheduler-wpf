using SchedulerWpfApp.Repository;

namespace SchedulerWpfApp.ServiceRefactor
{
    public class BaseService<T> : IBaseService<T> where T : class
    {
        #region Fields
        protected readonly IUnitOfWork _unitOfWork;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the BaseService class
        /// </summary>
        /// <param name="unitOfWork"></param>
        public BaseService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Retrieves all entities of type T from the database.
        /// </summary>
        /// <returns></returns>
        public virtual async Task<List<T>> GetAllAsync()
        {
            return await _unitOfWork.Repository<T>().GetAllAsync();
        }

        /// <summary>
        /// Retrieves an entity of type T by its ID.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public virtual async Task<T?> GetByIdAsync(int id)
        {
            return await _unitOfWork.Repository<T>().GetByIdAsync(id);
        }

        /// <summary>
        /// Adds a new entity of type T to the database.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public virtual async Task AddAsync(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            await _unitOfWork.BeginTransactionAsync();
            await _unitOfWork.Repository<T>().AddAsync(entity);
            await _unitOfWork.CommitAsync();
        }

        /// <summary>
        /// Updates an existing entity of type T in the database.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public virtual async Task UpdateAsync(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            await _unitOfWork.BeginTransactionAsync();
            await _unitOfWork.Repository<T>().UpdateAsync(entity);
            await _unitOfWork.CommitAsync();
        }

        /// <summary>
        /// Deletes an entity of type T from the database by its ID.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public virtual async Task DeleteAsync(int id)
        {
            await _unitOfWork.Repository<T>().DeleteAsync(id);
        }
        #endregion
    }
}
