using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using SchedulerWpfApp.Data;
using SchedulerWpfApp.Model;
using SchedulerWpfApp.Services;
using SchedulerWpfApp.Tests.Services.Helper;

namespace SchedulerWpfApp.Tests.Services
{
    [TestFixture]
    public class PersonServiceTest
    {
        // In-memory DataContext for testing
        private DataContext _dataContext;
        private PersonService _personService;

        [SetUp]
        public void Setup()
        {
            // Create a test database context using a helper method
            _dataContext = SetupService.CreateSetup();

            // Initialize the service with the test context
            _personService = new PersonService(_dataContext);
        }

        [Test]
        public async Task AddPerson_Should_AddPersonToDatabase()
        {
            // Arrange - create a new person object
            var person = new Person { Email = "test@gmail.com", FirstName = "Math", LastName = "SE", Phone = "0123456789" };

            // Act - call the service to add the person
            await _personService.AddPerson(person);

            // Assert - check if the person was successfully added to the database
            var result = await _dataContext.Persons.FirstOrDefaultAsync(p => p.Email == person.Email);
            Assert.NotNull(result);
            Assert.AreEqual("test@gmail.com", result.Email);
        }

    }
}
