using Microsoft.EntityFrameworkCore;
using TeamPlanner.Data;
using ZEIN_TeamPlanner.DTOs.TasksDto;
using ZEIN_TeamPlanner.Models;
using ZEIN_TeamPlanner.Services.Interfaces;

namespace ZEIN_TeamPlanner.Services.Implementations
{
    public class TaskService : ITaskService
    {
        private readonly ApplicationDbContext _context;
        private readonly IGroupService _groupService;

        public TaskService(ApplicationDbContext context, IGroupService groupService)
        {
            _context = context;
            _groupService = groupService;
        }

        public async Task<TaskItem> CreateTaskAsync(TaskCreateDto dto, string userId)
        {
            if (!await _groupService.IsUserAdminAsync(dto.GroupId, userId))
                throw new UnauthorizedAccessException("Bạn không có quyền tạo nhiệm vụ trong nhóm này.");

            if (!string.IsNullOrEmpty(dto.AssignedToUserId))
            {
                var isValidAssignee = await _context.GroupMembers
                    .AnyAsync(gm => gm.GroupId == dto.GroupId && gm.UserId == dto.AssignedToUserId && gm.LeftAt == null)
                    || await _context.Groups
                        .AnyAsync(g => g.GroupId == dto.GroupId && g.CreatedByUserId == dto.AssignedToUserId);
                if (!isValidAssignee)
                    throw new InvalidOperationException("Người được giao không phải là thành viên hoặc người tạo nhóm.");
            }

            if (dto.PriorityId.HasValue && !await _context.Priorities.AnyAsync(p => p.PriorityId == dto.PriorityId))
                throw new InvalidOperationException("Ưu tiên không hợp lệ.");

            if (dto.Deadline.HasValue && dto.Deadline <= DateTime.UtcNow)
                throw new InvalidOperationException("Hạn chót phải lớn hơn thời điểm hiện tại.");

            var now = DateTime.UtcNow;

            var task = new TaskItem
            {
                Title = dto.Title,
                Description = dto.Description,
                Status = dto.Status,
                Deadline = dto.Deadline,
                AssignedToUserId = dto.AssignedToUserId,
                GroupId = dto.GroupId,
                PriorityId = dto.PriorityId,
                Tags = dto.Tags,
                CreatedAt = now,
                UpdatedAt = now,
                StatusChangedAt = now,
                CompletedAt = dto.Status == TaskItem.TaskStatus.Done ? now : null
            };

            _context.TaskItems.Add(task);
            await _context.SaveChangesAsync();
            return task;
        }

        public async Task<TaskItem> UpdateTaskAsync(TaskEditDto dto, string userId)
        {
            var task = await _context.TaskItems
                .Include(t => t.Group).ThenInclude(g => g.Members)
                .FirstOrDefaultAsync(t => t.TaskItemId == dto.TaskItemId);

            if (task == null)
                throw new KeyNotFoundException("Nhiệm vụ không tồn tại.");

            if (!await _groupService.IsUserAdminAsync(task.GroupId, userId))
                throw new UnauthorizedAccessException("Bạn không có quyền chỉnh sửa nhiệm vụ này.");

            if (!string.IsNullOrEmpty(dto.AssignedToUserId))
            {
                var isValidAssignee = await _context.GroupMembers
                    .AnyAsync(gm => gm.GroupId == task.GroupId && gm.UserId == dto.AssignedToUserId && gm.LeftAt == null)
                    || await _context.Groups
                        .AnyAsync(g => g.GroupId == task.GroupId && g.CreatedByUserId == dto.AssignedToUserId);
                if (!isValidAssignee)
                    throw new InvalidOperationException("Người được giao không phải là thành viên hoặc người tạo nhóm.");
            }

            if (dto.PriorityId.HasValue && !await _context.Priorities.AnyAsync(p => p.PriorityId == dto.PriorityId))
                throw new InvalidOperationException("Ưu tiên không hợp lệ.");

            if (dto.Deadline.HasValue && dto.Deadline <= DateTime.UtcNow)
                throw new InvalidOperationException("Hạn chót phải lớn hơn thời điểm hiện tại.");

            var now = DateTime.UtcNow;
            var previousStatus = task.Status;

            task.Title = dto.Title;
            task.Description = dto.Description;
            task.Status = dto.Status;
            task.Deadline = dto.Deadline;
            task.AssignedToUserId = dto.AssignedToUserId;
            task.PriorityId = dto.PriorityId;
            task.Tags = dto.Tags;
            task.UpdatedAt = now;

            if (previousStatus != dto.Status)
            {
                task.StatusChangedAt = now;
            }

            task.CompletedAt = dto.Status == TaskItem.TaskStatus.Done ? now : null;

            await _context.SaveChangesAsync();
            return task;
        }

        public async Task<bool> CanAccessTaskAsync(int taskId, string userId)
        {
            var task = await _context.TaskItems
                .Include(t => t.Group).ThenInclude(g => g.Members)
                .FirstOrDefaultAsync(t => t.TaskItemId == taskId);

            if (task == null)
                return false;

            return task.Group.Members.Any(m => m.UserId == userId && m.LeftAt == null) ||
                   task.Group.CreatedByUserId == userId;
        }

        public async Task DeleteTaskAsync(int taskId, string userId)
        {
            var task = await _context.TaskItems
                .Include(t => t.Group).ThenInclude(g => g.Members)
                .FirstOrDefaultAsync(t => t.TaskItemId == taskId);

            if (task == null)
                throw new KeyNotFoundException("Nhiệm vụ không tồn tại.");

            if (!await _groupService.IsUserAdminAsync(task.GroupId, userId))
                throw new UnauthorizedAccessException("Bạn không có quyền xóa nhiệm vụ này.");

            _context.TaskItems.Remove(task);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateTaskStatusAsync(int taskId, TaskItem.TaskStatus status, string userId)
        {
            var task = await _context.TaskItems
                .Include(t => t.Group).ThenInclude(g => g.Members)
                .FirstOrDefaultAsync(t => t.TaskItemId == taskId);

            if (task == null)
                throw new KeyNotFoundException("Nhiệm vụ không tồn tại.");

            if (!await CanAccessTaskAsync(taskId, userId))
                throw new UnauthorizedAccessException("Bạn không có quyền cập nhật trạng thái nhiệm vụ này.");

            var now = DateTime.UtcNow;
            var previousStatus = task.Status;

            task.Status = status;
            task.UpdatedAt = now;

            if (previousStatus != status)
            {
                task.StatusChangedAt = now;
            }

            task.CompletedAt = status == TaskItem.TaskStatus.Done ? now : null;

            await _context.SaveChangesAsync();
        }
    }
}
