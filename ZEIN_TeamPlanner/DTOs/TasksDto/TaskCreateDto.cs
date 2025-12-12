using System.ComponentModel.DataAnnotations;
using ZEIN_TeamPlanner.DTOs.Shared;
using ZEIN_TeamPlanner.Models;
using ZEIN_TeamPlanner.Shared;

namespace ZEIN_TeamPlanner.DTOs.TasksDto
{
    public class TaskCreateDto
    {
        [Required(ErrorMessage = "* Không được để trống tiêu đề nhiệm vụ")]
        [StringLength(200, ErrorMessage = "Tiêu đề không được vượt quá 200 ký tự")]
        public string Title { get; set; } = "";

        [StringLength(1000, ErrorMessage = "Mô tả không được vượt quá 1000 ký tự")]
        public string Description { get; set; } = "";

        [Required(ErrorMessage = "* Vui lòng chọn trạng thái")]
        public TaskItem.TaskStatus Status { get; set; } = TaskItem.TaskStatus.ToDo;

        [FutureDate(ErrorMessage = "* Hạn chót phải lớn hơn thời điểm hiện tại")]
        [DataType(DataType.Date)]
        public DateTime? Deadline { get; set; }

        public string? AssignedToUserId { get; set; }

        [Required(ErrorMessage = "* Nhiệm vụ phải thuộc một group")]
        public int GroupId { get; set; }

        public int? PriorityId { get; set; }

        [StringLength(500, ErrorMessage = "Tags không được vượt quá 500 ký tự")]
        public string? Tags { get; set; }
    }
}