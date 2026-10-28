namespace QueueSystem.API.DTOs.Responses;

public class CounterResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public List<int> SupportedServiceTypeIds { get; set; } = new List<int>();
    public List<SupportedServiceTypeResponseDto> SupportedServiceTypes { get; set; } = new List<SupportedServiceTypeResponseDto>();
}
