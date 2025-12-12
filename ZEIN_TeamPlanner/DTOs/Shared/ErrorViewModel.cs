namespace ZEIN_TeamPlanner.DTOs.Shared
{
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
        public string? ErrorMessage { get; set; } // User-friendly message
        public int? StatusCode { get; set; } // HTTP status code (e.g., 404, 500)
    }
}
