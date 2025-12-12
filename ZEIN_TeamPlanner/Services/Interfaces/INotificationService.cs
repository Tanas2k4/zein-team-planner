using ZEIN_TeamPlanner.Models;

namespace ZEIN_TeamPlanner.Services.Interfaces
{
    public interface INotificationService
    {
        Task CreateNotificationAsync(string userId, string message, string type, string? relatedEntityId = null, string? relatedEntityType = null);
        Task<List<Notification>> GetUserNotificationsAsync(string userId);
        Task MarkAsReadAsync(int notificationId);
    }
}