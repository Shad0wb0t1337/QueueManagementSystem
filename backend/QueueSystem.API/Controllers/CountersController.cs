using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QueueSystem.API.DTOs.Requests;
using QueueSystem.API.DTOs.Responses;
using QueueSystem.API.Models;
using QueueSystem.API.Services.Interfaces;

namespace QueueSystem.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class CountersController : ControllerBase
{
    private readonly ICounterService _counterService;
    private readonly IServiceLoadService _serviceLoadService;

    public CountersController(ICounterService counterService, IServiceLoadService serviceLoadService)
    {
        _counterService = counterService;
        _serviceLoadService = serviceLoadService;
    }

    [HttpGet]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<ActionResult<List<CounterResponseDto>>> GetAll(CancellationToken cancellationToken)
    {
        List<CounterResponseDto> response = await _counterService.GetAllAsync(cancellationToken);
        return Ok(response);
    }

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<ActionResult<CounterResponseDto>> Create([FromBody] CreateCounterRequestDto request, CancellationToken cancellationToken)
    {
        CounterResponseDto response = await _counterService.CreateAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<ActionResult<CounterResponseDto>> Update(int id, [FromBody] UpdateCounterRequestDto request, CancellationToken cancellationToken)
    {
        CounterResponseDto response = await _counterService.UpdateAsync(id, request, cancellationToken);
        return Ok(response);
    }

    [HttpPut("my/status")]
    [Authorize(Roles = nameof(UserRole.Operator))]
    public async Task<ActionResult<CounterResponseDto>> UpdateOwnStatus(
        [FromBody] UpdateOwnCounterStatusRequestDto request,
        CancellationToken cancellationToken)
    {
        int appUserId = GetAppUserId();
        CounterResponseDto response = await _counterService.UpdateOwnStatusAsync(appUserId, request, cancellationToken);
        return Ok(response);
    }

    [HttpGet("{counterId:int}/service-profile")]
    [Authorize(Roles = nameof(UserRole.Operator))]
    public async Task<ActionResult<List<CounterServiceProfileItemResponseDto>>> GetServiceProfile(
        int counterId,
        CancellationToken cancellationToken)
    {
        int appUserId = GetAppUserId();
        List<CounterServiceProfileItemResponseDto> response = await _counterService.GetServiceProfileAsync(counterId, appUserId, cancellationToken);
        return Ok(response);
    }

    [HttpPut("{counterId:int}/service-profile")]
    [Authorize(Roles = nameof(UserRole.Operator))]
    public async Task<ActionResult<List<CounterServiceProfileItemResponseDto>>> UpdateServiceProfile(
        int counterId,
        [FromBody] UpdateCounterServiceProfileRequestDto request,
        CancellationToken cancellationToken)
    {
        int appUserId = GetAppUserId();
        List<CounterServiceProfileItemResponseDto> response = await _counterService.UpdateServiceProfileAsync(counterId, appUserId, request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("{counterId:int}/service-profile/help-most-loaded")]
    [Authorize(Roles = nameof(UserRole.Operator))]
    public async Task<ActionResult<HelpMostLoadedServiceResponseDto>> HelpMostLoaded(
        int counterId,
        CancellationToken cancellationToken)
    {
        int appUserId = GetAppUserId();
        HelpMostLoadedServiceResponseDto response = await _serviceLoadService.HelpMostLoadedAsync(counterId, appUserId, cancellationToken);
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
