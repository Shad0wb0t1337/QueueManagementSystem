namespace QueueSystem.API.Models;

public class Ticket
{
    public int Id { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public int ServiceTypeId { get; set; }
    public int? CounterId { get; set; }
    public TicketStatus Status { get; set; } = TicketStatus.Waiting;
    public DateTime CreatedAt { get; set; }
    public DateTime QueueEnteredAt { get; set; }
    public DateTime? CalledAt { get; set; }
    public DateTime? ServedAt { get; set; }
    public DateTime? RecallRequestedAt { get; set; }

    public ServiceType? ServiceType { get; set; }
    public Counter? Counter { get; set; }
    public ICollection<TicketRedirect> Redirects { get; set; } = new List<TicketRedirect>();
}
