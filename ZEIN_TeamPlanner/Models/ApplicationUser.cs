using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace ZEIN_TeamPlanner.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; } // Nullable to avoid errors if no value is provided
        public string? Address { get; set; } // Nullable if not required
        public DateTime? DateOfBirth { get; set; } // Nullable as it's not required
        public string TimeZoneId { get; set; } = "UTC"; // For scheduling across time zones
        public bool IsOnline { get; set; } = false; // Presence status
        public string? StatusMessage { get; set; } // Custom status like "Busy" or "Available"
        public string? Department { get; set; } // Useful for organization-wide apps
        public bool ReceiveEmailNotifications { get; set; } = true; // Notification preference

        // Thuộc tính tính toán Age từ DateOfBirth
        public int? Age
        {
            get
            {
                if (DateOfBirth.HasValue)
                {
                    var today = DateTime.UtcNow;
                    int age = today.Year - DateOfBirth.Value.Year;
                    if (today < DateOfBirth.Value.AddYears(age))
                        age--;
                    return age;
                }
                return null;
            }
        }

        // Đặt giá trị mặc định cho CreateAT
        [Required]
        public DateTime CreateAT { get; set; } = DateTime.UtcNow;
        // lưu đường dẫn avatar
        public string AvatarUrl { get; set; } = "/images/default-avatar.png"; 
        public ICollection<GroupMember>? GroupMemberships { get; set; }
        public ICollection<TaskItem>? AssignedTasks { get; set; }
        public ICollection<FileAttachment>? FileAttachments { get; set; }
    }
}
