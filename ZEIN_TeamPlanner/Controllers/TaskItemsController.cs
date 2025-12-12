using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TeamPlanner.Data;
using ZEIN_TeamPlanner.DTOs.TasksDto;
using ZEIN_TeamPlanner.Models;
using ZEIN_TeamPlanner.Services.Interfaces;

namespace ZEIN_TeamPlanner.Controllers
{
    [Authorize]
    public class TaskItemsController : Controller
    {
        private readonly ITaskService _taskService;
        private readonly IGroupService _groupService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;

        public TaskItemsController(ITaskService taskService, IGroupService groupService, UserManager<ApplicationUser> userManager, ApplicationDbContext context, INotificationService notificationService)
        {
            _taskService = taskService;
            _groupService = groupService;
            _userManager = userManager;
            _context = context;
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int groupId, string search, string status, string assignedTo)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!await _groupService.CanAccessGroupAsync(groupId, userId))
                return Forbid();

            var isMember = !await _groupService.IsUserAdminAsync(groupId, userId);
            var query = _context.TaskItems
                .Include(t => t.AssignedToUser)
                .Include(t => t.Priority)
                .Where(t => t.GroupId == groupId);

            // Apply filters
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(t => t.Title.Contains(search) || t.Description.Contains(search));
            }
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<TaskItem.TaskStatus>(status, out var statusFilter))
            {
                query = query.Where(t => t.Status == statusFilter);
            }
            if (!string.IsNullOrEmpty(assignedTo))
            {
                query = query.Where(t => assignedTo == "self" ? t.AssignedToUserId == userId : t.AssignedToUserId != userId);
            }

            var tasks = await query
                .OrderBy(t => t.Status == TaskItem.TaskStatus.InProgress ? 0 : t.Status == TaskItem.TaskStatus.ToDo ? 1 : 2)
                .ThenBy(t => t.Status == TaskItem.TaskStatus.InProgress && t.Deadline.HasValue ? t.Deadline.Value : DateTime.MaxValue)
                .ToListAsync();

            ViewBag.GroupId = groupId;
            ViewBag.GroupName = (await _context.Groups.FindAsync(groupId))?.GroupName;
            ViewBag.IsMember = isMember;
            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.AssignedTo = assignedTo;
            return View(tasks);
        }

        [HttpGet]
        public async Task<IActionResult> GlobalTasks(string search, string status, string assignedTo)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var groupIds = await _context.GroupMembers
                .Where(gm => gm.UserId == userId && gm.LeftAt == null)
                .Select(gm => gm.GroupId)
                .Union(_context.Groups.Where(g => g.CreatedByUserId == userId).Select(g => g.GroupId))
                .ToListAsync();

            var query = _context.TaskItems
                .Include(t => t.Group).ThenInclude(g => g.Members)
                .Include(t => t.Priority)
                .Include(t => t.AssignedToUser)
                .Where(t => groupIds.Contains(t.GroupId));

            // Apply filters
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(t => t.Title.Contains(search) || t.Description.Contains(search));
            }
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<TaskItem.TaskStatus>(status, out var statusFilter))
            {
                query = query.Where(t => t.Status == statusFilter);
            }
            if (!string.IsNullOrEmpty(assignedTo))
            {
                query = query.Where(t => assignedTo == "self" ? t.AssignedToUserId == userId : t.AssignedToUserId != userId);
            }

            var tasks = await query
                .OrderBy(t => t.Status == TaskItem.TaskStatus.InProgress ? 0 : t.Status == TaskItem.TaskStatus.ToDo ? 1 : 2)
                .ThenBy(t => t.Status == TaskItem.TaskStatus.InProgress && t.Deadline.HasValue ? t.Deadline.Value : DateTime.MaxValue)
                .ToListAsync();

            if (!tasks.Any() && string.IsNullOrEmpty(search) && string.IsNullOrEmpty(status) && string.IsNullOrEmpty(assignedTo))
            {
                ViewBag.Message = "Bạn chưa có nhiệm vụ nào. Hãy tham gia hoặc tạo một nhóm để bắt đầu.";
            }

            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.AssignedTo = assignedTo;
            return View(tasks);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var task = await _context.TaskItems
                .Include(t => t.Group)
                .Include(t => t.AssignedToUser)
                .Include(t => t.Priority)
                .FirstOrDefaultAsync(t => t.TaskItemId == id);

            if (task == null || !await _taskService.CanAccessTaskAsync(id, userId))
                return Forbid();

            ViewBag.IsMember = !await _groupService.IsUserAdminAsync(task.GroupId, userId);

            var attachments = await _context.FileAttachments
                .Where(f => f.EntityType == "TaskItem" && f.EntityId == id)
                .ToListAsync();
            ViewBag.Attachments = attachments;

            return View(task);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int groupId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!await _groupService.IsUserAdminAsync(groupId, userId))
                return Forbid();

            var members = await _context.GroupMembers
                .Where(gm => gm.GroupId == groupId && gm.LeftAt == null)
                .Include(gm => gm.User)
                .Select(gm => new { gm.UserId, gm.User.FullName })
                .ToListAsync();

            var group = await _context.Groups
                .Include(g => g.CreatedByUser)
                .FirstOrDefaultAsync(g => g.GroupId == groupId);

            if (group == null)
                return NotFound();

            if (!members.Any(m => m.UserId == group.CreatedByUserId))
            {
                members.Add(new { UserId = group.CreatedByUserId, FullName = group.CreatedByUser?.FullName ?? "Admin" });
            }

            var priorities = await _context.Priorities
                .Select(p => new { p.PriorityId, p.Name })
                .ToListAsync();

            ViewBag.Members = members;
            ViewBag.Priorities = priorities;
            ViewBag.GroupId = groupId;
            ViewBag.GroupName = group.GroupName;

            return View(new TaskCreateDto { GroupId = groupId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TaskCreateDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!await _groupService.IsUserAdminAsync(dto.GroupId, userId))
                return Forbid();

            if (!ModelState.IsValid)
            {
                var members = await _context.GroupMembers
                    .Where(gm => gm.GroupId == dto.GroupId && gm.LeftAt == null)
                    .Include(gm => gm.User)
                    .Select(gm => new { gm.UserId, gm.User.FullName })
                    .ToListAsync();
                var priorities = await _context.Priorities
                    .Select(p => new { p.PriorityId, p.Name })
                    .ToListAsync();
                ViewBag.Members = members;
                ViewBag.Priorities = priorities;
                ViewBag.GroupId = dto.GroupId;
                ViewBag.GroupName = (await _context.Groups.FindAsync(dto.GroupId))?.GroupName;
                return View(dto);
            }

            try
            {
                var task = await _taskService.CreateTaskAsync(dto, userId);
                if (!string.IsNullOrEmpty(dto.AssignedToUserId))
                {
                    var loadedTask = await _context.TaskItems
                        .Include(t => t.Group)
                        .FirstOrDefaultAsync(t => t.TaskItemId == task.TaskItemId);
                    await _notificationService.CreateNotificationAsync(
                        dto.AssignedToUserId,
                        $"Bạn được giao nhiệm vụ '{loadedTask.Title}' trong nhóm '{loadedTask.Group.GroupName}'.",
                        "TaskAssigned",
                        task.TaskItemId.ToString(),
                        "TaskItem"
                    );
                }
                return RedirectToAction("Index", new { groupId = dto.GroupId });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                var members = await _context.GroupMembers
                    .Where(gm => gm.GroupId == dto.GroupId && gm.LeftAt == null)
                    .Include(gm => gm.User)
                    .Select(gm => new { gm.UserId, gm.User.FullName })
                    .ToListAsync();
                var priorities = await _context.Priorities
                    .Select(p => new { p.PriorityId, p.Name })
                    .ToListAsync();
                ViewBag.Members = members;
                ViewBag.Priorities = priorities;
                ViewBag.GroupId = dto.GroupId;
                ViewBag.GroupName = (await _context.Groups.FindAsync(dto.GroupId))?.GroupName;
                return View(dto);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var task = await _context.TaskItems
                .Include(t => t.Group).ThenInclude(g => g.Members)
                .FirstOrDefaultAsync(t => t.TaskItemId == id);

            if (task == null)
                return NotFound();

            if (!await _taskService.CanAccessTaskAsync(id, userId))
                return Forbid();

            var dto = new TaskEditDto
            {
                TaskItemId = task.TaskItemId,
                Title = task.Title,
                Description = task.Description,
                Status = task.Status,
                Deadline = task.Deadline,
                AssignedToUserId = task.AssignedToUserId,
                GroupId = task.GroupId,
                PriorityId = task.PriorityId,
                Tags = task.Tags,
                CompletedAt = task.CompletedAt
            };

            var members = await _context.GroupMembers
                .Where(gm => gm.GroupId == task.GroupId && gm.LeftAt == null)
                .Include(gm => gm.User)
                .Select(gm => new { gm.UserId, gm.User.FullName })
                .ToListAsync();
            var priorities = await _context.Priorities
                .Select(p => new { p.PriorityId, p.Name })
                .ToListAsync();

            ViewBag.Members = members;
            ViewBag.Priorities = priorities;
            ViewBag.GroupId = task.GroupId;
            ViewBag.GroupName = task.Group.GroupName;

            return View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(TaskEditDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!await _taskService.CanAccessTaskAsync(dto.TaskItemId, userId))
                return Forbid();

            if (!ModelState.IsValid)
            {
                var members = await _context.GroupMembers
                    .Where(gm => gm.GroupId == dto.GroupId && gm.LeftAt == null)
                    .Include(gm => gm.User)
                    .Select(gm => new { gm.UserId, gm.User.FullName })
                    .ToListAsync();
                var priorities = await _context.Priorities
                    .Select(p => new { p.PriorityId, p.Name })
                    .ToListAsync();
                ViewBag.Members = members;
                ViewBag.Priorities = priorities;
                ViewBag.GroupId = dto.GroupId;
                ViewBag.GroupName = (await _context.Groups.FindAsync(dto.GroupId))?.GroupName;
                return View(dto);
            }

            try
            {
                var taskBeforeUpdate = await _context.TaskItems
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.TaskItemId == dto.TaskItemId);
                await _taskService.UpdateTaskAsync(dto, userId);
                var taskAfterUpdate = await _context.TaskItems
                    .Include(t => t.Group).ThenInclude(g => g.Members)
                    .FirstOrDefaultAsync(t => t.TaskItemId == dto.TaskItemId);

                // Notify if AssignedToUserId changed
                if (taskBeforeUpdate.AssignedToUserId != dto.AssignedToUserId && !string.IsNullOrEmpty(dto.AssignedToUserId))
                {
                    await _notificationService.CreateNotificationAsync(
                        dto.AssignedToUserId,
                        $"Bạn được giao nhiệm vụ '{taskAfterUpdate.Title}' trong nhóm '{taskAfterUpdate.Group.GroupName}'.",
                        "TaskAssigned",
                        dto.TaskItemId.ToString(),
                        "TaskItem"
                    );
                }

                // Notify if Status changed by a member
                var isAdmin = await _groupService.IsUserAdminAsync(dto.GroupId, userId);
                if (!isAdmin && taskBeforeUpdate.Status != dto.Status)
                {
                    var admins = taskAfterUpdate.Group.Members
                        .Where(m => m.Role == GroupRole.Admin && m.LeftAt == null)
                        .Select(m => m.UserId)
                        .Union(new[] { taskAfterUpdate.Group.CreatedByUserId })
                        .Distinct();
                    foreach (var adminId in admins)
                    {
                        await _notificationService.CreateNotificationAsync(
                            adminId,
                            $"Thành viên đã cập nhật trạng thái nhiệm vụ '{taskAfterUpdate.Title}' thành '{dto.Status}' trong nhóm '{taskAfterUpdate.Group.GroupName}'.",
                            "TaskStatusUpdated",
                            dto.TaskItemId.ToString(),
                            "TaskItem"
                        );
                    }
                }

                return RedirectToAction("Index", new { groupId = dto.GroupId });
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                var members = await _context.GroupMembers
                    .Where(gm => gm.GroupId == dto.GroupId && gm.LeftAt == null)
                    .Include(gm => gm.User)
                    .Select(gm => new { gm.UserId, gm.User.FullName })
                    .ToListAsync();
                var priorities = await _context.Priorities
                    .Select(p => new { p.PriorityId, p.Name })
                    .ToListAsync();
                ViewBag.Members = members;
                ViewBag.Priorities = priorities;
                ViewBag.GroupId = dto.GroupId;
                ViewBag.GroupName = (await _context.Groups.FindAsync(dto.GroupId))?.GroupName;
                return View(dto);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var task = await _context.TaskItems
                .Include(t => t.Group)
                .Include(t => t.AssignedToUser)
                .Include(t => t.Priority)
                .FirstOrDefaultAsync(t => t.TaskItemId == id);

            if (task == null)
                return NotFound();

            if (!await _groupService.IsUserAdminAsync(task.GroupId, userId))
                return Forbid();

            return View(task);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var task = await _context.TaskItems.FindAsync(id);
            if (task == null)
                return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!await _groupService.IsUserAdminAsync(task.GroupId, userId))
                return Forbid();

            try
            {
                await _taskService.DeleteTaskAsync(id, userId);
                return RedirectToAction(nameof(Index), new { groupId = task.GroupId });
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int taskId, TaskItem.TaskStatus status)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var task = await _context.TaskItems
                .Include(t => t.Group).ThenInclude(g => g.Members)
                .Include(t => t.AssignedToUser)
                .FirstOrDefaultAsync(t => t.TaskItemId == taskId);

            if (task == null)
                return NotFound();

            if (!await _taskService.CanAccessTaskAsync(taskId, userId))
                return Forbid();

            try
            {
                await _taskService.UpdateTaskStatusAsync(taskId, status, userId);
                var isAdmin = await _groupService.IsUserAdminAsync(task.GroupId, userId);
                if (isAdmin && task.AssignedToUserId != null)
                {
                    await _notificationService.CreateNotificationAsync(
                        task.AssignedToUserId,
                        $"Quản trị viên đã cập nhật trạng thái nhiệm vụ '{task.Title}' thành '{status}' trong nhóm '{task.Group.GroupName}'.",
                        "TaskStatusUpdated",
                        taskId.ToString(),
                        "TaskItem"
                    );
                }
                else if (!isAdmin)
                {
                    var admins = task.Group.Members
                        .Where(m => m.Role == GroupRole.Admin && m.LeftAt == null)
                        .Select(m => m.UserId)
                        .Union(new[] { task.Group.CreatedByUserId })
                        .Distinct();
                    foreach (var adminId in admins)
                    {
                        await _notificationService.CreateNotificationAsync(
                            adminId,
                            $"Thành viên đã cập nhật trạng thái nhiệm vụ '{task.Title}' thành '{status}' trong nhóm '{task.Group.GroupName}'.",
                            "TaskStatusUpdated",
                            taskId.ToString(),
                            "TaskItem"
                        );
                    }
                }
                return RedirectToAction(nameof(Index), new { groupId = task.GroupId });
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Index), new { groupId = task.GroupId });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAttachment(int taskId, IFormFile file)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var task = await _context.TaskItems
                .Include(t => t.Group)
                .FirstOrDefaultAsync(t => t.TaskItemId == taskId);

            if (task == null || !await _taskService.CanAccessTaskAsync(taskId, userId))
                return Forbid();

            var isMember = !await _groupService.IsUserAdminAsync(task.GroupId, userId);
            var isWithinDeadline = task.Deadline.HasValue && task.Deadline > DateTime.UtcNow;

            if (isMember && !isWithinDeadline)
            {
                TempData["Error"] = "Không được tải tệp sau thời hạn.";
                return RedirectToAction(nameof(Details), new { id = taskId });
            }

            if (file != null && file.Length > 0)
            {
                var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads", fileName);

                Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? throw new InvalidOperationException());
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var attachment = new FileAttachment
                {
                    FileName = file.FileName,
                    FileUrl = $"/uploads/{fileName}",
                    EntityType = "TaskItem",
                    EntityId = taskId,
                    UserId = userId,
                    UploadedAt = DateTime.UtcNow
                };

                _context.FileAttachments.Add(attachment);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Tệp đã được tải lên thành công.";
            }

            return RedirectToAction(nameof(Details), new { id = taskId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAttachment(int attachmentId, int taskId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var attachment = await _context.FileAttachments
                .FirstOrDefaultAsync(f => f.Id == attachmentId && f.EntityType == "TaskItem" && f.EntityId == taskId);

            if (attachment == null || !await _taskService.CanAccessTaskAsync(taskId, userId))
                return Forbid();

            var task = await _context.TaskItems
                .FirstOrDefaultAsync(t => t.TaskItemId == taskId);

            var isMember = !await _groupService.IsUserAdminAsync(task.GroupId, userId);
            var isWithinDeadline = task.Deadline.HasValue && task.Deadline > DateTime.UtcNow;

            if (isMember && !isWithinDeadline)
            {
                TempData["Error"] = "Không được xóa tệp sau thời hạn.";
                return RedirectToAction(nameof(Details), new { id = taskId });
            }

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", attachment.FileUrl.TrimStart('/'));
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }

            _context.FileAttachments.Remove(attachment);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Tệp đã được xóa thành công.";
            return RedirectToAction(nameof(Details), new { id = taskId });
        }
    }
}