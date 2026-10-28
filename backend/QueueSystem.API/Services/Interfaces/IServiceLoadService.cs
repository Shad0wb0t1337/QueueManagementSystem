using QueueSystem.API.DTOs.Responses;

namespace QueueSystem.API.Services.Interfaces;

public interface IServiceLoadService
{
    Task<List<ServiceLoadSummaryResponseDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<List<ServiceLoadSummaryResponseDto>> GetForOperatorCounterAsync(int counterId, int appUserId, CancellationToken cancellationToken);
    Task<HelpMostLoadedServiceResponseDto> HelpMostLoadedAsync(int counterId, int appUserId, CancellationToken cancellationToken);
}
