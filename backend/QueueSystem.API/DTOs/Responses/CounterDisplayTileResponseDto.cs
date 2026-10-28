namespace QueueSystem.API.DTOs.Responses;

public class CounterDisplayTileResponseDto
{
    public int CounterId { get; set; }
    public string CounterName { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public string CounterStatus { get; set; } = string.Empty;
    public int? CurrentTicketId { get; set; }
    public string? CurrentTicketNumber { get; set; }
    public string? CurrentTicketStatus { get; set; }
    public int? ServiceTypeId { get; set; }
    public string? ServiceTypeName { get; set; }
    public DateTime LastUpdatedAt { get; set; }
}
