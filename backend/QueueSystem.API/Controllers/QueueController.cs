using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QueueSystem.API.DTOs.Requests;
using QueueSystem.API.DTOs.Responses;
using QueueSystem.API.Models;
using QueueSystem.API.Services.Interfaces;

namespace QueueSystem.API.Controllers;

[ApiController]
[Authorize(Roles = nameof(UserRole.Operator))]
[Route("api/[controller]")]
public class QueueController : ControllerBase
{
    private readonly IQueueService _queueService;

    public QueueController(IQueueService queueService)
    {
        _queueService = queueService;
    }

    [HttpPost("callnext")]
    public async Task<ActionResult<TicketCalledResponseDto>> CallNext([FromBody] CallNextRequestDto request, CancellationToken cancellationToken)
    {
        int appUserId = GetAppUserId();
        TicketCalledResponseDto response = await _queueService.CallNextAsync(request, appUserId, cancellationToken);
        return Ok(response);
    }

    [HttpPost("serving/{ticketId:int}")]
    public async Task<ActionResult<QueueTicketResponseDto>> MarkServing(int ticketId, CancellationToken cancellationToken)
    {
        int appUserId = GetAppUserId();
        QueueTicketResponseDto response = await _queueService.MarkServingAsync(ticketId, appUserId, cancellationToken);
        return Ok(response);
    }

    [HttpPost("done/{ticketId:int}")]
    public async Task<ActionResult<QueueTicketResponseDto>> MarkDone(int ticketId, CancellationToken cancellationToken)
    {
        int appUserId = GetAppUserId();
        QueueTicketResponseDto response = await _queueService.MarkDoneAsync(ticketId, appUserId, cancellationToken);
        return Ok(response);
    }

    [HttpPost("noshow/{ticketId:int}")]
    public async Task<ActionResult<QueueTicketResponseDto>> MarkNoShow(int ticketId, CancellationToken cancellationToken)
    {
        int appUserId = GetAppUserId();
        QueueTicketResponseDto response = await _queueService.MarkNoShowAsync(ticketId, appUserId, cancellationToken);
        return Ok(response);
    }

    [HttpGet("counters/{counterId:int}/recent-history")]
    public async Task<ActionResult<List<RecentTicketHistoryItemResponseDto>>> GetRecentHistory(
        int counterId,
        CancellationToken cancellationToken)
    {
        int appUserId = GetAppUserId();
        List<RecentTicketHistoryItemResponseDto> response =
            await _queueService.GetRecentHistoryAsync(counterId, appUserId, cancellationToken);
        return Ok(response);
    }

    [HttpPost("tickets/{ticketId:int}/recall")]
    public async Task<ActionResult<RecallTicketResponseDto>> RecallTicket(int ticketId, CancellationToken cancellationToken)
    {
        int appUserId = GetAppUserId();
        RecallTicketResponseDto response = await _queueService.RecallTicketAsync(ticketId, appUserId, cancellationToken);
        return Ok(response);
    }

    [HttpPost("tickets/{ticketId:int}/redirect")]
    public async Task<ActionResult<RedirectTicketResponseDto>> RedirectTicket(
        int ticketId,
        [FromBody] RedirectTicketRequestDto request,
        CancellationToken cancellationToken)
    {
        int appUserId = GetAppUserId();
        RedirectTicketResponseDto response = await _queueService.RedirectTicketAsync(ticketId, appUserId, request, cancellationToken);
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
