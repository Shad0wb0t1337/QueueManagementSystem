using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QueueSystem.API.DTOs.Requests;
using QueueSystem.API.DTOs.Responses;
using QueueSystem.API.Models;
using QueueSystem.API.Services.Interfaces;

namespace QueueSystem.API.Controllers;

[ApiController]
[Authorize(Roles = nameof(UserRole.Admin))]
[Route("api/[controller]")]
public class OperatorsController : ControllerBase
{
    private readonly IOperatorService _operatorService;

    public OperatorsController(IOperatorService operatorService)
    {
        _operatorService = operatorService;
    }

    [HttpGet]
    public async Task<ActionResult<List<OperatorResponseDto>>> GetAll(CancellationToken cancellationToken)
    {
        List<OperatorResponseDto> response = await _operatorService.GetAllAsync(cancellationToken);
        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<OperatorResponseDto>> Create([FromBody] CreateOperatorRequestDto request, CancellationToken cancellationToken)
    {
        OperatorResponseDto response = await _operatorService.CreateAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<OperatorResponseDto>> Update(int id, [FromBody] UpdateOperatorRequestDto request, CancellationToken cancellationToken)
    {
        OperatorResponseDto response = await _operatorService.UpdateAsync(id, request, cancellationToken);
        return Ok(response);
    }
}
