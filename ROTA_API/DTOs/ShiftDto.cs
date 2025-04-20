namespace ROTA_API.DTOs
{
    public class ShiftDto
    {
        public int ShiftId { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeFirstName { get; set; } = string.Empty; // Include names for display
        public string EmployeeLastName { get; set; } = string.Empty;
        public int? TeamId { get; set; } // Employee's Team ID
        public string? TeamName { get; set; } // Employee's Team Name
        public int? ShiftTypeId { get; set; }
        public string? ShiftTypeName { get; set; } // Include type name
        public bool IsOnCall { get; set; } // Include flag
        public DateTime ShiftStartDateTime { get; set; }
        public DateTime ShiftEndDateTime { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
