using SchedulerWpfApp.Helper;
using System.Windows.Input;

namespace SchedulerWpfApp.ViewModel
{
    public class ProcessBarViewModel : ViewBaseModel
    {
        private double _progressValue;
        private string _progressMessage;
        private bool _isOpen;
        public double ProgressValue
        {
            get => _progressValue;
            set => SetProperty(ref _progressValue, value);
        }

        public string ProgressMessage
        {
            get => _progressMessage;
            set => SetProperty(ref _progressMessage, value);
        }

        public bool IsOpen
        {
            get => _isOpen;
            set => SetProperty(ref _isOpen, value);
        }

        public ICommand CancelCommand { get; }

        public ProcessBarViewModel()
        {
            CancelCommand = new RelayCommand(OnCancel);
        }

        private void OnCancel()
        {
            ProgressMessage = "Đã hủy tiến trình.";
            ProgressValue = 0;
            IsOpen = false;
        }
  
        public void Start(string message)
        {
            ProgressMessage = message;
            ProgressValue = 0;
            IsOpen = true;
        }

        public void Finish()
        {
            IsOpen = false;
            ProgressValue = 0;
            ProgressMessage = string.Empty;
        }

    }
}
