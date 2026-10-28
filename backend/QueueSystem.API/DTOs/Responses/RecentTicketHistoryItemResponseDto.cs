namespace QueueSystem.API.DTOs.Responses;

public class RecentTicketHistoryItemResponseDto
{
    public int TicketId { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string TicketStatus { get; set; } = string.Empty;
    public int ServiceTypeId { get; set; }
    public string ServiceTypeName { get; set; } = string.Empty;
    public int? CounterId { get; set; }
    public string CounterName { get; set; } = string.Empty;
    public DateTime? CompletedAt { get; set; }
    public DateTime? RecallRequestedAt { get; set; }
    public DateTime LastUpdatedAt { get; set; }
    public bool CanRecall { get; set; }
    public string RecallMode { get; set; } = string.Empty;
}
