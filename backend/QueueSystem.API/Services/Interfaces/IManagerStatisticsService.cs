using QueueSystem.API.DTOs.Responses;

namespace QueueSystem.API.Services.Interfaces;

public interface IManagerStatisticsService
{
    Task<ManagerStatisticsOverviewResponseDto> GetOverviewAsync(int appUserId, DateTime? from, DateTime? to, int? departmentId, CancellationToken cancellationToken);
    Task<List<ManagerServiceStatisticsResponseDto>> GetServiceStatisticsAsync(int appUserId, DateTime? from, DateTime? to, int? departmentId, CancellationToken cancellationToken);
    Task<List<ManagerCounterStatisticsResponseDto>> GetCounterStatisticsAsync(int appUserId, DateTime? from, DateTime? to, int? departmentId, CancellationToken cancellationToken);
    Task<List<ManagerOperatorStatisticsResponseDto>> GetOperatorStatisticsAsync(int appUserId, DateTime? from, DateTime? to, int? departmentId, CancellationToken cancellationToken);
    Task<List<ManagerRedirectStatisticsResponseDto>> GetRedirectStatisticsAsync(int appUserId, DateTime? from, DateTime? to, int? departmentId, CancellationToken cancellationToken);
    Task<List<ManagerRecallStatisticsResponseDto>> GetRecallStatisticsAsync(int appUserId, DateTime? from, DateTime? to, int? departmentId, CancellationToken cancellationToken);
    Task<List<ManagerTicketEventResponseDto>> GetRecentTicketEventsAsync(int appUserId, DateTime? from, DateTime? to, int? departmentId, int? limit, CancellationToken cancellationToken);
}
