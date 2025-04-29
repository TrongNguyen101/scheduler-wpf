using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Services
{
    public interface IPersonService
    {
        Task<List<Person>> GetAllAsync();
        Task<Person?> GetByIdAsync(int id);
        Task AddPerson(Person person);
        Task UpdatePerson(Person person);
        Task DeletePerson(int id);
    }
}
