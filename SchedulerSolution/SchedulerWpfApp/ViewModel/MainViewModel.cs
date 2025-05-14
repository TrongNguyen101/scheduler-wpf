using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using SchedulerWpfApp.Helper;
using SchedulerWpfApp.Model;
using SchedulerWpfApp.Services;
using Syncfusion.XlsIO;

namespace SchedulerWpfApp.ViewModel
{
    public class MainViewModel : ViewBaseModel
    {
        private object _currentViewModel;
        private readonly IServiceProvider _serviceProvider;
        public object CurrentViewModel
        {
            get => _currentViewModel;
            set { _currentViewModel = value; OnPropertyChanged(); }
        }

        public ICommand ShowCourseCommand { get; }
        public ICommand ShowTeacherCommand { get; }
        public ICommand ShowRoomCommand { get; }

        public MainViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            ShowCourseCommand = new RelayCommand(ShowCourse);
            ShowTeacherCommand = new RelayCommand(ShowTeacher);
            ShowRoomCommand = new RelayCommand(ShowRoom);

            CurrentViewModel = _serviceProvider.GetRequiredService<LecturerViewModel>();
        }

        private void ShowCourse()
        {
            CurrentViewModel = new CourseViewModel();
        }

        private void ShowTeacher()
        {
            CurrentViewModel = _serviceProvider.GetRequiredService<LecturerViewModel>();
        }

        private void ShowRoom()
        {
            CurrentViewModel = new RoomViewModel();
        }

    }
}
