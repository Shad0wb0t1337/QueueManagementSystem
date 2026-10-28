using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QueueSystem.API.DTOs.Responses;
using QueueSystem.API.Services.Interfaces;

namespace QueueSystem.API.Controllers;

[ApiController]
[Route("api/display")]
public class DisplayController : ControllerBase
{
    private readonly IDisplayService _displayService;

    public DisplayController(IDisplayService displayService)
    {
        _displayService = displayService;
    }

    [HttpGet("overview")]
    [AllowAnonymous]
    public async Task<ActionResult<DisplayOverviewResponseDto>> GetOverview(CancellationToken cancellationToken)
    {
        DisplayOverviewResponseDto response = await _displayService.GetOverviewAsync(cancellationToken);
        return Ok(response);
    }

    [HttpGet("counters")]
    [AllowAnonymous]
    public async Task<ActionResult<CounterDisplayGridResponseDto>> GetCounters(
        [FromQuery] int? from,
        [FromQuery] int? to,
        CancellationToken cancellationToken)
    {
        CounterDisplayGridResponseDto response = await _displayService.GetCounterGridAsync(from, to, cancellationToken);
        return Ok(response);
    }

    [HttpGet("counter/{counterId:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<CounterDisplayStateResponseDto>> GetCounterDisplayState(
        int counterId,
        CancellationToken cancellationToken)
    {
        CounterDisplayStateResponseDto response =
            await _displayService.GetCounterDisplayStateAsync(counterId, cancellationToken);
        return Ok(response);
    }
}
