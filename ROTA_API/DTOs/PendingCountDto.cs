namespace ROTA_API.DTOs
{
    public class PendingCountDto
    {
        public int Count { get; set; }
        // Optionally add TeamId/TeamName if the count is filtered by team
        public int? TeamId { get; set; }
        public string? TeamName { get; set; }
    }
}
