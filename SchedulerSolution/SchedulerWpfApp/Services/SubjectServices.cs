using AutoMapper;
using SchedulerWpfApp.Data;
using SchedulerWpfApp.Model;
using SchedulerWpfApp.Repository;

namespace SchedulerWpfApp.Services
{
    /// <summary>
    /// Service for managing Subject entities in the database
    /// </summary>
    public class SubjectServices : ISubjectServices
    {
        #region Fields
        private readonly IUnitOfWork _unitOfWork;
        private readonly DataContext _context;
        private readonly IMapper _mapper;
        #endregion

        #region Contructor
        /// <summary>
        /// Initializes a new instance of the PersonService class
        /// </summary>
        /// <param name="context">The database context used for data operations</param>
        public SubjectServices(DataContext context, IMapper mapper, IUnitOfWork unitOfWork)
        {
            _context = context;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Adds a new subject to the database
        /// </summary>
        /// <param name="subject">The person entity to add</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task AddSubject(Subject subject)
        {
            await _unitOfWork.BeginTransactionAsync();
            await _unitOfWork.Repository<Subject>().AddAsync(subject);
            await _unitOfWork.CommitAsync();
        }

        public async Task ImportSubjectFromExcel(List<Subject> listSubjectFromExcel)
        {
            foreach (var subject in listSubjectFromExcel)
            {
                _context.Subjects.Add(subject);
            }
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Deletes a subject from the database by their ID
        /// </summary>
        /// <param name="id">The ID of the subject to delete</param>
        /// <returns>A task representing the asynchronous operation</returns>
        /// <remarks>If no subject with the specified ID exists, no action is taken</remarks>
        public async Task DeleteSubject(string subjectCode)
        {
            var existingSubject = await GetBySubjectCodeAsync(subjectCode);
            if (existingSubject != null)
            {
                await _unitOfWork.SubjectRepository.DeleteAsync(subjectCode);
                await _unitOfWork.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Retrieves all subjects from the database
        /// </summary>
        /// <returns>A list of all subjects in the database</returns>
        public async Task<List<Subject>> GetAllAsync()
        {
            return await _unitOfWork.Repository<Subject>().GetAllAsync();
        }

        /// <summary>
        /// Retrieves a specific subject by their ID
        /// </summary>
        /// <param name="id">The ID of the subject to retrieve</param>
        /// <returns>The subject with the specified ID, or null if not found</returns>
        public async Task<Subject?> GetBySubjectCodeAsync(string subjectCode)
        {
            return await _unitOfWork.SubjectRepository.GetSubjectByCodeAsync(subjectCode);
        }

        /// <summary>
        /// Updates an existing subject in the database
        /// </summary>
        /// <param name="subject">The subject entity with updated values</param>
        /// <returns>A task representing the asynchronous operation</returns>
        /// <remarks>If no subject with the specified ID exists, no action is taken</remarks>
        public async Task UpdateSubject(Subject subject)
        {
            var existingSubject = await GetBySubjectCodeAsync(subject.SubjectCode);
            if (existingSubject != null)
            {
                existingSubject.SubjectNameEnglish = subject.SubjectNameEnglish;
                existingSubject.SubjectNameVietnamese = subject.SubjectNameVietnamese;
                existingSubject.TotalCredits = subject.TotalCredits;
                existingSubject.TotalTime = subject.TotalTime;
                //_mapper.Map(subject, existingSubject);

                await _unitOfWork.BeginTransactionAsync();
                await _unitOfWork.Repository<Subject>().UpdateAsync(existingSubject);
                await _unitOfWork.CommitAsync();
            }
        }
        #endregion
    }
}
