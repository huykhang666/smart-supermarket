using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartSupermarket.Backend.Common.Results;
using SmartSupermarket.Backend.Features.Inventory.DTOs;
using SmartSupermarket.Backend.Features.Inventory.Services;
using SmartSupermarket.Backend.Infrastructure.Persistence;
using System.Security.Claims;
using SmartSupermarket.Backend.Domain.Enums;

namespace SmartSupermarket.Backend.Features.Inventory.Controllers;

[ApiController]
[Route("api/inventory")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;
    private readonly AppDbContext _context;

    public InventoryController(IInventoryService inventoryService, AppDbContext context)
    {
        _inventoryService = inventoryService;
        _context = context;
    }

    private int GetCurrentUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return idClaim != null && int.TryParse(idClaim.Value, out int id) ? id : 0;
    }

    [HttpGet("batches")]
    public async Task<IActionResult> GetBatches()
    {
        var batches = await _context.InventoryBatches
            .Include(b => b.Product)
            .Select(b => new
            {
                b.BatchId,
                b.BatchCode,
                ProductName = b.Product.ProductName,
                Quantity = b.Quantity.ToString() + " " + b.Product.Unit,
                ExpiryDate = b.ExpiryDate.ToString("yyyy-MM-dd"),
                Location = b.StorageLocation,
                Status = b.ExpiryDate < DateTime.UtcNow.AddDays(7) ? "🔴 Đỏ (Sắp hết hạn)" : (b.Quantity < 20 ? "🟠 Cam (Tồn thấp)" : "🟢 Xanh (An toàn)")
            })
            .ToListAsync();

        return Ok(new { status = 200, data = batches });
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<IActionResult> GetInventoryList([FromQuery] int branchId = 1, [FromQuery] string? status = null, [FromQuery] string? search = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var (items, total) = await _inventoryService.GetInventoryListAsync(branchId, status, search, page, pageSize);
        return Ok(ApiResult<object>.Success(new { Items = items, TotalCount = total }));
    }

    [HttpGet("{productId}")]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<IActionResult> GetInventoryByProduct(int productId, [FromQuery] int branchId = 1)
    {
        var item = await _inventoryService.GetInventoryByProductAsync(productId, branchId);
        if (item == null) return NotFound(ApiResult<object>.Failure("Không tìm thấy thông tin tồn kho."));
        return Ok(ApiResult<Domain.Entities.Inventory>.Success(item));
    }

    [HttpGet("low-stock")]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<IActionResult> GetLowStock([FromQuery] int branchId = 1, [FromQuery] int top = 10)
    {
        var items = await _inventoryService.GetLowStockProductsAsync(branchId, top);
        return Ok(ApiResult<IEnumerable<Domain.Entities.Inventory>>.Success(items));
    }

    [HttpGet("expiring")]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<IActionResult> GetExpiring([FromQuery] int branchId = 1, [FromQuery] int days = 15)
    {
        var items = await _inventoryService.GetExpiringProductsAsync(branchId, days);
        return Ok(ApiResult<IEnumerable<Domain.Entities.StockHistory>>.Success(items));
    }

    [HttpPut("{productId}/adjust")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> AdjustStock(int productId, [FromBody] StockAdjustRequest request, [FromQuery] int branchId = 1)
    {
        try
        {
            await _inventoryService.AdjustStockAsync(productId, branchId, request.QuantityChange, request.Note, GetCurrentUserId());
            return Ok(ApiResult<bool>.Success(true, "Điều chỉnh số lượng thành công."));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResult<bool>.Failure(ex.Message));
        }
    }

    [HttpGet("stock-history")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GetStockHistory([FromQuery] int productId, [FromQuery] int branchId = 1, [FromQuery] StockChangeType? type = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var (items, total) = await _inventoryService.GetStockHistoryAsync(productId, branchId, type, null, null, page, pageSize);
        return Ok(ApiResult<object>.Success(new { Items = items, TotalCount = total }));
    }
}
