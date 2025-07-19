using Notifications.Wpf;
using SchedulerWpfApp.ServiceRefactor.NotificationService;

public class NotificationService: INotificationService
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
            Type = NotificationType.Success
        }, areaName: "WindowArea");
    }

    public void ShowError(string message)
    {
        _notificationManager.Show(new NotificationContent
        {
            Title = "Lỗi",
            Message = message,
            Type = NotificationType.Error
        }, areaName: "WindowArea");
    }

    public void ShowWarning(string message)
    {
        _notificationManager.Show(new NotificationContent
        {
            Title = "Cảnh báo",
            Message = message,
            Type = NotificationType.Warning
        }, areaName: "WindowArea");
    }

    public void ShowInfo(string message)
    {
        _notificationManager.Show(new NotificationContent
        {
            Title = "Thông tin",
            Message = message,
            Type = NotificationType.Information
        }, areaName: "WindowArea");
    }
}
