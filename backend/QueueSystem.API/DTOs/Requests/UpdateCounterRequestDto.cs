using QueueSystem.API.Models;

namespace QueueSystem.API.DTOs.Requests;

public class UpdateCounterRequestDto
{
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public int DepartmentId { get; set; }
    public List<int> ServiceTypeIds { get; set; } = new List<int>();
    public string Status { get; set; } = CounterStatus.Active.ToString();
}
