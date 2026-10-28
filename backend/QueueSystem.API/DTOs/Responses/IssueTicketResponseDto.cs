namespace QueueSystem.API.DTOs.Responses;

public class IssueTicketResponseDto
{
    public int TicketId { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public int ServiceTypeId { get; set; }
    public string ServiceTypeName { get; set; } = string.Empty;
    public int QueuePosition { get; set; }
    public int EstimatedWaitMinutes { get; set; }
    public string Status { get; set; } = string.Empty;
}
