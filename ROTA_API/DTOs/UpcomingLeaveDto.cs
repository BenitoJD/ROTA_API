namespace ROTA_API.DTOs
{
    public class UpcomingLeaveDto
    {
        public int LeaveRequestId { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public int? TeamId { get; set; }
        public string? TeamName { get; set; }
        public int LeaveTypeId { get; set; }
        public string LeaveTypeName { get; set; } = string.Empty;
        public DateTime LeaveStartDateTime { get; set; }
        public DateTime LeaveEndDateTime { get; set; }
    }
}
