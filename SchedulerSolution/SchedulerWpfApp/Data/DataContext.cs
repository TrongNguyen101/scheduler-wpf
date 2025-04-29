using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Data
{
    public class DataContext : DbContext
    {
        public DataContext()
        {
        }
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
        }
        public DbSet<Person> Persons { get; set; } = null!;
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            try
            {
                if(!optionsBuilder.IsConfigured)
                {
                    string binDirectory = AppDomain.CurrentDomain.BaseDirectory; ;
                    string baseDirectory = Path.GetFullPath(Path.Combine(binDirectory, @"..\\..\\..\\"));
                    string appDataPath = Path.Combine(baseDirectory, "AppData");
                    if(!Directory.Exists(appDataPath))
                    {
                        baseDirectory = Path.GetFullPath(Path.Combine(binDirectory, "@..\\.."));
                        Directory.CreateDirectory(appDataPath);
                    }
                    string dbPath = Path.Combine(appDataPath, "app.db");
                    optionsBuilder.UseSqlite($"Data Source={dbPath}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error configuring database connection", ex);
            }
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
        }
    }
}
