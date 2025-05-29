using Microsoft.EntityFrameworkCore;
using SchedulerWpfApp.Data;
using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Services
{
    public class LectureSubjectService : ILectureSubjectService
    {
        private readonly DataContext _context;
        public LectureSubjectService(DataContext context)
        {
            _context = context;
        }
        /// <summary>
        /// Retrieves all lecturer subjects from the database asynchronously.
        /// </summary>
        /// <returns></returns>
        public async Task<List<LecturerSubject>> GetAllAsync()
        {
            return await _context.LecturerSubjects.ToListAsync();
        }

        /// <summary>
        /// Imports a list of lecturer subjects from an Excel file into the database.
        /// </summary>
        public async Task ImportLectureSubjectFromExcel(List<LecturerSubject> listlecturesubjectFromExcel)
        {
            foreach (var lecturesubject in listlecturesubjectFromExcel)
            {
                _context.LecturerSubjects.Add(lecturesubject);
            }
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Retrieves a lecturer subject by its ID asynchronously.
        /// </summary>
        public async Task<LecturerSubject> GetByIdAsync(int id)
        {
            return await _context.LecturerSubjects.FindAsync(id);
        }

        /// <summary>
        /// Adds a new lecturer subject to the database asynchronously.
        /// </summary>
        public async Task AddAsync(LecturerSubject lecturerSubject)
        {
            var existingLecturer = await _context.Lecturers.FindAsync(lecturerSubject.LecturerId);
            if (existingLecturer == null)
            {
                throw new Exception("Lecturer not found");
            }
            lecturerSubject.LecturerName = existingLecturer.LecturerName;
            _context.LecturerSubjects.Add(lecturerSubject);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Updates an existing lecturer subject in the database asynchronously.
        /// </summary>
        public async Task UpdateAsync(LecturerSubject lecturerSubject)
        {
            var existingLecturerSubject = await _context.LecturerSubjects.FindAsync(lecturerSubject.Id);
            if (existingLecturerSubject == null)
            {
                throw new Exception("LecturerSubject not found");
            }
            var existinglecture = await _context.Lecturers.FindAsync(lecturerSubject.LecturerId);
            if (existinglecture == null)
            {
                throw new Exception("Lecturer not found");
            }
            existingLecturerSubject.LecturerId = lecturerSubject.LecturerId;
            existingLecturerSubject.SubjectCode = lecturerSubject.SubjectCode;
            existingLecturerSubject.LecturerName = existinglecture.LecturerName;
            existingLecturerSubject.NumberOfClasses = lecturerSubject.NumberOfClasses;
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Deletes a lecturer subject by its ID asynchronously.
        /// </summary>
        public async Task DeleteAsync(int id)
        {
            try
            {
                var lecturerSubject = await _context.LecturerSubjects.FindAsync(id);
                if (lecturerSubject != null)
                {
                    _context.LecturerSubjects.Remove(lecturerSubject);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error deleting LecturerSubject", ex);
            }
        }
    }
}
