namespace ROTA_API.DTOs
{
    public class TeamAvailabilityDto
    {
        public int TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public DateTime PeriodStart { get; set; } // Start date of the period checked
        public DateTime PeriodEnd { get; set; }   // End date of the period checked
        public int TotalActiveTeamMembers { get; set; } // Count of active employees IN the team
        public int MembersOnShiftCount { get; set; } // Count of distinct team members with shifts overlapping the period
        public int MembersOnLeaveCount { get; set; } // Count of distinct team members with APPROVED leave overlapping the period
        public int MembersPotentiallyAvailable { get; set; } //


    }
}
