using System.ComponentModel.DataAnnotations;

namespace ROTA_API.DTOs
{
    public class UpdateLeaveRequestDto
    {
        [Required]
        public int LeaveTypeId { get; set; }
        [Required]
        public DateTime LeaveStartDateTime { get; set; }
        [Required]
        public DateTime LeaveEndDateTime { get; set; }
        [MaxLength(500)]
        public string? Reason { get; set; }
        // Should not allow changing EmployeeId or Status here
    }
}
