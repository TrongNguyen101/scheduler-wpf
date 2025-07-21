using System.Windows.Media;
using Notification.Wpf;
using SchedulerWpfApp.ServiceRefactor.NotificationService;

public class NotificationService : INotificationService
{
    private readonly NotificationManager _notificationManager;

    public NotificationService()
    {
        _notificationManager = new NotificationManager();
    }

    public void ShowSuccess(string message)
    {
        _notificationManager.Show(new NotificationContent
        {
            Title = "Thành công",
            Message = message,
            Type = NotificationType.Success,
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#34A853"))
        }, areaName: "WindowArea");
    }

    public void ShowError(string message)
    {
        _notificationManager.Show(new NotificationContent
        {
            Title = "Lỗi",
            Message = message,
            Type = NotificationType.Error,
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EA4335"))
        }, areaName: "WindowArea");
    }

    public void ShowWarning(string message)
    {
        _notificationManager.Show(new NotificationContent
        {
            Title = "Cảnh báo",
            Message = message,
            Type = NotificationType.Warning,
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9800"))
        }, areaName: "WindowArea");
    }

    public void ShowInfo(string message)
    {
        _notificationManager.Show(new NotificationContent
        {
            Title = "Thông tin",
            Message = message,
            Type = NotificationType.Information,
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4285F4"))
        }, areaName: "WindowArea");
    }
}
