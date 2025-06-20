using Microsoft.EntityFrameworkCore;
using SchedulerWpfApp.Data;
using SchedulerWpfApp.Model;
using SchedulerWpfApp.Repository;

namespace SchedulerWpfApp.Services.LecturerSubjectServices
{
    public class ImplementLecturerServices : InterfaceLecturerServices
    {
        #region Fields
        private readonly DataContext _context;
        private IUnitOfWork _unitOfWork;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the ImplementLecturerServices class
        /// </summary>
        /// <param name="context">The database context used for data operations</param>
        public ImplementLecturerServices(DataContext context, IUnitOfWork unitOfWork)
        {
            _context = context;
            _unitOfWork = unitOfWork;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Adds a new lecture to the database
        /// </summary>
        /// <param name="lecture">The person entity to add</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task AddLecture(Lecturer lecture)
        {
            await _unitOfWork.BeginTransactionAsync();
            await _unitOfWork.Repository<Lecturer>().AddAsync(lecture);
            await _unitOfWork.CommitAsync();
        }

        public async Task ImportLectureFromExcel(List<Lecturer> listLectureFromExcel)
        {
            foreach (var lecture in listLectureFromExcel)
            {
                _context.Lecturers.Add(lecture);
            }
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Deletes a subject from the database by their ID
        /// </summary>
        /// <param name="LecturerId">The ID of the lecture to delete</param>
        /// <returns>A task representing the asynchronous operation</returns>
        /// <remarks>If no lecture with the specified ID exists, no action is taken</remarks>
        public async Task DeleteLecture(string LecturerId)
        {
            var existingLecture = await _unitOfWork.LecturerRepository.GetByLecturerCodeAsync(LecturerId);
            if (existingLecture != null)
            {
                await _unitOfWork.LecturerRepository.DeleteLecturer(LecturerId);
                await _unitOfWork.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Retrieves a specific lecture by their ID
        /// </summary>
        /// <param name="LecturerId">The ID of the lecture to retrieve</param>
        /// <returns>The lecture with the specified ID, or null if not found</returns>
        public async Task<Lecturer?> GetByLectureCodeAsync(string LecturerId)
        {
            return await _unitOfWork.LecturerRepository.GetByLecturerCodeAsync(LecturerId);
        }

        /// <summary>
        /// Retrieves all lectures from the database
        /// </summary>
        /// <returns>A list of all lectures in the database</returns>
        public async Task<List<Lecturer>> GetAllLecturerAsync()
        {
            return await _unitOfWork.Repository<Lecturer>().GetAllAsync();
        }

        /// <summary>
        /// Updates an existing lecture in the database
        /// </summary>
        /// <param name="lecture">The lecture entity with updated values</param>
        /// <returns>A task representing the asynchronous operation</returns>
        /// <remarks>If no lecture with the specified ID exists, no action is taken</remarks>
        public async Task UpdateLecture(Lecturer lecture)
        {
            var existingLecture = await _unitOfWork.LecturerRepository.GetByLecturerCodeAsync(lecture.LecturerId);
            if (existingLecture != null)
            {
                existingLecture.LecturerName = lecture.LecturerName;
                existingLecture.Role = lecture.Role;
                existingLecture.Department = lecture.Department;

                await _unitOfWork.BeginTransactionAsync();
                await _unitOfWork.Repository<Lecturer>().UpdateAsync(existingLecture);
                await _unitOfWork.CommitAsync();
            }
        }
        #endregion
    }
}
