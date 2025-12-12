namespace ZEIN_TeamPlanner.Models
{
    public class FileAttachment
    {
        public int Id { get; set; }
        public string FileName { get; set; } = null!;
        public string? FileUrl { get; set; } 
        public string? EntityType { get; set; } // "TaskItem", "Message"
        public int EntityId { get; set; } // Related entity ID
        public string? UserId { get; set; } 
        public ApplicationUser User { get; set; } = null!;
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}
