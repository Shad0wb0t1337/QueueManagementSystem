namespace QueueSystem.API.Models;

public class Operator
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int CounterId { get; set; }
    public bool IsActive { get; set; } = true;

    public Counter? Counter { get; set; }
    public AppUser? AppUser { get; set; }
}
