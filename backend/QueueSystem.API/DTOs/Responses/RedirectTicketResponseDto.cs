namespace QueueSystem.API.DTOs.Responses;

public class RedirectTicketResponseDto
{
    public int TicketId { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public int FromServiceTypeId { get; set; }
    public string FromServiceTypeName { get; set; } = string.Empty;
    public int ToServiceTypeId { get; set; }
    public string ToServiceTypeName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime QueueEnteredAt { get; set; }
    public string Message { get; set; } = string.Empty;
}
