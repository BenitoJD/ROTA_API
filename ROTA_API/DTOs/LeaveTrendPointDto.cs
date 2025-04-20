namespace ROTA_API.DTOs
{
    public class LeaveTrendPointDto
    {
        public string PeriodLabel { get; set; } = string.Empty; // e.g., "2024-01", "2024-Q1"
        public DateTime PeriodStart { get; set; } // Start date of the period (e.g., first day of month/quarter)
        public DateTime PeriodEnd { get; set; }   // End date of the period (e.g., last day of month/quarter)
        public int LeaveRequestCount { get; set; }
        public double TotalLeaveDays { get; set; }
        // Optional: Add LeaveTypeId/Name if grouping within the trend
        public int? LeaveTypeId { get; set; }
        public string? LeaveTypeName { get; set; }
    }
}
