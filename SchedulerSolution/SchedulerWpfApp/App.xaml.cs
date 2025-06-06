using System.IO;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SchedulerWpfApp.Data;
using SchedulerWpfApp.Algorithm;
using SchedulerWpfApp.Services;
using SchedulerWpfApp.ViewModel;
using SchedulerWpfApp.Views;
using Syncfusion.Licensing;
using Microsoft.Extensions.Logging;
using SchedulerWpfApp.Services.ScheduleServices;
using SchedulerWpfApp.Services.LecturerSubjectServices;
using SchedulerWpfApp.Helper;
namespace SchedulerWpfApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// Main application class that handles startup configuration including Syncfusion license registration.
    /// </summary>
    public partial class App : Application
    {
        #region Fields
        private readonly IHost _host;
        #endregion

        #region Constructor
        /// <summary>
        /// Constructor for the application class.
        /// Initializes the host using the Host Builder pattern.
        /// </summary>
        public App()
        {
            _host = Host.CreateDefaultBuilder() // Creates a default builder with preconfigured defaults
                .ConfigureServices((context, services) => // Configures the service container
                {
                    ConfigureService(services); // Delegates service registration to the ConfigureService method
                })
                .Build(); // Builds the host, finalizing the service container configuration
        }
        #endregion

        #region Methods
        /// <summary>
        /// Application startup event handler.
        /// </summary>
        /// <param name="e"></param>
        /// <exception cref="FileNotFoundException"></exception>
        protected override async void OnStartup(StartupEventArgs e)
        {
            await _host.StartAsync();
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
            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.DataContext = _host.Services.GetRequiredService<MainViewModel>();
            mainWindow.Show();
            base.OnStartup(e);
        }

        /// <summary>
        /// Configures the dependency injection container with required services.
        /// </summary>
        /// <param name="services">The service collection to configure</param>
        private void ConfigureService(IServiceCollection services)
        {
            services.AddLogging(config =>
            {
                config.ClearProviders(); // Delete all existing providers
                config.AddConsole();     // Show log in Console output
                config.AddDebug();       // Show log in Debug output
            });

            // Register the AutoMapper configuration
            services.AddAutoMapper(typeof(AutoMappingProfiles));

            // Register the database context
            services.AddDbContext<DataContext>();

            // Register person service with scoped lifetime (one instance per scope)
            services.AddScoped<IPersonService, PersonService>();
            services.AddScoped<ISubjectServices, SubjectServices>();
            services.AddScoped<IGroupNameService, GroupNameService>();
            services.AddScoped<InterfaceScheduleServices, ImplementScheduleServices>();
            services.AddScoped<InterfaceLecturerServices, ImplementLecturerServices>();
            services.AddScoped<InterfaceLecturerSubjectServices, ImplementLecturerSubjectServices>();

            services.AddScoped<IExcelPersonImporter, ExcelPersonImporter>();
            services.AddScoped<IExcelPersonExporter, ExcelPersonExporter>();

            services.AddScoped<IExcelSubjectImporter, ExcelSubjectImporter>();
            services.AddScoped<IExcelSubjectExporter, ExcelSubjectExporter>();

            services.AddScoped<IExcelLectureExporter, ExcelLectureExporter>();
            services.AddScoped<IExcelLectureImporter, ExcelLectureImporter>();
            services.AddScoped<IRoomService, RoomService>();
            services.AddScoped<IExcelRoomExporter, ExcelRoomExporter>();
            services.AddScoped<IExcelRoomImport, ExcelRoomImport>();
            services.AddScoped<ILectureSubjectService, LectureSubjectService>();
            services.AddScoped<IExcelLectureSubjectImporter, ExcelLectureSubjectImporter>();
            services.AddScoped<IExcelLectureSubjectExporter, ExcelLectureSubjectExporter>();
            // Register the main window as singleton (single instance for the application)
            services.AddSingleton<MainWindow>();

            services.AddScoped<MainViewModel>();

            // Register the add person window as transient (new instance created each time)
            services.AddTransient<AddPersonWindow>();

            // Register the add person window as transient (new instance created each time)
            services.AddTransient<PersonViewModel>();

            services.AddTransient<SubjectViewModel>();
            services.AddTransient<LectureView>();
            services.AddTransient<GroupNameViewModel>();
            services.AddTransient<CreateScheduleViewModel>();
            services.AddTransient<LectureViewModel>();
            services.AddTransient<RoomViewModel>();
            services.AddTransient<LectureSubjectViewModel>();

            // Add factories
            services.AddSingleton<Func<SubjectViewModel>>(sp => () => sp.GetRequiredService<SubjectViewModel>());
            services.AddSingleton<Func<PersonViewModel>>(sp => () => sp.GetRequiredService<PersonViewModel>());
            services.AddSingleton<Func<GroupNameViewModel>>(sp => () => sp.GetRequiredService<GroupNameViewModel>());
            services.AddSingleton<Func<CreateScheduleViewModel>>(sp => () => sp.GetRequiredService<CreateScheduleViewModel>());
            services.AddSingleton<Func<LectureViewModel>>(sp => () => sp.GetRequiredService<LectureViewModel>());
            services.AddSingleton<Func<LectureSubjectViewModel>>(sp => () => sp.GetRequiredService<LectureSubjectViewModel>());
            services.AddSingleton<Func<RoomViewModel>>(sp => () => sp.GetRequiredService<RoomViewModel>());

            services.AddScoped<CreateScheduleTree>(); // Register ScheduleTreeDAO with a scoped lifetime
            services.AddScoped<TreeForSchedule>(); // Register TreeNode with a scoped lifetime
            services.AddScoped<SortSubjectsOneSession>(); // Register SortSubjectsOneSession with a scoped lifetime
            services.AddScoped<GetLecturerForSubject>(); // Register GetLecturerForSubject with a scoped lifetime
            services.AddScoped<GenerateScheduleForAllDate>(); // Register GenerateScheduleForAllDate with a scoped lifetime
            services.AddScoped<CreateSlotTypeCode>(); // Register CreateSlotTypeCode with a scoped lifetime
        }

        /// <summary>
        /// Application exit event handler.
        /// Properly disposes of the host and stops all hosted services.
        /// </summary>
        /// <param name="e">Exit event arguments</param>
        protected override async void OnExit(ExitEventArgs e)
        {
            using (_host)
            {
                // Stop all hosted services gracefully
                await _host.StopAsync();
                // Release all resources held by the host
                _host.Dispose();
            }
            base.OnExit(e);
        }
        #endregion
    }
}

