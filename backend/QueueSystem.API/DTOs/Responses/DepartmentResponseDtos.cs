namespace QueueSystem.API.DTOs.Responses;

public class DepartmentResponseDto
{
    public int DepartmentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CounterCount { get; set; }
    public int ManagerCount { get; set; }
}

public class ManagerUserResponseDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public List<int> DepartmentIds { get; set; } = new List<int>();
}

public class ManagerDepartmentResponseDto
{
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
}
