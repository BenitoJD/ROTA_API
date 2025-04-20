namespace ROTA_API.DTOs
{
    public class OnCallHoursSummaryDto
    {
        public int? GroupingId { get; set; } // EmployeeId or TeamId
        public string GroupingName { get; set; } = string.Empty; // Employee Name or Team Name
        public double TotalOnCallHours { get; set; }
    }
}
