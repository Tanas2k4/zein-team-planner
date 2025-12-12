using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using TeamPlanner.Data;
using ZEIN_TeamPlanner.DTOs.Shared;
using ZEIN_TeamPlanner.Models;

namespace ZEIN_TeamPlanner.Controllers
{
    public class DashboardController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public DashboardController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public IActionResult Index()
        {
            var currentUserId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var currentUser = currentUserId != null ? _userManager.FindByIdAsync(currentUserId).Result : null;

            if (currentUser == null) return RedirectToAction("Login", "Account");

            var model = new DashboardViewModel
            {
                WeeklyProgress = currentUserId != null ? CalculateWeeklyProgress(currentUserId) : 0,
                TotalHoursWorked = currentUserId != null ? CalculateTotalHoursWorked(currentUserId) : "00:00",
                ProjectCount = _context.TaskItems
                    .Count(t => t.AssignedToUserId == currentUserId), // Tổng số task được giao
                CompletedProjectCount = _context.TaskItems
                    .Count(t => t.AssignedToUserId == currentUserId && t.Status == TaskItem.TaskStatus.Done && t.CompletedAt.HasValue),
                ProjectProgressDates = _context.TaskItems
                    .Where(t => t.AssignedToUserId == currentUserId && t.StatusChangedAt.HasValue)
                    .Select(t => t.StatusChangedAt ?? DateTime.MinValue)
                    .Distinct()
                    .OrderByDescending(d => d)
                    .Take(5)
                    .ToList(),
                AchievedProjects = _context.TaskItems
                    .Where(t => t.AssignedToUserId == currentUserId && t.Status == TaskItem.TaskStatus.Done)
                    .GroupBy(t => t.StatusChangedAt ?? t.CreatedAt)
                    .Select(g => g.Count())
                    .ToList(),
                ToDoProjects = _context.TaskItems
                    .Where(t => t.AssignedToUserId == currentUserId && t.Status == TaskItem.TaskStatus.ToDo)
                    .GroupBy(t => t.StatusChangedAt ?? t.CreatedAt)
                    .Select(g => g.Count())
                    .ToList(),
                InProgressProjects = _context.TaskItems
                    .Where(t => t.AssignedToUserId == currentUserId && t.Status == TaskItem.TaskStatus.InProgress)
                    .GroupBy(t => t.StatusChangedAt ?? t.CreatedAt)
                    .Select(g => g.Count())
                    .ToList(),
                BlockedProjects = _context.TaskItems
                    .Where(t => t.AssignedToUserId == currentUserId && t.Status == TaskItem.TaskStatus.Blocked)
                    .GroupBy(t => t.StatusChangedAt ?? t.CreatedAt)
                    .Select(g => g.Count())
                    .ToList(),
                TargetProjects = _context.TaskItems
                    .Where(t => t.AssignedToUserId == currentUserId)
                    .GroupBy(t => t.StatusChangedAt ?? t.CreatedAt)
                    .Select(g => g.Count() * 2) // Ví dụ: Target gấp đôi Achieved
                    .ToList(),
                Collaborators = _context.Users
                    .Include(u => u.AssignedTasks)
                    .Where(u => _context.TaskItems.Any(t => t.AssignedToUserId == u.Id && t.Group.Members.Any(m => m.UserId == currentUserId)))
                    .ToList(),
                RecentTasks = _context.TaskItems
                    .Where(t => t.AssignedToUserId == currentUserId)
                    .OrderByDescending(t => t.CreatedAt)
                    .Take(5)
                    .ToList()
            };

            return View(model);
        }

        private int CalculateWeeklyProgress(string userId)
        {
            var completedTasks = _context.TaskItems
                .Count(t => t.AssignedToUserId == userId && t.Status == TaskItem.TaskStatus.Done && t.CompletedAt.HasValue);
            var totalTasks = _context.TaskItems.Count(t => t.AssignedToUserId == userId);
            return totalTasks > 0 ? (int)((completedTasks / (double)totalTasks) * 100) : 0;
        }

        private string CalculateTotalHoursWorked(string userId)
        {
            var totalMinutes = _context.TaskItems
                .Where(t => t.AssignedToUserId == userId && t.CompletedAt.HasValue)
                .AsEnumerable()
                .Sum(t => (t.CompletedAt.Value - t.CreatedAt).TotalMinutes);
            var hours = (int)(totalMinutes / 60);
            var minutes = (int)(totalMinutes % 60);
            return $"{hours:00}:{minutes:00}";
        }
    }
}