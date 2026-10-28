using QueueSystem.API.Models;

namespace QueueSystem.API.DTOs.Requests;

public class UpdateOwnCounterStatusRequestDto
{
    public string Status { get; set; } = CounterStatus.Active.ToString();
}
