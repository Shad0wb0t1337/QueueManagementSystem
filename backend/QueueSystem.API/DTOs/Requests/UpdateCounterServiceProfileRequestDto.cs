namespace QueueSystem.API.DTOs.Requests;

public class UpdateCounterServiceProfileRequestDto
{
    public List<int> EnabledServiceTypeIds { get; set; } = new List<int>();
}
