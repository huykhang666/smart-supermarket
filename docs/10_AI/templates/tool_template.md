# TEMPLATE: KHAI BÁO & ĐỊNH NGHĨA TOOL (TOOL SPECIFICATION TEMPLATE)

---

## 1. TÊN TOOL (TOOL NAME)
- **Tool Identifier**: `[Tên Tool viết theo PascalCase, ví dụ: InventoryTool]`
- **Interface C#**: `I[ToolName]`
- **LLM Function Identifier**: `[Tên hàm trong JSON Schema, ví dụ: queryInventoryStock]`

---

## 2. PURPOSE (MỤC ĐÍCH NGHIỆP VỤ)
- Mô tả rõ ranh giới chức năng của Tool.
- Xác định trường hợp nào LLM được phép kích hoạt Tool này.
- **Quy tắc bất biến**: Tool **không chứa Business Logic**, chỉ đóng vai trò adapter chuyển tiếp yêu cầu sang Service Layer.

---

## 3. METHODS (DANH SÁCH PHƯƠNG THỨC)
Liệt kê các method được khai báo trong Tool:
- `Task<int> GetStockAsync(int productId)`
- `Task<List<NearExpiryItemDto>> GetNearExpiryAsync(int daysThreshold)`
- `Task<ProductDto?> GetAlternativeAsync(int productId)`
- `Task<List<BatchDto>> GetAvailableBatchAsync(int productId)`

---

## 4. INPUT (THAM SỐ ĐẦU VÀO)
Bảng tham số đầu vào được mapping từ lệnh gọi của LLM:

| Tham Số | Kiểu Dữ Liệu | Bắt Buộc | Mô Tả |
|---|---|---|---|
| `productId` | `int` | Có | Mã ID sản phẩm trong siêu thị |
| `daysThreshold` | `int` | Không (Mặc định: 3) | Ngưỡng số ngày còn lại để xác định cận date |

---

## 5. OUTPUT (DỮ LIỆU ĐẦU RA)
Đặc tả DTO trả về cho Agent và LLM:
```json
{
  "productId": 105,
  "productName": "Thịt Nạc Dăm Heo C.P",
  "stockQuantity": 14,
  "isAvailable": true,
  "isNearExpiry": true,
  "expiryDate": "2026-10-29T00:00:00Z"
}
```

---

## 6. CALL SERVICE (DỊCH VỤ NGHIỆP VỤ ĐƯỢC GỌI)
- **Service Dependency**: `[Ví dụ: IInventoryService]`
- **Phương thức Service tương ứng**: `_inventoryService.GetNearExpiryBatchesAsync(daysThreshold)`
- **Lưu ý**: Tool inject Service thông qua Constructor Dependency Injection.

---

## 7. EXCEPTION (XỬ LÝ LỖI & NGOẠI LỆ)
Định nghĩa cách thức Tool bẫy lỗi để LLM không bị crash hoặc sinh ảo giác:
- `ProductNotFoundException` $\rightarrow$ Trả về `ToolExecutionResult.NotFound("Sản phẩm không tồn tại")`.
- `ServiceUnavailableException` (Database timeout) $\rightarrow$ Kích hoạt Fallback trả về dữ liệu an toàn từ MemoryCache hoặc báo lỗi lịch sự.
- `ValidationException` (Tham số âm, ID sai) $\rightarrow$ Trả về `ToolExecutionResult.InvalidInput(...)`.

---

## 8. EXAMPLE (CODE IMPLEMENTATION MẪU)

```csharp
namespace SmartSupermarket.Backend.Features.AI.Tools;

public class InventoryTool : IInventoryTool
{
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<InventoryTool> _logger;

    public InventoryTool(IInventoryService inventoryService, ILogger<InventoryTool> logger)
    {
        _inventoryService = inventoryService;
        _logger = logger;
    }

    public async Task<ToolExecutionResult> GetNearExpiryAsync(int daysThreshold)
    {
        try
        {
            var results = await _inventoryService.GetNearExpiryBatchesAsync(daysThreshold);
            return ToolExecutionResult.Success(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tra cứu hàng cận date với ngưỡng {Days} ngày", daysThreshold);
            return ToolExecutionResult.Failed("Không thể truy xuất thông tin hạn dùng lúc này.");
        }
    }
}
```
