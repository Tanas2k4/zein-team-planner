using Microsoft.EntityFrameworkCore;
using TeamPlanner.Data;
using ZEIN_TeamPlanner.Models;
using ZEIN_TeamPlanner.Services.Interfaces;

namespace ZEIN_TeamPlanner.Shared
{
    public class ReminderBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public ReminderBackgroundService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                    var now = DateTime.UtcNow;
                    var reminderTime = now.AddHours(24);

                    // Task reminders
                    var tasks = await context.TaskItems
                        .Include(t => t.AssignedToUser)
                        .Include(t => t.Group).ThenInclude(g => g.Members)
                        .Where(t => t.Deadline.HasValue && t.Deadline.Value >= now && t.Deadline.Value <= reminderTime)
                        .ToListAsync();

                    foreach (var task in tasks)
                    {
                        // Notify assigned user
                        if (task.AssignedToUserId != null)
                        {
                            await notificationService.CreateNotificationAsync(
                                task.AssignedToUserId,
                                $"Nhiệm vụ '{task.Title}' sắp đến hạn vào {task.Deadline.Value:dd MMMM yyyy HH:mm}.",
                                "TaskReminder",
                                task.TaskItemId.ToString(),
                                "TaskItem"
                            );
                        }

                        // Notify group admins
                        var admins = task.Group.Members
                            .Where(m => m.Role == GroupRole.Admin && m.LeftAt == null)
                            .Select(m => m.UserId)
                            .Union(new[] { task.Group.CreatedByUserId })
                            .Distinct();
                        foreach (var adminId in admins)
                        {
                            await notificationService.CreateNotificationAsync(
                                adminId,
                                $"Nhiệm vụ '{task.Title}' trong nhóm '{task.Group.GroupName}' sắp đến hạn vào {task.Deadline.Value:dd MMMM yyyy HH:mm}.",
                                "TaskReminder",
                                task.TaskItemId.ToString(),
                                "TaskItem"
                            );
                        }
                    }

                    // Event reminders
                    var events = await context.CalendarEvents
    .Include(e => e.Group).ThenInclude(g => g.Members)
    .Where(e => e.StartTime >= new DateTimeOffset(now, TimeSpan.Zero) && e.StartTime <= new DateTimeOffset(reminderTime, TimeSpan.Zero))
    .ToListAsync();

                    foreach (var @event in events)
                    {
                        // Notify all group members
                        var members = @event.Group.Members
                            .Where(m => m.LeftAt == null)
                            .Select(m => m.UserId)
                            .Union(new[] { @event.Group.CreatedByUserId })
                            .Distinct();
                        foreach (var userId in members)
                        {
                            await notificationService.CreateNotificationAsync(
                                userId,
                                $"Sự kiện '{@event.Title}' trong nhóm '{@event.Group.GroupName}' sắp diễn ra vào {@event.StartTime:dd MMMM yyyy HH:mm}.",
                                "EventReminder",
                                @event.CalendarEventId.ToString(),
                                "CalendarEvent"
                            );
                        }
                    }
                }

                // Check every hour
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }
}