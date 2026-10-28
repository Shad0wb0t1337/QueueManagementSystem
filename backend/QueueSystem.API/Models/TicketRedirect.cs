namespace QueueSystem.API.Models;

public class TicketRedirect
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public int FromServiceTypeId { get; set; }
    public int ToServiceTypeId { get; set; }
    public int? FromCounterId { get; set; }
    public int RedirectedByOperatorId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public Ticket? Ticket { get; set; }
    public ServiceType? FromServiceType { get; set; }
    public ServiceType? ToServiceType { get; set; }
    public Counter? FromCounter { get; set; }
    public Operator? RedirectedByOperator { get; set; }
}
