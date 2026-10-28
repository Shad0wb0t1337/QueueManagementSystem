namespace QueueSystem.API.Models;

public class Department
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Counter> Counters { get; set; } = new List<Counter>();
    public ICollection<ManagerDepartment> ManagerDepartments { get; set; } = new List<ManagerDepartment>();
}
