using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartSupermarket.Backend.Infrastructure.Persistence;

namespace SmartSupermarket.Backend.Features.Inventory.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly AppDbContext _context;

    public InventoryController(AppDbContext context)
    {
        _context = context;
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
}
