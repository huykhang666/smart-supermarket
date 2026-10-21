using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSupermarket.Backend.Features.Inventory.DTOs;
using SmartSupermarket.Backend.Features.Inventory.Services;

namespace SmartSupermarket.Backend.Features.Inventory.Controllers;

[ApiController]
[Route("api/stock-audit")]
[Authorize]
public class StockAuditController : ControllerBase
{
    private readonly IStockAuditService _stockAuditService;

    public StockAuditController(IStockAuditService stockAuditService)
    {
        _stockAuditService = stockAuditService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<IActionResult> GetAuditList([FromQuery] string? status, [FromQuery] string? zone)
    {
        var result = await _stockAuditService.GetAuditListAsync(status, zone);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<IActionResult> CreateAudit([FromBody] CreateStockAuditRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CreatedBy))
        {
            request.CreatedBy = User.Identity?.Name ?? User.FindFirst(ClaimTypes.Name)?.Value ?? "NV-Kho";
        }
        var result = await _stockAuditService.CreateAuditAsync(request);
        return Ok(result);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<IActionResult> GetAudit(Guid id)
    {
        try
        {
            var result = await _stockAuditService.GetAuditAsync(id);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/details")]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<IActionResult> SaveAuditDetail(Guid id, [FromBody] SaveAuditDetailRequest request)
    {
        try
        {
            var result = await _stockAuditService.SaveAuditDetailAsync(id, request);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/complete")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> CompleteAudit(Guid id)
    {
        try
        {
            var result = await _stockAuditService.CompleteAuditAsync(id);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/reconcile")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> ReconcileAudit(Guid id, [FromBody] ReconcileStockAuditRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.ReconciledBy))
            {
                request.ReconciledBy = User.Identity?.Name ?? User.FindFirst(ClaimTypes.Name)?.Value ?? "Quản lý";
            }
            var result = await _stockAuditService.ReconcileAuditAsync(id, request);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
