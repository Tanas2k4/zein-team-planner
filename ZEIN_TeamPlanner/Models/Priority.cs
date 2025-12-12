namespace ZEIN_TeamPlanner.Models
{
    public class Priority
    {
        public int PriorityId { get; set; }
        public string Name { get; set; } = null!; // "High", "Medium", "Low"
        public int Weight { get; set; } // For sorting (e.g., 1=High, 3=Low)
    }
}
