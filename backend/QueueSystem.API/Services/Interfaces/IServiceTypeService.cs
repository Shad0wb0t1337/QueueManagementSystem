using QueueSystem.API.DTOs.Requests;
using QueueSystem.API.DTOs.Responses;

namespace QueueSystem.API.Services.Interfaces;

public interface IServiceTypeService
{
    Task<List<ServiceTypeResponseDto>> GetAllAsync(bool includeInactive, CancellationToken cancellationToken);
    Task<ServiceTypeResponseDto> CreateAsync(CreateServiceTypeRequestDto request, CancellationToken cancellationToken);
    Task<ServiceTypeResponseDto> UpdateAsync(int id, UpdateServiceTypeRequestDto request, CancellationToken cancellationToken);
    Task DeleteAsync(int id, CancellationToken cancellationToken);
}
