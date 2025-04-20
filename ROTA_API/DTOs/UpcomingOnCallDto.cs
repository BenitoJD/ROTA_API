namespace ROTA_API.DTOs
{
    public class UpcomingOnCallDto
    {
        public DateTime Date { get; set; } // The specific date
        public List<OnCallAssignmentDto> Assignments { get; set; } = new();
    }
}
