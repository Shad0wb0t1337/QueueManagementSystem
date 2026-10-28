using QueueSystem.API.DTOs.Requests;
using QueueSystem.API.DTOs.Responses;

namespace QueueSystem.API.Services.Interfaces;

public interface ICounterService
{
    Task<List<CounterResponseDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<CounterResponseDto> CreateAsync(CreateCounterRequestDto request, CancellationToken cancellationToken);
    Task<CounterResponseDto> UpdateAsync(int id, UpdateCounterRequestDto request, CancellationToken cancellationToken);
    Task<CounterResponseDto> UpdateOwnStatusAsync(int appUserId, UpdateOwnCounterStatusRequestDto request, CancellationToken cancellationToken);
    Task<List<CounterServiceProfileItemResponseDto>> GetServiceProfileAsync(int counterId, int appUserId, CancellationToken cancellationToken);
    Task<List<CounterServiceProfileItemResponseDto>> UpdateServiceProfileAsync(int counterId, int appUserId, UpdateCounterServiceProfileRequestDto request, CancellationToken cancellationToken);
}
