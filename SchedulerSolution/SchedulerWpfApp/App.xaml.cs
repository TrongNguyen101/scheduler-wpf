using System.IO;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Syncfusion.Licensing;

namespace SchedulerWpfApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            try
            {
                string binDirectory = AppDomain.CurrentDomain.BaseDirectory; ;
                string baseDirectory = Path.GetFullPath(Path.Combine(binDirectory, @"..\\..\\..\\"));
                string configFilePath = Path.Combine(baseDirectory, "appsettings.json");
                if (!File.Exists(configFilePath))
                {
                    baseDirectory = Path.GetFullPath(Path.Combine(binDirectory, "@..\\.."));
                    configFilePath = Path.Combine(baseDirectory, "appsettings.json");
                    if(!File.Exists(configFilePath))
                    {
                        throw new FileNotFoundException("appsettings.json file not found.");
                    }
                }
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(baseDirectory)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .Build();
                string syncfusionLicenceKey = configuration["Syncfusion:LicenceKey"];
                if (!string.IsNullOrEmpty(syncfusionLicenceKey))
                {
                    SyncfusionLicenseProvider.RegisterLicense(syncfusionLicenceKey);
                }
                else
                {
                    MessageBox.Show("Syncfusion license key is not configured.", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error registering Syncfusion license: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

