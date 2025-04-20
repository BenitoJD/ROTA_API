using System.ComponentModel.DataAnnotations;

namespace ROTA_API.DTOs
{
    public class UpdateShiftDto
    {
        [Required]
        public int EmployeeId { get; set; } // Allow changing employee? Or keep fixed? Decide based on need.

        public int? ShiftTypeId { get; set; }

        [Required]
        public DateTime ShiftStartDateTime { get; set; }

        [Required]
        public DateTime ShiftEndDateTime { get; set; }

        public string? Notes { get; set; }
    }
}
