using QueueSystem.API.DTOs.Requests;
using QueueSystem.API.DTOs.Responses;

namespace QueueSystem.API.Services.Interfaces;

public interface IDepartmentService
{
    Task<List<DepartmentResponseDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<DepartmentResponseDto> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<DepartmentResponseDto> CreateAsync(CreateDepartmentRequestDto request, CancellationToken cancellationToken);
    Task<DepartmentResponseDto> UpdateAsync(int id, UpdateDepartmentRequestDto request, CancellationToken cancellationToken);
    Task<DepartmentResponseDto> DeactivateAsync(int id, CancellationToken cancellationToken);
}
