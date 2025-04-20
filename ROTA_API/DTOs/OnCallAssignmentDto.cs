namespace ROTA_API.DTOs
{
    public class OnCallAssignmentDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public int? TeamId { get; set; }
        public string? TeamName { get; set; }
        public int ShiftTypeId { get; set; }
        public string ShiftTypeName { get; set; } = string.Empty; // e.g., "On-Call Primary", "On-Call Secondary"
        public DateTime ShiftStartDateTime { get; set; }
        public DateTime ShiftEndDateTime { get; set; }
    }
}
