using System.ComponentModel.DataAnnotations;
using ZEIN_TeamPlanner.Models;

namespace ZEIN_TeamPlanner.DTOs.Shared
{
    public class InviteMemberDto
    {
        [Required(ErrorMessage = "* Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "* Email không hợp lệ")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "* Vui lòng chọn vai trò")]
        public GroupRole Role { get; set; }

        [Required]
        public int GroupId { get; set; }
    }
}