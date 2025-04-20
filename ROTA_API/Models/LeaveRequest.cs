using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ROTA_API.Models
{
    public class LeaveRequest
    {
        public int LeaveRequestId { get; set; } // PK

        // Foreign Key to Employee
        public int EmployeeId { get; set; }
        [ForeignKey("EmployeeId")]
        public virtual Employee Employee { get; set; } = null!;

        // Foreign Key to LeaveType
        public int LeaveTypeId { get; set; }
        [ForeignKey("LeaveTypeId")]
        public virtual LeaveType LeaveType { get; set; } = null!;

        [Required]
        public DateTime LeaveStartDateTime { get; set; }
        [Required]
        public DateTime LeaveEndDateTime { get; set; }

        public string? Reason { get; set; }

        [Required]
        public LeaveStatus Status { get; set; } = LeaveStatus.Pending;

        public DateTime RequestedDate { get; set; } = DateTime.UtcNow;

        // Foreign Key for Approver
        public int? ApproverUserId { get; set; } // Nullable
        [ForeignKey("ApproverUserId")]
        public virtual User? ApproverUser { get; set; } // Nullable navigation property

        public DateTime? ApprovalDate { get; set; }
        public string? ApproverNotes { get; set; }

        // Auditing Foreign Keys
        public int? CreatedByUserId { get; set; }
        [ForeignKey("CreatedByUserId")]
        public virtual User? CreatedByUser { get; set; }

        public int? UpdatedByUserId { get; set; }
        [ForeignKey("UpdatedByUserId")]
        public virtual User? UpdatedByUser { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Track when record created in DB
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow; // Track last update
    }
}
