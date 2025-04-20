using System.ComponentModel.DataAnnotations;

namespace ROTA_API.DTOs
{
    public class UpdateLeaveTypeDto
    {
        [Required]
        [MaxLength(100)]
        public string LeaveTypeName { get; set; } = string.Empty;

        [Required] // Make sure this is always provided on update
        public bool RequiresApproval { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }
    }
}
