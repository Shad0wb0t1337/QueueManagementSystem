namespace QueueSystem.API.DTOs.Requests;

public class CreateDepartmentRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class UpdateDepartmentRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class UpdateManagerDepartmentsRequestDto
{
    public List<int> DepartmentIds { get; set; } = new List<int>();
}
