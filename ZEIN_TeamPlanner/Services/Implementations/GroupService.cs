using Microsoft.EntityFrameworkCore;
using TeamPlanner.Data;
using ZEIN_TeamPlanner.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using ZEIN_TeamPlanner.Services.Interfaces;
using ZEIN_TeamPlanner.DTOs.GroupsDto;

namespace ZEIN_TeamPlanner.Services.Implementations
{
    public class GroupService : IGroupService
    {
        private readonly ApplicationDbContext _context;

        public GroupService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> CanAccessGroupAsync(int groupId, string userId)
        {
            var group = await _context.Groups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.GroupId == groupId);

            if (group == null)
                return false;

            return group.CreatedByUserId == userId ||
                   group.Members.Any(m => m.UserId == userId && m.LeftAt == null);
        }

        public async Task<bool> IsUserAdminAsync(int groupId, string userId)
        {
            var group = await _context.Groups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.GroupId == groupId);

            return group != null &&
                   (group.CreatedByUserId == userId ||
                    group.Members.Any(m => m.UserId == userId && m.Role == GroupRole.Admin && m.LeftAt == null));
        }

        public async Task<Group> CreateGroupAsync(GroupCreateDto dto, string userId)
        {
            if (await _context.Groups.AnyAsync(g => g.GroupName == dto.GroupName))
                throw new InvalidOperationException("Tên nhóm đã tồn tại.");

            var group = new Group
            {
                GroupName = dto.GroupName,
                Description = dto.Description,
                CreatedByUserId = userId,
                CreateAt = DateTime.UtcNow
            };

            _context.Groups.Add(group);
            await _context.SaveChangesAsync();

            var groupMember = new GroupMember
            {
                GroupId = group.GroupId,
                UserId = userId,
                Role = GroupRole.Admin,
                JoinedAt = DateTime.UtcNow
            };

            _context.GroupMembers.Add(groupMember);

            if (dto.MemberIds != null && dto.MemberIds.Any())
            {
                foreach (var memberId in dto.MemberIds.Where(id => id != userId))
                {
                    if (await _context.Users.AnyAsync(u => u.Id == memberId))
                    {
                        var member = new GroupMember
                        {
                            GroupId = group.GroupId,
                            UserId = memberId,
                            Role = GroupRole.Member,
                            JoinedAt = DateTime.UtcNow
                        };
                        _context.GroupMembers.Add(member);
                    }
                }
            }

            await _context.SaveChangesAsync();
            return group;
        }

        public async Task<Group> UpdateGroupAsync(GroupEditDto dto, string userId)
        {
            var group = await _context.Groups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.GroupId == dto.GroupId);

            if (group == null)
                throw new KeyNotFoundException("Nhóm không tồn tại.");

            if (!await IsUserAdminAsync(dto.GroupId, userId))
                throw new UnauthorizedAccessException("Bạn không có quyền chỉnh sửa nhóm này.");

            if (await _context.Groups.AnyAsync(g => g.GroupName == dto.GroupName && g.GroupId != dto.GroupId))
                throw new InvalidOperationException("Tên nhóm đã tồn tại.");

            // Update group details only
            group.GroupName = dto.GroupName;
            group.Description = dto.Description;

            await _context.SaveChangesAsync();
            return group;
        }

        public async Task RemoveMemberAsync(int groupId, string memberId, string adminId)
        {
            var group = await _context.Groups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.GroupId == groupId);

            if (group == null)
                throw new KeyNotFoundException("Nhóm không tồn tại.");

            if (!await IsUserAdminAsync(groupId, adminId))
                throw new UnauthorizedAccessException("Bạn không có quyền xóa thành viên.");

            var member = group.Members.FirstOrDefault(m => m.UserId == memberId && m.LeftAt == null);
            if (member == null)
                throw new KeyNotFoundException("Thành viên không tồn tại trong nhóm.");

            // Không cho phép xóa chính mình
            if (memberId == adminId)
                throw new InvalidOperationException("Không thể xóa chính bạn khỏi nhóm.");

            member.LeftAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteGroupAsync(int groupId, string adminId)
        {
            var group = await _context.Groups
                .Include(g => g.Members)
                .Include(g => g.Tasks)
                .Include(g => g.Events)
                .FirstOrDefaultAsync(g => g.GroupId == groupId);

            if (group == null)
                throw new KeyNotFoundException("Nhóm không tồn tại.");

            if (!await IsUserAdminAsync(groupId, adminId))
                throw new UnauthorizedAccessException("Bạn không có quyền xóa nhóm.");

            _context.GroupMembers.RemoveRange(group.Members);
            _context.TaskItems.RemoveRange(group.Tasks);
            _context.CalendarEvents.RemoveRange(group.Events);
            _context.Groups.Remove(group);

            await _context.SaveChangesAsync();
        }

        public async Task LeaveGroupAsync(int groupId, string userId)
        {
            var group = await _context.Groups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.GroupId == groupId);

            if (group == null)
                throw new KeyNotFoundException("Nhóm không tồn tại.");

            var member = group.Members.FirstOrDefault(m => m.UserId == userId && m.LeftAt == null);
            if (member == null)
                throw new KeyNotFoundException("Bạn không phải là thành viên của nhóm này.");

            if (member.Role == GroupRole.Admin && group.Members.Count(m => m.Role == GroupRole.Admin && m.LeftAt == null) == 1)
                throw new InvalidOperationException("Bạn là Admin duy nhất, không thể rời nhóm. Vui lòng chỉ định Admin khác trước khi rời.");

            member.LeftAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}