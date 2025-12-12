using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Security.Claims;
using TeamPlanner.Data;
using ZEIN_TeamPlanner.Services.Interfaces;

namespace ZEIN_TeamPlanner.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly INotificationService _notificationService;
        private readonly ApplicationDbContext _context;

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
            return Json(notifications); // Ensure this returns an array
        }
        [HttpPost("MarkAllAsRead")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Console.WriteLine($"Marking all as read for user: {userId}");

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();
            if (notifications.Count == 0)
            {
                return Ok(new { message = "No unread notifications to mark." });
            }

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
            Console.WriteLine($"Raw Request Body: {await new StreamReader(HttpContext.Request.Body).ReadToEndAsync()}");
            Console.WriteLine($"Received notificationIds: {JsonConvert.SerializeObject(model?.NotificationIds)}");

            if (model?.NotificationIds == null || !model.NotificationIds.Any())
            {
                return BadRequest(new { error = "No notification IDs provided." });
            }

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && model.NotificationIds.Contains(n.Id))
                .ToListAsync();
            if (notifications.Count == 0)
            {
                return NotFound(new { error = "No matching notifications found." });
            }

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}