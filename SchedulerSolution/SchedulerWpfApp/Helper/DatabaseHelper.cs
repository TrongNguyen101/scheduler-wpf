using System.IO;

namespace SchedulerWpfApp.Helper
{
    public static class DatabaseHelper
    {
        public static void ClearAllData()
        {
            string userDbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SchedulerApp",
                "app.db"
            );

            // Get the application's base directory
            string binDirectory = AppDomain.CurrentDomain.BaseDirectory;
            // Navigate up three directories to the project root
            string baseDirectory = Path.GetFullPath(Path.Combine(binDirectory, @"..\\..\\..\\"));
            // Define the path for storing application data
            string appDataPath = Path.Combine(baseDirectory, "AppData");

            // Fallback logic if the directory structure is different (possibly in production)
            if (!Directory.Exists(appDataPath))
            {
                baseDirectory = Path.GetFullPath(Path.Combine(binDirectory, "@..\\.."));
                Directory.CreateDirectory(appDataPath);
            }

            // Define the database file path
            string dbPath = Path.Combine(appDataPath, "app.db");

            if (File.Exists(userDbPath))
            {
                File.Delete(userDbPath);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(userDbPath));

            if (File.Exists(dbPath))
            {
                File.Copy(dbPath, userDbPath);
            }
            else
            {
                throw new FileNotFoundException("Không tìm thấy file database gốc trong thư mục dự án.");
            }
        }
    }
}
