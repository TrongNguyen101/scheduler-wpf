using System.IO;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Syncfusion.Licensing;

namespace SchedulerWpfApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// Main application class that handles startup configuration including Syncfusion license registration.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Application startup event handler.
        /// </summary>
        /// <param name="e"></param>
        /// <exception cref="FileNotFoundException"></exception>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            try
            {
                // Get the executing assembly's directory
                string binDirectory = AppDomain.CurrentDomain.BaseDirectory;

                // First attempt: Try to find appsettings.json in project root (3 levels up from bin)
                // This path works when running from the IDE
                string baseDirectory = Path.GetFullPath(Path.Combine(binDirectory, @"..\\..\\..\\"));
                string configFilePath = Path.Combine(baseDirectory, "appsettings.json");

                if (!File.Exists(configFilePath))
                {
                    // Second attempt: Try an alternative path (2 levels up)
                    // This path typically works when running from published/deployed location
                    baseDirectory = Path.GetFullPath(Path.Combine(binDirectory, "@..\\.."));
                    configFilePath = Path.Combine(baseDirectory, "appsettings.json");

                    if (!File.Exists(configFilePath))
                    {
                        // If config file is not found in either location, throw an exception
                        throw new FileNotFoundException("appsettings.json file not found.");
                    }
                }

                // Load configuration from appsettings.json
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(baseDirectory)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .Build();

                // Get the Syncfusion license key from configuration
                string syncfusionLicenceKey = configuration["Syncfusion:LicenceKey"];

                if (!string.IsNullOrEmpty(syncfusionLicenceKey))
                {
                    // Register the Syncfusion license if key is found
                    SyncfusionLicenseProvider.RegisterLicense(syncfusionLicenceKey);
                }
                else
                {
                    // Show error if license key is missing from configuration
                    MessageBox.Show("Syncfusion license key is not configured.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                // Handle and display any errors that occur during startup
                MessageBox.Show($"Error registering Syncfusion license: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

