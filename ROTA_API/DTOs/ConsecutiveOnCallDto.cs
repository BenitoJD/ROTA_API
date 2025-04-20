namespace ROTA_API.DTOs
{
    public class ConsecutiveOnCallDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public int ConsecutiveCount { get; set; } // Number of consecutive shifts/periods
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
    }
}
