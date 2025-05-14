using Microsoft.EntityFrameworkCore;
using SchedulerWpfApp.Data;
using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Services
{
    /// <summary>
    /// Service for managing Person entities in the database
    /// </summary>
    public class PersonService : IPersonService
    {
        #region Fields
        private readonly DataContext _context;
        #endregion

        #region Contructor
        /// <summary>
        /// Initializes a new instance of the PersonService class
        /// </summary>
        /// <param name="context">The database context used for data operations</param>
        public PersonService(DataContext context)
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
        public async Task AddPerson(Person person)
        {
            _context.Persons.Add(person);
            await _context.SaveChangesAsync();
        }

        public async Task ImportPersonFromExcel(List<Person> listPersonFromExcel)
        {
            foreach (var person in listPersonFromExcel)
            {
                _context.Persons.Add(person);
            }
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Deletes a person from the database by their ID
        /// </summary>
        /// <param name="id">The ID of the person to delete</param>
        /// <returns>A task representing the asynchronous operation</returns>
        /// <remarks>If no person with the specified ID exists, no action is taken</remarks>
        public async Task DeletePerson(int id)
        {
            var existingPerson = await GetByIdAsync(id);
            if (existingPerson != null)
            {
                _context.Persons.Remove(existingPerson);
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Retrieves all persons from the database
        /// </summary>
        /// <returns>A list of all persons in the database</returns>
        public async Task<List<Person>> GetAllAsync()
        {
            return await _context.Persons.ToListAsync();
        }

        /// <summary>
        /// Retrieves a specific person by their ID
        /// </summary>
        /// <param name="id">The ID of the person to retrieve</param>
        /// <returns>The person with the specified ID, or null if not found</returns>
        public async Task<Person?> GetByIdAsync(int id)
        {
            return await _context.Persons.FindAsync(id);
        }

        /// <summary>
        /// Updates an existing person in the database
        /// </summary>
        /// <param name="person">The person entity with updated values</param>
        /// <returns>A task representing the asynchronous operation</returns>
        /// <remarks>If no person with the specified ID exists, no action is taken</remarks>
        public async Task UpdatePerson(Person person)
        {
            var existingPerson = await GetByIdAsync(person.Id);
            if (existingPerson != null)
            {
                // Mark the entity as modified to avoid having to copy properties manually
                _context.Entry(person).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }
        }
        #endregion
    }
}
