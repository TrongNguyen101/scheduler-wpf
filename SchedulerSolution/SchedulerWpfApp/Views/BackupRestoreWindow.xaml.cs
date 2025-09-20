using Microsoft.Extensions.DependencyInjection;
using SchedulerWpfApp.ViewModel;
using System;
using System.ComponentModel;
using System.Windows;

namespace SchedulerWpfApp.Views
{
    /// <summary>
    /// Interaction logic for BackupRestoreWindow.xaml
    /// </summary>
    public partial class BackupRestoreWindow : Window
    {
        private BackupRestoreViewModel? _viewModel;

        public BackupRestoreWindow()
        {
            InitializeComponent();

            // Ensure we're on the UI thread and window is loaded before setting up ViewModel
            this.Loaded += OnWindowLoaded;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Get ViewModel from DI container
                _viewModel = ((App)Application.Current).Services.GetRequiredService<BackupRestoreViewModel>();
                DataContext = _viewModel;

                // Subscribe to ViewModel events
                _viewModel.RequestClose += OnViewModelRequestClose;

                // Handle window closing to ensure proper cleanup
                Closing += OnWindowClosing;

                // Remove the loaded event handler as it's no longer needed
                this.Loaded -= OnWindowLoaded;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing Backup/Restore window: {ex.Message}", "Initialization Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private void OnViewModelRequestClose(object? sender, EventArgs e)
        {
            Close();
        }

        private void OnWindowClosing(object? sender, CancelEventArgs e)
        {
            // Check if operations are in progress
            if (_viewModel != null && (_viewModel.IsBackupInProgress || _viewModel.IsRestoreInProgress))
            {
                var result = MessageBox.Show(
                    "An operation is currently in progress. Do you want to cancel it and close the window?",
                    "Operation in Progress",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                    return;
                }

                // Cancel the operation
                _viewModel.CancelOperationCommand.Execute(null);
            }

            // Unsubscribe from events
            if (_viewModel != null)
            {
                _viewModel.RequestClose -= OnViewModelRequestClose;
                _viewModel.Dispose();
            }

            Closing -= OnWindowClosing;
        }
    }
}