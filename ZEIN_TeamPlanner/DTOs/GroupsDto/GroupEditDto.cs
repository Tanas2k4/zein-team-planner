using System.ComponentModel.DataAnnotations;

namespace ZEIN_TeamPlanner.DTOs.GroupsDto
{
    public class GroupEditDto
    {
        public int GroupId { get; set; }

        [Required(ErrorMessage = "* Không được để trống tên Group")]
        [StringLength(100, ErrorMessage = "Tên Group không được vượt quá 100 ký tự")]
        public string GroupName { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
        public string Description { get; set; } = string.Empty;

        public List<string> MemberIds { get; set; } = new List<string>(); // IDs of users to be members
    }
}