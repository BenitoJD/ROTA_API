using System.ComponentModel.DataAnnotations;

namespace ROTA_API.DTOs
{
    public class CreateLeaveRequestDto
    {
      
        [Required]
        public int EmployeeId { get; set; }

        [Required]
        public int LeaveTypeId { get; set; }

        [Required]
        public DateTime LeaveStartDateTime { get; set; }

        [Required]
        public DateTime LeaveEndDateTime { get; set; }

        [MaxLength(500)]
        public string? Reason { get; set; }
    }
}
