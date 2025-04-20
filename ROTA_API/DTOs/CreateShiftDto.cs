using System.ComponentModel.DataAnnotations;

namespace ROTA_API.DTOs
{
    public class CreateShiftDto
    {
        [Required]
        public int EmployeeId { get; set; }

        public int? ShiftTypeId { get; set; } // Optional: Can be null if ShiftTypes allow null FK

        [Required]
        public DateTime ShiftStartDateTime { get; set; }

        [Required]
        // Add custom validation later to ensure End > Start
        public DateTime ShiftEndDateTime { get; set; }

        public string? Notes { get; set; }
    }
}
