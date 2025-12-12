using System.ComponentModel.DataAnnotations;
using ZEIN_TeamPlanner.DTOs.Shared;
using ZEIN_TeamPlanner.Models;
using ZEIN_TeamPlanner.Shared;

namespace ZEIN_TeamPlanner.DTOs.TasksDto
{
    public class TaskCreateDto
    {
        [Required(ErrorMessage = "* Task title cannot be empty")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string Title { get; set; } = "";

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string Description { get; set; } = "";

        [Required(ErrorMessage = "* Please select status")]
        public TaskItem.TaskStatus Status { get; set; } = TaskItem.TaskStatus.ToDo;

        [FutureDate(ErrorMessage = "* Deadline must be greater than current time")]
        [DataType(DataType.Date)]
        public DateTime? Deadline { get; set; }

        public string? AssignedToUserId { get; set; }

        [Required(ErrorMessage = "* Task must belong to a group")]
        public int GroupId { get; set; }

        public int? PriorityId { get; set; }

        [StringLength(500, ErrorMessage = "Tags cannot exceed 500 characters")]
        public string? Tags { get; set; }
    }
}