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
                throw new InvalidOperationException("Group name already exists.");

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
                throw new KeyNotFoundException("Group does not exist.");

            if (!await IsUserAdminAsync(dto.GroupId, userId))
                throw new UnauthorizedAccessException("You do not have permission to edit this group.");

            if (await _context.Groups.AnyAsync(g => g.GroupName == dto.GroupName && g.GroupId != dto.GroupId))
                throw new InvalidOperationException("Group name already exists.");

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
                throw new KeyNotFoundException("Group does not exist.");

            if (!await IsUserAdminAsync(groupId, adminId))
                throw new UnauthorizedAccessException("You do not have permission to remove members.");

            var member = group.Members.FirstOrDefault(m => m.UserId == memberId && m.LeftAt == null);
            if (member == null)
                throw new KeyNotFoundException("Member does not exist in the group.");

            // Do not allow removing yourself
            if (memberId == adminId)
                throw new InvalidOperationException("You cannot remove yourself from the group.");

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
                throw new KeyNotFoundException("Group does not exist.");

            if (!await IsUserAdminAsync(groupId, adminId))
                throw new UnauthorizedAccessException("You do not have permission to delete the group.");

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
                throw new KeyNotFoundException("Group does not exist.");

            var member = group.Members.FirstOrDefault(m => m.UserId == userId && m.LeftAt == null);
            if (member == null)
                throw new KeyNotFoundException("You are not a member of this group.");

            if (member.Role == GroupRole.Admin && group.Members.Count(m => m.Role == GroupRole.Admin && m.LeftAt == null) == 1)
                throw new InvalidOperationException("You are the only Admin and cannot leave the group. Please assign another Admin before leaving.");

            member.LeftAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}