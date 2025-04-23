using System.Configuration;
using System.Data;
using System.Windows;
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
            //Synnfusion lincense key
            SyncfusionLicenseProvider.RegisterLicense("MzgyODQ1MEAzMjM5MmUzMDJlMzAzYjMyMzkzYklZclFGTjJ4eVUzc3BWT3hmd1pOL3hiMmd1UDdFUmZNUXhVd1lnYU13WG89");
            base.OnStartup(e);
            // Set the default theme for the application
            // This is where you can set the theme for your application
            // For example, you can set it to "Light" or "Dark"
            // Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/YourTheme.xaml") });
        }
    }
}

