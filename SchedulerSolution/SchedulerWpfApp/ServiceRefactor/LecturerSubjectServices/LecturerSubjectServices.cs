using SchedulerWpfApp.Model;
using SchedulerWpfApp.Repository;

namespace SchedulerWpfApp.ServiceRefactor.LecturerSubjectServices
{
    public class LecturerSubjectServices : ILecturerSubjectServices
    {
        private readonly IUnitOfWork _unitOfWork;
        public LecturerSubjectServices(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<List<LecturerSubject>> GetAllAsync()
        {
            return await _unitOfWork.Repository<LecturerSubject>().GetAllAsync();
        }

        /// <summary>
        /// Imports a list of lecturer subjects from an Excel file into the database.
        /// </summary>
        //public async Task ImportLectureSubjectFromExcel(List<LecturerSubject> listlecturesubjectFromExcel)
        //{
        //    foreach (var lecturesubject in listlecturesubjectFromExcel)
        //    {
        //        _context.LecturerSubjects.Add(lecturesubject);
        //    }
        //    await _context.SaveChangesAsync();
        //}

        /// <summary>
        /// Retrieves a lecturer subject by its ID asynchronously.
        /// </summary>
        public async Task<LecturerSubject> GetByIdAsync(int id)
        {
            return await _unitOfWork.Repository<LecturerSubject>().GetByIdAsync(id);
        }

        /// <summary>
        /// Adds a new lecturer subject to the database asynchronously.
        /// </summary>
        public async Task AddAsync(LecturerSubject lecturerSubject)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _unitOfWork.Repository<LecturerSubject>().AddAsync(lecturerSubject);
                await _unitOfWork.CommitAsync();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Updates an existing lecturer subject in the database asynchronously.
        /// </summary>
        public async Task UpdateAsync(LecturerSubject lecturerSubject)
        {
            var existingLecturerSubject = await _unitOfWork.LectureSubjectRepository.GetLectureSubjectByCodeAsync(lecturerSubject.Id);
            //if (existingLecturerSubject == null)
            //{
            //    throw new Exception("LecturerSubject not found");
            //}
            //var existinglecture = await _context.Lecturers.FindAsync(lecturerSubject.LecturerId);
            //if (existinglecture == null)
            //{
            //    throw new Exception("Lecturer not found");
            //}
            //existingLecturerSubject.LecturerId = lecturerSubject.LecturerId;
            //existingLecturerSubject.SubjectCode = lecturerSubject.SubjectCode;
            //existingLecturerSubject.LecturerName = existinglecture.LecturerName;
            //existingLecturerSubject.NumberOfClasses = lecturerSubject.NumberOfClasses;
            //await _context.SaveChangesAsync();
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _unitOfWork.Repository<LecturerSubject>().UpdateAsync(existingLecturerSubject);
                await _unitOfWork.CommitAsync();
            }
            catch (Exception ex)
            {
            }
        }

        /// <summary>
        /// Deletes a lecturer subject by its ID asynchronously.
        /// </summary>
        public async Task DeleteAsync(int id)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _unitOfWork.Repository<LecturerSubject>().DeleteAsync(id);
                await _unitOfWork.CommitAsync();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }
    }
}
