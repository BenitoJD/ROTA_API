using System.ComponentModel.DataAnnotations;

namespace ROTA_API.Models
{
    public class LeaveType
    {
        public int LeaveTypeId { get; set; } // PK

        [Required]
        [MaxLength(100)]
        public string LeaveTypeName { get; set; } = string.Empty;

        public bool RequiresApproval { get; set; } = true;
        public string? Description { get; set; }

        // Navigation Property
        public virtual ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
    }
}
