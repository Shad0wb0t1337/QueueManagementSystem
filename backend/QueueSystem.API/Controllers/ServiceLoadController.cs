using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QueueSystem.API.DTOs.Responses;
using QueueSystem.API.Models;
using QueueSystem.API.Services.Interfaces;

namespace QueueSystem.API.Controllers;

[ApiController]
[Authorize]
[Route("api/service-load")]
public class ServiceLoadController : ControllerBase
{
    private readonly IServiceLoadService _serviceLoadService;

    public ServiceLoadController(IServiceLoadService serviceLoadService)
    {
        _serviceLoadService = serviceLoadService;
    }

    [HttpGet]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<ActionResult<List<ServiceLoadSummaryResponseDto>>> GetAll(CancellationToken cancellationToken)
    {
        List<ServiceLoadSummaryResponseDto> response = await _serviceLoadService.GetAllAsync(cancellationToken);
        return Ok(response);
    }

    [HttpGet("operator/{counterId:int}")]
    [Authorize(Roles = nameof(UserRole.Operator))]
    public async Task<ActionResult<List<ServiceLoadSummaryResponseDto>>> GetForOperator(
        int counterId,
        CancellationToken cancellationToken)
    {
        int appUserId = GetAppUserId();
        List<ServiceLoadSummaryResponseDto> response =
            await _serviceLoadService.GetForOperatorCounterAsync(counterId, appUserId, cancellationToken);
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
