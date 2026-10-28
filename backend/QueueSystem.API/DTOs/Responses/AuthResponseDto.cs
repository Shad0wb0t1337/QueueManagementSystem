namespace QueueSystem.API.DTOs.Responses;

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int? OperatorId { get; set; }
    public string? OperatorName { get; set; }
    public int? CounterId { get; set; }
    public string? CounterName { get; set; }
    public string? CounterStatus { get; set; }
    public int? ServiceTypeId { get; set; }
    public string? ServiceTypeName { get; set; }
    public List<int> SupportedServiceTypeIds { get; set; } = new List<int>();
    public List<SupportedServiceTypeResponseDto> SupportedServiceTypes { get; set; } = new List<SupportedServiceTypeResponseDto>();
}
