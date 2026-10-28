using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QueueSystem.API.DTOs.Responses;
using QueueSystem.API.Services.Interfaces;

namespace QueueSystem.API.Controllers;

[ApiController]
[Authorize(Roles = "Manager")]
[Route("api/manager/statistics")]
public class ManagerStatisticsController : ControllerBase
{
    private readonly IManagerStatisticsService _managerStatisticsService;

    public ManagerStatisticsController(IManagerStatisticsService managerStatisticsService)
    {
        _managerStatisticsService = managerStatisticsService;
    }

    [HttpGet("overview")]
    public async Task<ActionResult<ManagerStatisticsOverviewResponseDto>> GetOverview(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int? departmentId,
        CancellationToken cancellationToken)
    {
        int appUserId = GetAppUserId();
        ManagerStatisticsOverviewResponseDto response =
            await _managerStatisticsService.GetOverviewAsync(appUserId, from, to, departmentId, cancellationToken);
        return Ok(response);
    }

    [HttpGet("services")]
    public async Task<ActionResult<List<ManagerServiceStatisticsResponseDto>>> GetServices(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int? departmentId,
        CancellationToken cancellationToken)
    {
        int appUserId = GetAppUserId();
        List<ManagerServiceStatisticsResponseDto> response =
            await _managerStatisticsService.GetServiceStatisticsAsync(appUserId, from, to, departmentId, cancellationToken);
        return Ok(response);
    }

    [HttpGet("counters")]
    public async Task<ActionResult<List<ManagerCounterStatisticsResponseDto>>> GetCounters(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int? departmentId,
        CancellationToken cancellationToken)
    {
        int appUserId = GetAppUserId();
        List<ManagerCounterStatisticsResponseDto> response =
            await _managerStatisticsService.GetCounterStatisticsAsync(appUserId, from, to, departmentId, cancellationToken);
        return Ok(response);
    }

    [HttpGet("operators")]
    public async Task<ActionResult<List<ManagerOperatorStatisticsResponseDto>>> GetOperators(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int? departmentId,
        CancellationToken cancellationToken)
    {
        int appUserId = GetAppUserId();
        List<ManagerOperatorStatisticsResponseDto> response =
            await _managerStatisticsService.GetOperatorStatisticsAsync(appUserId, from, to, departmentId, cancellationToken);
        return Ok(response);
    }

    [HttpGet("redirects")]
    public async Task<ActionResult<List<ManagerRedirectStatisticsResponseDto>>> GetRedirects(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int? departmentId,
        CancellationToken cancellationToken)
    {
        int appUserId = GetAppUserId();
        List<ManagerRedirectStatisticsResponseDto> response =
            await _managerStatisticsService.GetRedirectStatisticsAsync(appUserId, from, to, departmentId, cancellationToken);
        return Ok(response);
    }

    [HttpGet("recalls")]
    public async Task<ActionResult<List<ManagerRecallStatisticsResponseDto>>> GetRecalls(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int? departmentId,
        CancellationToken cancellationToken)
    {
        int appUserId = GetAppUserId();
        List<ManagerRecallStatisticsResponseDto> response =
            await _managerStatisticsService.GetRecallStatisticsAsync(appUserId, from, to, departmentId, cancellationToken);
        return Ok(response);
    }

    [HttpGet("events")]
    public async Task<ActionResult<List<ManagerTicketEventResponseDto>>> GetEvents(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int? departmentId,
        [FromQuery] int? limit,
        CancellationToken cancellationToken)
    {
        int appUserId = GetAppUserId();
        List<ManagerTicketEventResponseDto> response =
            await _managerStatisticsService.GetRecentTicketEventsAsync(appUserId, from, to, departmentId, limit, cancellationToken);
        return Ok(response);
    }

    private int GetAppUserId()
    {
        string? userIdRaw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdRaw, out int appUserId))
        {
            throw new UnauthorizedAccessException("Invalid token user id.");
        }

        return appUserId;
    }
}
