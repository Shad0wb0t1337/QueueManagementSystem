namespace QueueSystem.API.Models;

public class ManagerDepartment
{
    public int AppUserId { get; set; }
    public int DepartmentId { get; set; }

    public AppUser? AppUser { get; set; }
    public Department? Department { get; set; }
}
