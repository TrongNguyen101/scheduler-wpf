using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Microsoft.Extensions.DependencyInjection;
using SchedulerWpfApp.ServiceRefactor.BackupRestoreService;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System;
using IOPath = System.IO.Path;
using System.IO;

namespace SchedulerWpfApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = this.WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        /// <summary>
        /// Test method for local backup functionality - improved version
        /// </summary>
        private async void TestLocalBackup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var serviceProvider = ((App)Application.Current).Services;
                var backupService = serviceProvider.GetRequiredService<IBackupRestoreService>();
                var logger = serviceProvider.GetRequiredService<ILogger<MainWindow>>();

                // Create test backup path
                var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var testBackupPath = IOPath.Combine(documentsPath, $"test_backup_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.sqlite");

                logger.LogInformation("Starting improved test backup to: {BackupPath}", testBackupPath);

                // Test SQLite info first
                var (version, supportsVacuum) = await backupService.GetSqliteInfoAsync();

                var message = $"SQLite Version: {version}\nVACUUM INTO Support: {supportsVacuum}";

                if (supportsVacuum)
                {
                    message += $"\n\nTesting improved backup method to:\n{testBackupPath}";
                    var result = MessageBox.Show(message + "\n\nProceed with backup test?", "SQLite Backup Test", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        // Progress tracking
                        var progress = new Progress<(string message, double percentage)>(p =>
                        {
                            logger.LogInformation("Backup progress: {Message} ({Percentage}%)", p.message, p.percentage);
                        });

                        // Create backup using new improved method
                        bool success = await backupService.CreateLocalBackupAsync(testBackupPath, progress);

                        if (success)
                        {
                            var fileInfo = new FileInfo(testBackupPath);
                            MessageBox.Show(
                                $"✅ Backup created successfully!\n\n" +
                                $"File: {testBackupPath}\n" +
                                $"Size: {fileInfo.Length:N0} bytes\n" +
                                $"Created: {fileInfo.CreationTime:yyyy-MM-dd HH:mm:ss}\n\n" +
                                $"The improved method bypassed connection pool issues!",
                                "Test Backup Success",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show(
                                "❌ Backup creation failed even with improved method.\nCheck logs for details.",
                                "Test Backup Failed",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                        }
                    }
                }
                else
                {
                    MessageBox.Show(
                        message + "\n\n❌ VACUUM INTO is not supported.\nPlease upgrade SQLite to version 3.44.0+",
                        "SQLite Compatibility Issue",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Error during test backup: {ex.Message}\n\nCheck logs for full details.", "Test Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}