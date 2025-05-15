using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SchedulerWpfApp.Data;
using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Services
{
    public class CourseService : ICourseService
    {
        #region Fields
        private readonly DataContext _context;
        #endregion

        #region Contructor
        /// <summary>
        /// Initializes a new instance of the PersonService class
        /// </summary>
        /// <param name="context">The database context used for data operations</param>
        public CourseService(DataContext context)
        {
            _context = context;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Adds a new person to the database
        /// </summary>
        /// <param name="person">The person entity to add</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task AddSubject(Subject subject)
        {
            _context.Subjects.Add(subject);
            await _context.SaveChangesAsync();
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
        /// Deletes a person from the database by their ID
        /// </summary>
        /// <param name="id">The ID of the person to delete</param>
        /// <returns>A task representing the asynchronous operation</returns>
        /// <remarks>If no person with the specified ID exists, no action is taken</remarks>
        public async Task DeleteSubject(string subjectCode)
        {
            var existingSubject = await GetBySubjectCodeAsync(subjectCode);
            if (existingSubject != null)
            {
                _context.Subjects.Remove(existingSubject);
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Retrieves all persons from the database
        /// </summary>
        /// <returns>A list of all persons in the database</returns>
        public async Task<List<Subject>> GetAllAsync()
        {
            return await _context.Subjects.ToListAsync();
        }

        /// <summary>
        /// Retrieves a specific person by their ID
        /// </summary>
        /// <param name="id">The ID of the person to retrieve</param>
        /// <returns>The person with the specified ID, or null if not found</returns>
        public async Task<Subject?> GetBySubjectCodeAsync(string subjectCode)
        {
            return await _context.Subjects.FindAsync(subjectCode);
        }

        /// <summary>
        /// Updates an existing person in the database
        /// </summary>
        /// <param name="person">The person entity with updated values</param>
        /// <returns>A task representing the asynchronous operation</returns>
        /// <remarks>If no person with the specified ID exists, no action is taken</remarks>
        public async Task UpdateSubject(Subject subject)
        {
            var existingSubject = await GetBySubjectCodeAsync(subject.SubjectCode);
            if (existingSubject != null)
            {
                // Mark the entity as modified to avoid having to copy properties manually
                _context.Entry(subject).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }
        }
        #endregion
    }
}
