using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Services
{
    public class PersonService : IPersonService
    {
        public Task AddPerson(Person person)
        {
            throw new NotImplementedException();
        }
        public Task DeletePerson(int id)
        {
            throw new NotImplementedException();
        }
        public Task<List<Person>> GetAllAsync()
        {
            throw new NotImplementedException();
        }
        public Task<Person?> GetByIdAsync(int id)
        {
            throw new NotImplementedException();
        }
        public Task UpdatePerson(Person person)
        {
            throw new NotImplementedException();
        }
    }
}
