namespace ROTA_API.DTOs
{
    public class LeaveSummaryDto
    {
        // Grouping keys (could be one or more)
        public int? GroupingId { get; set; } // e.g., LeaveTypeId, TeamId, EmployeeId
        public string GroupingName { get; set; } = string.Empty; // e.g., LeaveTypeName, TeamName, EmployeeName
                                                                 // Metrics
        public int LeaveRequestCount { get; set; }
        public double TotalLeaveDays { get; set; } // Calculate duration for each approved request

        public string GroupingDimension { get; set; } = string.Empty; // e.g., "LeaveType", "Team", "Employee"

    }
}
