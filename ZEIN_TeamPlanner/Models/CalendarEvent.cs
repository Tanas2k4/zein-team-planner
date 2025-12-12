using System.ComponentModel.DataAnnotations;

namespace ZEIN_TeamPlanner.Models
{
    public class CalendarEvent
    {
        public int CalendarEventId { get; set; }

        [Required(ErrorMessage = "* Event title cannot be empty")]
        [StringLength(200)]
        public string Title { get; set; } = "";

        [StringLength(1000)]
        public string Description { get; set; } = "";

        [Required]
        public DateTimeOffset StartTime { get; set; }

        public DateTimeOffset? EndTime { get; set; }

        [Required]
        [StringLength(100)]
        public string TimeZoneId { get; set; } = "UTC"; // IANA time zone, e.g., "Asia/Ho_Chi_Minh"

        public int GroupId { get; set; }

        public Group Group { get; set; } = null!;

        public bool IsAllDay { get; set; } = false;

        [StringLength(500)]
        public string? RecurrenceRule { get; set; }

        public enum EventType { Meeting, Deadline, Reminder }
        public EventType Type { get; set; } = EventType.Meeting;
    }
}