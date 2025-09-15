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
        public string Username
        {
            get => _username;
            set
            {
                _username = value?.Trim() ?? string.Empty;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanLogin));
                ClearErrorIfValid();
            }
        }

        private string _password = string.Empty;
        public string Password
        {
            get => _password;
            set
            {
                _password = value ?? string.Empty;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanLogin));
                ClearErrorIfValid();
            }
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                _isBusy = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanLogin));
            }
        }

        private string _error = string.Empty;
        public string Error
        {
            get => _error;
            set
            {
                _error = value;
                OnPropertyChanged();
            }
        }

        public bool CanLogin => !IsBusy && !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);

        public ICommand LoginCommand { get; }

        public LoginViewModel(AuthService auth)
        {
            _auth = auth;
            LoginCommand = new RelayCommand(async () => await DoLoginAsync(), () => CanLogin);
        }

        private async Task DoLoginAsync()
        {
            if (!ValidateInputs()) return;

            IsBusy = true;
            Error = string.Empty;

            try
            {
                var ok = await _auth.LoginAsync(Username, Password);
                if (!ok)
                {
                    Error = "Tài khoản hoặc mật khẩu không chính xác";
                    // Clear password on failed login for security
                    Password = string.Empty;
                }
            }
            catch (UnauthorizedAccessException)
            {
                Error = "Tài khoản hoặc mật khẩu không chính xác";
                Password = string.Empty;
            }
            catch (System.Net.Http.HttpRequestException)
            {
                Error = "Không thể kết nối đến máy chủ. Vui lòng kiểm tra kết nối mạng.";
            }
            catch (TaskCanceledException)
            {
                Error = "Kết nối đã hết thời gian chờ. Vui lòng thử lại.";
            }
            catch (Exception ex)
            {
                Error = "Đã xảy ra lỗi không mong muốn. Vui lòng thử lại.";
                System.Diagnostics.Trace.WriteLine($"Login error: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool ValidateInputs()
        {
            if (string.IsNullOrWhiteSpace(Username))
            {
                Error = "Vui lòng nhập tài khoản";
                return false;
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                Error = "Vui lòng nhập mật khẩu";
                return false;
            }

            if (Username.Length > 50)
            {
                Error = "Tài khoản không được vượt quá 50 ký tự";
                return false;
            }

            if (Password.Length < 6)
            {
                Error = "Mật khẩu phải có ít nhất 6 ký tự";
                return false;
            }

            return true;
        }

        private void ClearErrorIfValid()
        {
            if (!string.IsNullOrEmpty(Error) && ValidateInputs())
            {
                Error = string.Empty;
            }
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}


