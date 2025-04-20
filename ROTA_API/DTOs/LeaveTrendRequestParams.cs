namespace ROTA_API.DTOs
{
    public enum TrendPeriod
    {
        Monthly,
        Quarterly,
        Yearly
    }

    public class LeaveTrendRequestParams
    {
        public DateTime? StartDate { get; set; } // Overall range start
        public DateTime? EndDate { get; set; }   // Overall range end
        public TrendPeriod Period { get; set; } = TrendPeriod.Monthly; // Default granularity
        public int? LeaveTypeId { get; set; } // Optional filter
        public int? TeamId { get; set; }      // Optional filter
        public int? EmployeeId { get; set; }  // Optional filter
    }
}
