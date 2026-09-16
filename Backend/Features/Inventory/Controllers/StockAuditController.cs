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

    [HttpPost]
    public async Task<IActionResult> CreateAudit([FromBody] CreateStockAuditRequest request)
    {
        var result = await _stockAuditService.CreateAuditAsync(request);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAudit(Guid id)
    {
        var result = await _stockAuditService.GetAuditAsync(id);
        return Ok(result);
    }

    [HttpPost("{id}/details")]
    public async Task<IActionResult> SaveAuditDetail(Guid id, [FromBody] SaveAuditDetailRequest request)
    {
        var result = await _stockAuditService.SaveAuditDetailAsync(id, request);
        return Ok(result);
    }

    [HttpPost("{id}/complete")]
    public async Task<IActionResult> CompleteAudit(Guid id)
    {
        var result = await _stockAuditService.CompleteAuditAsync(id);
        return Ok(result);
    }
}
