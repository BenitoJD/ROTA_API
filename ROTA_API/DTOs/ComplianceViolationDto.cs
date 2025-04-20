namespace ROTA_API.DTOs
{
    public class ComplianceViolationDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string ViolationType { get; set; } // e.g., "MinRest", "MaxHours"
        public DateTime ViolationDate { get; set; } // When the violation occurred
        public double ActualValue { get; set; } // e.g., Actual rest hours, Actual hours worked
        public double RuleValue { get; set; } // e.g., Required min rest, Max allowed hours
    }
}
