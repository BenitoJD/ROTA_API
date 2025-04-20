namespace ROTA_API.DTOs
{
    public class ShiftCoverageDto
    {
        public DateTime Date { get; set; } // Could be daily, weekly, etc.
        public int? TeamId { get; set; } // Grouping key
        public string? TeamName { get; set; } // Grouping key
        public int ShiftCount { get; set; } // Number of shifts scheduled
        public int UniqueEmployeeCount { get; set; } // Number of distinct employees scheduled
                                                     // Could add specific shift type counts if needed
    }
}
