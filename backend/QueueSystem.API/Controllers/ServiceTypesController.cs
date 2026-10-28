using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QueueSystem.API.DTOs.Requests;
using QueueSystem.API.DTOs.Responses;
using QueueSystem.API.Models;
using QueueSystem.API.Services.Interfaces;

namespace QueueSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServiceTypesController : ControllerBase
{
    private readonly IServiceTypeService _serviceTypeService;

    public ServiceTypesController(IServiceTypeService serviceTypeService)
    {
        _serviceTypeService = serviceTypeService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<ServiceTypeResponseDto>>> GetAll([FromQuery] bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        if (includeInactive && !User.IsInRole(UserRole.Admin.ToString()))
        {
            return Forbid();
        }

        List<ServiceTypeResponseDto> response = await _serviceTypeService.GetAllAsync(includeInactive, cancellationToken);
        return Ok(response);
    }

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<ActionResult<ServiceTypeResponseDto>> Create([FromBody] CreateServiceTypeRequestDto request, CancellationToken cancellationToken)
    {
        ServiceTypeResponseDto response = await _serviceTypeService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { id = response.Id }, response);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<ActionResult<ServiceTypeResponseDto>> Update(int id, [FromBody] UpdateServiceTypeRequestDto request, CancellationToken cancellationToken)
    {
        ServiceTypeResponseDto response = await _serviceTypeService.UpdateAsync(id, request, cancellationToken);
        return Ok(response);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _serviceTypeService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
