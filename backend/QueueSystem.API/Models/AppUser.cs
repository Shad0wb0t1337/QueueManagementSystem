namespace QueueSystem.API.Models;

public class AppUser
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public int? OperatorId { get; set; }

    public Operator? Operator { get; set; }
    public ICollection<ManagerDepartment> ManagerDepartments { get; set; } = new List<ManagerDepartment>();
}
