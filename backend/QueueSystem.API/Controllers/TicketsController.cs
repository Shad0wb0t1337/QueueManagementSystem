using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QueueSystem.API.DTOs.Requests;
using QueueSystem.API.DTOs.Responses;
using QueueSystem.API.Models;
using QueueSystem.API.Services.Interfaces;

namespace QueueSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TicketsController : ControllerBase
{
    private readonly ITicketService _ticketService;

    public TicketsController(ITicketService ticketService)
    {
        _ticketService = ticketService;
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<IssueTicketResponseDto>> Issue([FromBody] IssueTicketRequestDto request, CancellationToken cancellationToken)
    {
        IssueTicketResponseDto response = await _ticketService.IssueTicketAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpGet("queue/{serviceTypeId:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<List<QueueTicketResponseDto>>> GetQueue(int serviceTypeId, CancellationToken cancellationToken)
    {
        List<QueueTicketResponseDto> response = await _ticketService.GetQueueAsync(serviceTypeId, cancellationToken);
        return Ok(response);
    }

    [HttpGet("queue/counter/{counterId:int}")]
    [Authorize(Roles = nameof(UserRole.Operator))]
    public async Task<ActionResult<List<QueueTicketResponseDto>>> GetCounterQueue(int counterId, CancellationToken cancellationToken)
    {
        List<QueueTicketResponseDto> response = await _ticketService.GetCounterQueueAsync(counterId, cancellationToken);
        return Ok(response);
    }

    [HttpGet("active")]
    [AllowAnonymous]
    public async Task<ActionResult<List<ActiveTicketResponseDto>>> GetActive(CancellationToken cancellationToken)
    {
        List<ActiveTicketResponseDto> response = await _ticketService.GetActiveTicketsAsync(cancellationToken);
        return Ok(response);
    }

    [HttpGet("stats/today")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<ActionResult<TodayStatsResponseDto>> GetTodayStats(CancellationToken cancellationToken)
    {
        TodayStatsResponseDto response = await _ticketService.GetTodayStatsAsync(cancellationToken);
        return Ok(response);
    }
}
