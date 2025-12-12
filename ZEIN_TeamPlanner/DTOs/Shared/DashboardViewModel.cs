using ZEIN_TeamPlanner.Models;

namespace ZEIN_TeamPlanner.DTOs.Shared
{
    public class DashboardViewModel
    {
        public int WeeklyProgress { get; set; }
        public string TotalHoursWorked { get; set; }
        public int ProjectCount { get; set; }
        public int CompletedProjectCount { get; set; }
        public int TaskWorkingCount { get; set; } // No longer in use
        public List<DateTime> ProjectProgressDates { get; set; }
        public List<int> AchievedProjects { get; set; } // Done
        public List<int> TargetProjects { get; set; }
        public List<int> ToDoProjects { get; set; } // Thêm cho ToDo
        public List<int> InProgressProjects { get; set; } // Thêm cho InProgress
        public List<int> BlockedProjects { get; set; } // Thêm cho Blocked
        public List<ApplicationUser> Collaborators { get; set; }
        public List<TaskItem> RecentTasks { get; set; } // Thêm danh sách task gần đây

        public DashboardViewModel()
        {
            ProjectProgressDates = new List<DateTime>();
            AchievedProjects = new List<int>();
            TargetProjects = new List<int>();
            ToDoProjects = new List<int>();
            InProgressProjects = new List<int>();
            BlockedProjects = new List<int>();
            Collaborators = new List<ApplicationUser>();
            RecentTasks = new List<TaskItem>();
        }
    }
}