namespace ROTA_API.DTOs
{
    public class OnCallGapDto
    {
        public int RequiredShiftTypeId { get; set; } // Which type had the gap
        public string RequiredShiftTypeName { get; set; } = string.Empty;
        public DateTime GapStart { get; set; }
        public DateTime GapEnd { get; set; }
        public double DurationHours { get; set; }
    }
}
