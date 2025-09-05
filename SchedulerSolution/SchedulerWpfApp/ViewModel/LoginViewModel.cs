using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using SchedulerWpfApp.Helper;
using SchedulerWpfApp.ServiceRefactor.Auth;

namespace SchedulerWpfApp.ViewModel
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private readonly AuthService _auth;
        public event PropertyChangedEventHandler? PropertyChanged;

        private string _username = string.Empty;
        public string Username { get => _username; set { _username = value; OnPropertyChanged(); } }

        private string _password = string.Empty;
        public string Password { get => _password; set { _password = value; OnPropertyChanged(); } }

        private bool _isBusy;
        public bool IsBusy { get => _isBusy; set { _isBusy = value; OnPropertyChanged(); } }

        private string _error = string.Empty;
        public string Error { get => _error; set { _error = value; OnPropertyChanged(); } }

        public ICommand LoginCommand { get; }

        public LoginViewModel(AuthService auth)
        {
            _auth = auth;
            LoginCommand = new RelayCommand(async () => await DoLoginAsync(), () => !IsBusy);
        }

        private async Task DoLoginAsync()
        {
            IsBusy = true; Error = string.Empty;
            try
            {
                var ok = await _auth.LoginAsync(Username, Password);
                if (!ok) Error = "Đăng nhập thất bại";
            }
            catch (Exception ex)
            {
                Error = ex.Message;
            }
            finally { IsBusy = false; }
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}


