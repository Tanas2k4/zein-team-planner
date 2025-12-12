using System.ComponentModel.DataAnnotations;

namespace ZEIN_TeamPlanner.DTOs.GroupsDto
{
    public class GroupCreateDto
    {
        [Required(ErrorMessage = "* Group name cannot be empty")]
        [StringLength(100, ErrorMessage = "Group name cannot exceed 100 characters")]
        public string GroupName { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string Description { get; set; } = string.Empty;

        public List<string> MemberIds { get; set; } = new List<string>(); // IDs of users to add as members
    }
}