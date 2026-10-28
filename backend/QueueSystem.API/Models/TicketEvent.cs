namespace QueueSystem.API.Models;

public class TicketEvent
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public TicketEventType EventType { get; set; }
    public int? OperatorId { get; set; }
    public int? CounterId { get; set; }
    public int ServiceTypeId { get; set; }
    public int? FromServiceTypeId { get; set; }
    public int? ToServiceTypeId { get; set; }
    public TicketStatus? PreviousStatus { get; set; }
    public TicketStatus? NewStatus { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Details { get; set; } = string.Empty;

    public Ticket? Ticket { get; set; }
    public Operator? Operator { get; set; }
    public Counter? Counter { get; set; }
    public ServiceType? ServiceType { get; set; }
    public ServiceType? FromServiceType { get; set; }
    public ServiceType? ToServiceType { get; set; }
}
