using QueueSystem.API.DTOs.Requests;
using QueueSystem.API.DTOs.Responses;

namespace QueueSystem.API.Services.Interfaces;

public interface IOperatorService
{
    Task<List<OperatorResponseDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<OperatorResponseDto> CreateAsync(CreateOperatorRequestDto request, CancellationToken cancellationToken);
    Task<OperatorResponseDto> UpdateAsync(int id, UpdateOperatorRequestDto request, CancellationToken cancellationToken);
}
