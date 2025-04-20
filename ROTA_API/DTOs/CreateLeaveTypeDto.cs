using System.ComponentModel.DataAnnotations;

namespace ROTA_API.DTOs
{
    public class CreateLeaveTypeDto
    {
        [Required]
        [MaxLength(100)]
        public string LeaveTypeName { get; set; } = string.Empty;

        // Default to true, but allow admin to override if needed
        public bool RequiresApproval { get; set; } = true;

        [MaxLength(500)]
        public string? Description { get; set; }
    }
}

