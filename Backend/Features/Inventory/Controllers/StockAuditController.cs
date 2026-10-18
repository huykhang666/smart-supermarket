using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SmartSupermarket.Backend.Features.Inventory.DTOs;
using SmartSupermarket.Backend.Features.Inventory.Services;

namespace SmartSupermarket.Backend.Features.Inventory.Controllers;

[ApiController]
[Route("api/stock-audit")]
public class StockAuditController : ControllerBase
{
    private readonly IStockAuditService _stockAuditService;

    public StockAuditController(IStockAuditService stockAuditService)
    {
        _stockAuditService = stockAuditService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAuditList([FromQuery] string? status, [FromQuery] string? zone)
    {
        var result = await _stockAuditService.GetAuditListAsync(status, zone);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAudit([FromBody] CreateStockAuditRequest request)
    {
        var result = await _stockAuditService.CreateAuditAsync(request);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAudit(Guid id)
    {
        try
        {
            var result = await _stockAuditService.GetAuditAsync(id);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/details")]
    public async Task<IActionResult> SaveAuditDetail(Guid id, [FromBody] SaveAuditDetailRequest request)
    {
        try
        {
            var result = await _stockAuditService.SaveAuditDetailAsync(id, request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/complete")]
    public async Task<IActionResult> CompleteAudit(Guid id)
    {
        try
        {
            var result = await _stockAuditService.CompleteAuditAsync(id);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/reconcile")]
    public async Task<IActionResult> ReconcileAudit(Guid id, [FromBody] ReconcileStockAuditRequest request)
    {
        try
        {
            var result = await _stockAuditService.ReconcileAuditAsync(id, request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
