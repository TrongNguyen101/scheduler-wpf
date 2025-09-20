using System;
using System.Windows.Controls;
using System.Windows;
using SchedulerWpfApp.ViewModel;

namespace SchedulerWpfApp.Views
{
    /// <summary>
    /// Interaction logic for CreateScheduleView.xaml
    /// </summary>
    public partial class CreateScheduleView : UserControl
    {
        public CreateScheduleView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Event handler for backup and restore button click
        /// Opens the BackupRestoreWindow
        /// </summary>
        private void BackupRestoreButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                var backupRestoreWindow = new BackupRestoreWindow();
                backupRestoreWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi mở cửa sổ sao lưu: {ex.Message}");
            }
        }
    }
}
