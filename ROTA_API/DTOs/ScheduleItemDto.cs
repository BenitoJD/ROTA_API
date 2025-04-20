namespace ROTA_API.DTOs
{
    public class ScheduleItemDto
    {
        public string ItemType { get; set; } = string.Empty; // "Shift", "Leave"
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public string Description { get; set; } = string.Empty; // e.g., Shift Type Name, Leave Type Name
        public string? Notes { get; set; } // Include shift notes?
        public int? ReferenceId { get; set; } // ShiftId or LeaveRequestId
        // Optional: Add status for leave if needed
        public string? Status { get; set; } // e.g. "Approved", "Pending"
    }
}
