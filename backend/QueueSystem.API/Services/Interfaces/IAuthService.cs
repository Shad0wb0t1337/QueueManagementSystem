using QueueSystem.API.DTOs.Requests;
using QueueSystem.API.DTOs.Responses;

namespace QueueSystem.API.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken);
    Task<AuthResponseDto> GetMeAsync(int appUserId, string token, CancellationToken cancellationToken);
    string GenerateJwt(int userId, string username, string role, int? operatorId, int? counterId);
}
