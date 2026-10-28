namespace QueueSystem.API.Models;

public class Counter
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public CounterStatus Status { get; set; } = CounterStatus.Active;
    public int DepartmentId { get; set; } = 1;

    public Department? Department { get; set; }
    public ICollection<CounterServiceType> CounterServiceTypes { get; set; } = new List<CounterServiceType>();
    public ICollection<Operator> Operators { get; set; } = new List<Operator>();
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
