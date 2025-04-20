using ROTA_API.Models;

namespace ROTA_API.DTOs
{
    public class LeaveRequestDto
    {
        public int LeaveRequestId { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeFirstName { get; set; } = string.Empty;
        public string EmployeeLastName { get; set; } = string.Empty;
        public int? TeamId { get; set; } // Employee's Team ID
        public string? TeamName { get; set; } // Employee's Team Name
        public int LeaveTypeId { get; set; }
        public string LeaveTypeName { get; set; } = string.Empty; // Include type name
        public DateTime LeaveStartDateTime { get; set; }
        public DateTime LeaveEndDateTime { get; set; }
        public string? Reason { get; set; }
        public LeaveStatus Status { get; set; } // Use the enum
        public string StatusString => Status.ToString(); // Helper for display
        public DateTime RequestedDate { get; set; }
        public int? ApproverUserId { get; set; }
        public string? ApproverUsername { get; set; } // Include approver's username if approved/rejected
        public DateTime? ApprovalDate { get; set; }
        public string? ApproverNotes { get; set; }
    }
}
