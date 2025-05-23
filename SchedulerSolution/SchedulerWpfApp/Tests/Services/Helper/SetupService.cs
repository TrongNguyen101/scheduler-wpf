
using Microsoft.EntityFrameworkCore;
using SchedulerWpfApp.Data;

namespace SchedulerWpfApp.Tests.Services.Helper
{
    public static class SetupService
    {
        // This method sets up an in-memory database context for unit testing
        public static DataContext CreateSetup()
        {
            // Create database options using an in-memory database with a unique name
            var options = new DbContextOptionsBuilder<DataContext>()
               .UseInMemoryDatabase(Guid.NewGuid().ToString()) // Unique DB instance for each test
               .Options;

            // Instantiate the DataContext with the options
            var _context = new DataContext(options);

            // Return the configured context
            return _context;
        }
    }
}
