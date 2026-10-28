using QueueSystem.API.DTOs.Requests;
using QueueSystem.API.DTOs.Responses;

namespace QueueSystem.API.Services.Interfaces;

public interface IManagerDepartmentService
{
    Task<List<ManagerUserResponseDto>> GetManagersAsync(CancellationToken cancellationToken);
    Task<List<ManagerDepartmentResponseDto>> GetManagerDepartmentsAsync(int appUserId, CancellationToken cancellationToken);
    Task<List<ManagerDepartmentResponseDto>> GetAssignedDepartmentsAsync(int appUserId, CancellationToken cancellationToken);
    Task<List<ManagerDepartmentResponseDto>> UpdateManagerDepartmentsAsync(
        int appUserId,
        UpdateManagerDepartmentsRequestDto request,
        CancellationToken cancellationToken);
}
