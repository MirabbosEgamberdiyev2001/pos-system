namespace POS.Infrastructure.Notifications;

// Kelajakdagi SMS/Email notification uchun interface
public interface INotificationService
{
    Task SendAsync(string recipient, string subject, string message);
}
