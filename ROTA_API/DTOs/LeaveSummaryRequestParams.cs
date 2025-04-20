namespace ROTA_API.DTOs
{
    public enum LeaveSummaryGrouping
    {
        None, // Overall summary
        LeaveType,
        Team,
        Employee
    }
    public class LeaveSummaryRequestParams
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? TeamId { get; set; } // Filter by team
        public int? EmployeeId { get; set; } // Filter by employee
        public int? LeaveTypeId { get; set; } // Filter by type
        public LeaveSummaryGrouping GroupBy { get; set; } = LeaveSummaryGrouping.LeaveType; // Default grouping
    }
}
