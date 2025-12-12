using System.ComponentModel.DataAnnotations;
using ZEIN_TeamPlanner.Models;

namespace ZEIN_TeamPlanner.DTOs.Shared
{
    public class InviteMemberDto
    {

        [Required(ErrorMessage = "* Please enter email")]
        [EmailAddress(ErrorMessage = "* Email is not valid")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "* Please select role")]
        public GroupRole Role { get; set; }

        [Required]
        public int GroupId { get; set; }
    }
}