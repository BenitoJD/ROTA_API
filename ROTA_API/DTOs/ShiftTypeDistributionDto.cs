namespace ROTA_API.DTOs
{
    public class ShiftTypeDistributionDto
    {
        public int ShiftTypeId { get; set; }
        public string ShiftTypeName { get; set; } = string.Empty;
        public bool IsOnCall { get; set; } // Include this flag
        public int ShiftCount { get; set; }
        public double PercentageOfTotal { get; set; } // Calculated after fetching all counts
    }
}
