using QueueSystem.API.DTOs.Responses;

namespace QueueSystem.API.Services.Interfaces;

public interface IDisplayService
{
    Task<DisplayOverviewResponseDto> GetOverviewAsync(CancellationToken cancellationToken);
    Task<CounterDisplayGridResponseDto> GetCounterGridAsync(int? from, int? to, CancellationToken cancellationToken);
    Task<CounterDisplayStateResponseDto> GetCounterDisplayStateAsync(int counterId, CancellationToken cancellationToken);
}
