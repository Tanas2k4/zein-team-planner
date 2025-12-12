using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TeamPlanner.Data;
using ZEIN_TeamPlanner.DTOs.GroupsDto;
using ZEIN_TeamPlanner.Models;
using ZEIN_TeamPlanner.Services.Interfaces;

namespace ZEIN_TeamPlanner.Controllers
{
    [Authorize] // Restricts access to authenticated users only
    public class GroupsController : Controller
    {
        private readonly IGroupService _groupService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;

        public GroupsController(IGroupService groupService, UserManager<ApplicationUser> userManager, ApplicationDbContext context, INotificationService notificationService)
        {
            _groupService = groupService;
            _userManager = userManager;
            _context = context;
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string nameSearch, string dateSearch, string roleSearch)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // Query groups where the user is either a member (not left) or the creator
            var query = _context.Groups
                .Include(g => g.Members)
                .Where(g => g.Members.Any(m => m.UserId == userId && m.LeftAt == null) || g.CreatedByUserId == userId);

            if (!string.IsNullOrWhiteSpace(nameSearch))
            {
                query = query.Where(g => g.GroupName.Contains(nameSearch));
            }

            if (!string.IsNullOrWhiteSpace(dateSearch) && DateTime.TryParse(dateSearch, out var date))
            {
                query = query.Where(g => g.CreateAt.Date == date.Date);
            }

            // Filter by role (Admin or Member) for the current user
            if (!string.IsNullOrWhiteSpace(roleSearch))
            {
                if (roleSearch == "Admin")
                {
                    query = query.Where(g => g.Members.Any(m => m.UserId == userId && m.Role == GroupRole.Admin && m.LeftAt == null));
                }
                else if (roleSearch == "Member")
                {
                    query = query.Where(g => g.Members.Any(m => m.UserId == userId && m.Role == GroupRole.Member && m.LeftAt == null));
                }
            }

            var groups = await query.ToListAsync();
            ViewData["NameSearch"] = nameSearch;
            ViewData["DateSearch"] = dateSearch;
            ViewData["RoleSearch"] = roleSearch;
            return View(groups);
        }

        [HttpGet]
        public async Task<IActionResult> Search(string nameSearch, string dateSearch, string roleSearch)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // Similar filtering logic as Index, but returns JSON for async search
            var query = _context.Groups
                .Include(g => g.Members)
                .Where(g => g.Members.Any(m => m.UserId == userId && m.LeftAt == null) || g.CreatedByUserId == userId);

            if (!string.IsNullOrWhiteSpace(nameSearch))
            {
                query = query.Where(g => g.GroupName.Contains(nameSearch));
            }

            if (!string.IsNullOrWhiteSpace(dateSearch) && DateTime.TryParse(dateSearch, out var date))
            {
                query = query.Where(g => g.CreateAt.Date == date.Date);
            }

            if (!string.IsNullOrWhiteSpace(roleSearch))
            {
                if (roleSearch == "Admin")
                {
                    query = query.Where(g => g.Members.Any(m => m.UserId == userId && m.Role == GroupRole.Admin && m.LeftAt == null));
                }
                else if (roleSearch == "Member")
                {
                    query = query.Where(g => g.Members.Any(m => m.UserId == userId && m.Role == GroupRole.Member && m.LeftAt == null));
                }
            }

            // Project query to a lightweight anonymous object for JSON response
            var groups = await query.Select(g => new
            {
                g.GroupId,
                g.GroupName,
                // Truncate description to 50 chars if too long
                Description = g.Description != null && g.Description.Length > 50 ? g.Description.Substring(0, 50) + "..." : (g.Description ?? "Không có mô tả"),
                MemberCount = g.Members.Count(m => m.LeftAt == null),
                CreateAt = g.CreateAt.ToString("dd MMM yyyy"),
                // Determine user's role in the group, default to "N/A" if not a member
                Role = g.Members.FirstOrDefault(m => m.UserId == userId && m.LeftAt == null) != null ? g.Members.FirstOrDefault(m => m.UserId == userId && m.LeftAt == null).Role.ToString() : "N/A",
                IsAdmin = g.Members.Any(m => m.UserId == userId && m.Role == GroupRole.Admin && m.LeftAt == null)
            }).ToListAsync();

            return Json(groups);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            // Load all users for the group creation form (e.g., to select members)
            var users = await _userManager.Users
                .Select(u => new { u.Id, u.FullName })
                .ToListAsync();
            ViewBag.Users = users;
            return View(new GroupCreateDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(GroupCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                var users = await _userManager.Users
                    .Select(u => new { u.Id, u.FullName })
                    .ToListAsync();
                ViewBag.Users = users;
                return View(dto);
            }

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                // Delegate group creation to the service layer
                var group = await _groupService.CreateGroupAsync(dto, userId);
                // Notify the creator about successful group creation
                await _notificationService.CreateNotificationAsync(
                    userId,
                    $"You have created '{group.GroupName}' successfully.",
                    "GroupCreated",
                    group.GroupId.ToString(),
                    "Group"
                );
                return RedirectToAction("Details", new { id = group.GroupId });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                var users = await _userManager.Users
                    .Select(u => new { u.Id, u.FullName })
                    .ToListAsync();
                ViewBag.Users = users;
                return View(dto);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // Check if the user has access to the group
            if (!await _groupService.CanAccessGroupAsync(id, userId))
                return Forbid();

            // Load group details with members and creator
            var group = await _context.Groups
                .Include(g => g.Members).ThenInclude(m => m.User)
                .Include(g => g.CreatedByUser)
                .FirstOrDefaultAsync(g => g.GroupId == id);

            if (group == null)
                return NotFound();

            ViewBag.IsAdmin = await _groupService.IsUserAdminAsync(id, userId);
            return View(group);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var group = await _context.Groups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.GroupId == id);

            if (group == null)
                return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // Ensure only admins can edit the group
            if (!await _groupService.IsUserAdminAsync(id, userId))
                return Forbid();

            // Map group data to DTO for editing
            var dto = new GroupEditDto
            {
                GroupId = group.GroupId,
                GroupName = group.GroupName,
                Description = group.Description,
                MemberIds = group.Members
                    .Where(m => m.LeftAt == null)
                    .Select(m => m.UserId)
                    .ToList()
            };

            var users = await _userManager.Users
                .Select(u => new { u.Id, u.FullName })
                .ToListAsync();
            ViewBag.Users = users;
            return View(dto);
        }
                                      
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(GroupEditDto dto)
        {
            if (!ModelState.IsValid)
            {
                var users = await _userManager.Users
                    .Select(u => new { u.Id, u.FullName })
                    .ToListAsync();
                ViewBag.Users = users;
                return View(dto);
            }

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                // Update group via service layer
                var group = await _groupService.UpdateGroupAsync(dto, userId);
                return RedirectToAction("Details", new { id = group.GroupId });
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                var users = await _userManager.Users
                    .Select(u => new { u.Id, u.FullName })
                    .ToListAsync();
                ViewBag.Users = users;
                return View(dto);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InviteMember(int groupId, string email)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // Only admins can invite members
            if (!await _groupService.IsUserAdminAsync(groupId, userId))
                return Forbid();

            var group = await _context.Groups.FindAsync(groupId);
            if (group == null)
                return NotFound();

            var invitedUser = await _userManager.FindByEmailAsync(email);
            if (invitedUser == null)
            {
                TempData["Error"] = "Can't find user with this email.";
                return RedirectToAction(nameof(Details), new { id = groupId });
            }

            // Remove any previous membership if the user left before
            var oldMember = await _context.GroupMembers
                .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == invitedUser.Id && gm.LeftAt != null);
            if (oldMember != null)
            {
                _context.GroupMembers.Remove(oldMember);
                await _context.SaveChangesAsync();
            }

            // Check if the user is already an active member
            var existingMember = await _context.GroupMembers
                .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == invitedUser.Id && gm.LeftAt == null);
            if (existingMember != null)
            {
                TempData["Error"] = "This user has already a member of this group.";
                return RedirectToAction(nameof(Details), new { id = groupId });
            }

            // Add new member to the group
            var groupMember = new GroupMember
            {
                GroupId = groupId,
                UserId = invitedUser.Id,
                Role = GroupRole.Member,
                JoinedAt = DateTime.UtcNow
            };

            _context.GroupMembers.Add(groupMember);
            await _context.SaveChangesAsync();

            // Notify the invited user
            await _notificationService.CreateNotificationAsync(
                invitedUser.Id,
                $"You have joined '{group.GroupName}'.",
                "GroupInvite",
                groupId.ToString(),
                "Group"
            );

            TempData["Success"] = $"Invited {invitedUser.FullName} as a member.";
            return RedirectToAction(nameof(Details), new { id = groupId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveMember(int groupId, string memberId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            try
            {
                var group = await _context.Groups.FindAsync(groupId);
                if (group == null)
                    throw new KeyNotFoundException();

                // Delegate member removal to service layer
                await _groupService.RemoveMemberAsync(groupId, memberId, userId);
                await _notificationService.CreateNotificationAsync(
                    memberId,
                    $"You have been removed from '{group.GroupName}'.",
                    "GroupMemberRemoved",
                    groupId.ToString(),
                    "Group"
                );
                TempData["Success"] = "Removed the member successfully.";
            }
            catch (KeyNotFoundException)
            {
                TempData["Error"] = "This group or member does not exist.";
            }
            catch (UnauthorizedAccessException)
            {
                TempData["Error"] = "You don't have permision to do this.";
            }
            return RedirectToAction(nameof(Details), new { id = groupId });
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // Only admins can delete groups
            if (!await _groupService.IsUserAdminAsync(id, userId))
                return Forbid();

            var group = await _context.Groups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.GroupId == id);

            if (group == null)
                return NotFound();

            return View(group);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            try
            {
                var group = await _context.Groups.FindAsync(id);
                if (group == null)
                    throw new KeyNotFoundException();

                // Delegate group deletion to service layer
                await _groupService.DeleteGroupAsync(id, userId);
                await _notificationService.CreateNotificationAsync(
                    userId,
                    $"'{group.GroupName}' has been deleted.",
                    "GroupDeleted",
                    id.ToString(),
                    "Group"
                );
                TempData["Success"] = "deleted succesfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (KeyNotFoundException)
            {
                TempData["Error"] = "Group does not exist.";
                return RedirectToAction(nameof(Index));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LeaveGroup(int groupId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            try
            {
                var group = await _context.Groups
                    .Include(g => g.Members)
                    .FirstOrDefaultAsync(g => g.GroupId == groupId);
                if (group == null)
                    throw new KeyNotFoundException();

                // Delegate leaving group to service layer
                await _groupService.LeaveGroupAsync(groupId, userId);
                // Notify all admins and the creator about the member leaving
                var admins = group.Members
                    .Where(m => m.Role == GroupRole.Admin && m.LeftAt == null)
                    .Select(m => m.UserId)
                    .Union(new[] { group.CreatedByUserId })
                    .Distinct();
                foreach (var adminId in admins)
                {
                    await _notificationService.CreateNotificationAsync(
                        adminId,
                        $"This member has left '{group.GroupName}'.",
                        "GroupMemberLeft",
                        groupId.ToString(),
                        "Group"
                    );
                }
                TempData["Success"] = "You have left the group.";
                return RedirectToAction(nameof(Index));
            }
            catch (KeyNotFoundException)
            {
                TempData["Error"] = "Group does not exist.";
                return RedirectToAction(nameof(Details), new { id = groupId });
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Details), new { id = groupId });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetTasks(int groupId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var group = await _context.Groups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.GroupId == groupId);

            if (group == null)
            {
                return NotFound(new { error = "Group not found." });
            }

            // Verify user is an active member
            var isMember = group.Members.Any(m => m.UserId == userId && m.LeftAt == null);
            if (!isMember)
            {
                return Forbid();
            }

            // Fetch tasks for the group
            var tasks = await _context.TaskItems
                .Where(t => t.GroupId == groupId)
                .Select(t => new
                {
                    Status = t.Status.ToString(),
                    CreatedAt = t.CreatedAt.ToString("yyyy-MM-dd")
                })
                .ToListAsync();

            // Aggregate task counts by status for charting
            var statusCounts = tasks
                .GroupBy(t => t.Status)
                .Select(g => new
                {
                    Status = g.Key,
                    Count = g.Count()
                })
                .ToList();

            return Json(new
            {
                Tasks = tasks,
                StatusCounts = statusCounts
            });
        }
    }
}