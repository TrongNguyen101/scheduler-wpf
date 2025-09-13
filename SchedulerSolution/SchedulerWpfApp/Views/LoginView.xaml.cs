using System.Windows.Controls;
using System.Windows;
using SchedulerWpfApp.ViewModel;

namespace SchedulerWpfApp.Views
{
    public partial class LoginView : UserControl
    {
        public LoginView()
        {
            InitializeComponent();
            Loaded += (s, e) =>
            {
                if (DataContext is LoginViewModel vm)
                {
                    Pwd.PasswordChanged += (s2, e2) => vm.Password = Pwd.Password;
                }
            };
        }
    }
}


