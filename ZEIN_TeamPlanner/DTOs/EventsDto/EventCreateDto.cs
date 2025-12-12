using System.ComponentModel.DataAnnotations;
using ZEIN_TeamPlanner.Models;
using ZEIN_TeamPlanner.Shared;

namespace ZEIN_TeamPlanner.DTOs.EventsDto
{
    public class EventCreateDto
    {
        [Required(ErrorMessage = "* Event title cannot be empty")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string Title { get; set; } = "";

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string Description { get; set; } = "";

        [Required(ErrorMessage = "* Please select start time")]
        [FutureDate(ErrorMessage = "* Start time must be greater than current time")] //check constraint for future date
        [DataType(DataType.DateTime)]
        public DateTimeOffset StartTime { get; set; }

        [EndTimeGreaterThanStartTime("StartTime", ErrorMessage = "* End time must be greater than start time")] //check constraint for end time greater than start time
        [DataType(DataType.DateTime)]
        public DateTimeOffset? EndTime { get; set; }

        [Required(ErrorMessage = "* Please select time zone")]
        [StringLength(100)]
        public string TimeZoneId { get; set; } = "UTC";

        [Required(ErrorMessage = "* Event must belong to a group")]
        public int GroupId { get; set; }

        public bool IsAllDay { get; set; } = false;

        [StringLength(500, ErrorMessage = "Recurrence rule cannot exceed 500 characters")]
        public string? RecurrenceRule { get; set; }

        [Required(ErrorMessage = "* Please select event type")]
        public CalendarEvent.EventType Type { get; set; } = CalendarEvent.EventType.Meeting;
    }
}