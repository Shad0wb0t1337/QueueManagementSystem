namespace QueueSystem.API.DTOs.Responses;

public class ActiveTicketResponseDto
{
    public int TicketId { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public int? CounterId { get; set; }
    public string CounterName { get; set; } = string.Empty;
    public int ServiceTypeId { get; set; }
    public string ServiceTypeName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
