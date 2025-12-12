namespace ZEIN_TeamPlanner.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public string UserId { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;
        public string? Message { get; set; }
        public string? Type { get; set; }  // "TaskAssigned", "GroupInvite"
        public string? RelatedEntityId { get; set; } // E.g., TaskItemId
        public string? RelatedEntityType { get; set; } // E.g., "TaskItem"
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
