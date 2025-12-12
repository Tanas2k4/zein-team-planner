using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Security.Claims;
using TeamPlanner.Data;
using ZEIN_TeamPlanner.Services.Interfaces;

namespace ZEIN_TeamPlanner.Controllers
{
    [Authorize] // Restricts access to authenticated users only
    public class NotificationsController : Controller
    {
        private readonly INotificationService _notificationService;
        private readonly ApplicationDbContext _context;

        // Model for handling multiple notification IDs in MarkAsRead action
        public class MarkAsReadModel
        {
            public int[] NotificationIds { get; set; }
        }

        public NotificationsController(INotificationService notificationService, ApplicationDbContext context)
        {
            _notificationService = notificationService;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // Fetch all notifications for the current user, sorted by creation date (newest first)
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
            return View(notifications);
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // Fetch up to 20 recent notifications for the user, projected to a lightweight object for JSON
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(20)
                .Select(n => new
                {
                    n.Id,
                    n.Message,
                    n.Type,
                    n.RelatedEntityId,
                    n.RelatedEntityType,
                    n.IsRead,
                    n.CreatedAt
                })
                .ToListAsync();
            return Json(notifications); // Returns an array of notifications for client-side rendering
        }

        [HttpPost("MarkAllAsRead")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Console.WriteLine($"Marking all as read for user: {userId}");

            // Fetch all unread notifications for the user
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();
            if (notifications.Count == 0)
            {
                return Ok(new { message = "No unread notifications to mark." });
            }

            // Mark all fetched notifications as read
            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("MarkAsRead")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead([FromBody] MarkAsReadModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // Log request body for debugging purposes
            Console.WriteLine($"Raw Request Body: {await new StreamReader(HttpContext.Request.Body).ReadToEndAsync()}");
            Console.WriteLine($"Received notificationIds: {JsonConvert.SerializeObject(model?.NotificationIds)}");

            // Validate input: ensure notification IDs are provided
            if (model?.NotificationIds == null || !model.NotificationIds.Any())
            {
                return BadRequest(new { error = "No notification IDs provided." });
            }

            // Fetch notifications that belong to the user and match the provided IDs
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && model.NotificationIds.Contains(n.Id))
                .ToListAsync();
            if (notifications.Count == 0)
            {
                return NotFound(new { error = "No matching notifications found." });
            }

            // Mark selected notifications as read
            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}