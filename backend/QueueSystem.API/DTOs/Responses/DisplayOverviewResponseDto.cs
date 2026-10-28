namespace QueueSystem.API.DTOs.Responses;

public class DisplayOverviewResponseDto
{
    public List<DisplayTicketResponseDto> ActiveTickets { get; set; } = new List<DisplayTicketResponseDto>();
    public List<DisplayTicketResponseDto> RecentHistory { get; set; } = new List<DisplayTicketResponseDto>();
    public DateTime LastUpdatedAt { get; set; }
}
