# TEMPLATE: KHỞI TẠO & ĐỊNH NGHĨA SERVICE NGHIỆP VỤ (SERVICE SPECIFICATION TEMPLATE)

---

## 1. THÔNG TIN ĐỊNH DANH SERVICE
- **Service Name**: `[Tên Service viết theo PascalCase, ví dụ: InventoryService]`
- **Interface C#**: `I[ServiceName]`
- **Namespace**: `SmartSupermarket.Backend.Features.AI.Services`
- **Mục tiêu**: Chứa toàn bộ Business Logic của nghiệp vụ, điều phối Repository và tính toán quy tắc kinh doanh.

---

## 2. REPOSITORY & PHỤ THUỘC (DEPENDENCIES)
- `AppDbContext` hoặc `I[Entity]Repository`
- `IMemoryCache` (nếu cần cache hiệu năng cao)
- `ILogger<ServiceName>`

---

## 3. BUSINESS LOGIC RULES (CÁC QUY TẮC KINH DOANH)
Liệt kê chi tiết logic nghiệp vụ được đóng gói trong Service:
- **Ví dụ với InventoryService**:
  1. Hạn sử dụng $\le 3$ ngày $\rightarrow$ Đủ điều kiện Flash Sale giảm 30%.
  2. Hạn sử dụng $\le 1$ ngày $\rightarrow$ Đủ điều kiện Flash Sale xả hàng giảm 50%.
  3. Lọc bỏ các lô hàng đã quá hạn sử dụng (`ExpiryDate < DateTime.UtcNow`).
  4. Sắp xếp ưu tiên xuất lô cận date trước (FIFO / FEFO).

---

## 4. EXAMPLE IMPLEMENTATION C#

```csharp
namespace SmartSupermarket.Backend.Features.AI.Services;

public class InventoryService : IInventoryService
{
    private readonly AppDbContext _context;
    private readonly ILogger<InventoryService> _logger;

    public InventoryService(AppDbContext context, ILogger<InventoryService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<NearExpiryItemDto>> GetNearExpiryBatchesAsync(int daysThreshold, CancellationToken ct = default)
    {
        var targetDate = DateTime.UtcNow.AddDays(daysThreshold);
        var now = DateTime.UtcNow;

        var batches = await _context.ProductBatches
            .AsNoTracking()
            .Include(b => b.Product)
            .Where(b => b.ExpiryDate > now && b.ExpiryDate <= targetDate && b.Quantity > 0)
            .OrderBy(b => b.ExpiryDate)
            .Select(b => new NearExpiryItemDto
            {
                BatchId = b.Id,
                ProductId = b.ProductId,
                ProductName = b.Product.Name,
                OriginalPrice = b.Product.Price,
                DiscountPercent = (b.ExpiryDate - now).TotalDays <= 1 ? 50 : 30,
                FinalPrice = (b.ExpiryDate - now).TotalDays <= 1 
                    ? b.Product.Price * 0.5m 
                    : b.Product.Price * 0.7m,
                QuantityRemaining = b.Quantity,
                ExpiryDate = b.ExpiryDate
            })
            .ToListAsync(ct);

        return batches;
    }
}
```
