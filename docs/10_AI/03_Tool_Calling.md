# 10_AI - TÀI LIỆU 03: THIẾT KẾ ĐIỀU PHỐI CÔNG CỤ (TOOL DISPATCHER DESIGN)

---

## MỤC LỤC
1. [Nguyên lý Tool Dispatcher Pattern](#1-nguyên-lý-tool-dispatcher-pattern)
2. [5 Ưu điểm vượt trội của Tool Dispatcher](#2-5-ưu-điểm-vượt-trội-của-tool-dispatcher)
3. [Kiến trúc & Interface của Tool Dispatcher](#3-kiến-trúc--interface-của-tool-dispatcher)
4. [Kỹ thuật Gọi Công Cụ Song Song (Parallel Tool Calling)](#4-kỹ-thuật-gọi-công-cụ-song-song)
5. [Đặc tả các Tools trong hệ thống](#5-đặc-tả-các-tools-trong-hệ-thống)
6. [Cơ chế bẫy lỗi & Fallback của Tool](#6-cơ-chế-bẫy-lỗi--fallback-của-tool)

---

## 1. NGUYÊN LÝ TOOL DISPATCHER PATTERN

Trong kiến trúc thông thường, Agent gọi Tool trực tiếp (`ShoppingAgent -> InventoryTool`). Tuy nhiên, cách tiếp cận này khiến Agent bị phụ thuộc chặt chẽ, khó theo dõi log, không thể xử lý timeout hoặc retry một cách nhất quán.

**Kiến trúc Tool Dispatcher chuẩn**:
```text
ShoppingAgent ──(Yêu cầu thực thi)──► Tool Dispatcher ──► Logging / Metrics / Retry
                                             │
                       ┌─────────────────────┼─────────────────────┐
                       ▼                     ▼                     ▼
                  InventoryTool         PromotionTool          RecipeTool
                       │                     │                     │
                       ▼                     ▼                     ▼
                InventoryService      PromotionService       RecipeService
                       │                     │                     │
                       ▼                     ▼                     ▼
                 Repositories          Repositories          Repositories
                       │                     │                     │
                       ▼                     ▼                     ▼
                                  SQL Server Database
```

---

## 2. 5 ƯU ĐIỂM VƯỢT TRỘI CỦA TOOL DISPATCHER

1. **Audit Logging**: Ghi nhận toàn bộ tham số đầu vào, kết quả đầu ra và timestamp của mọi lệnh gọi Tool từ LLM, phục vụ truy vết khi có sai lệch giá hoặc số lượng.
2. **Resilience & Retry**: Tự động thử lại tối đa 3 lần (với độ trễ lũy thừa mũ $200\text{ms} \rightarrow 400\text{ms} \rightarrow 800\text{ms}$) khi gặp sự cố tạm thời về kết nối cơ sở dữ liệu.
3. **Timeout Protection**: Đặt ngưỡng ngắt cứng ($1500\text{ms}$). Nếu một Tool bị tắc nghẽn, Dispatcher tự động ngắt và trả về kết quả dự phòng, không để toàn bộ cuộc trò chuyện của khách hàng bị treo.
4. **Parallel Execution**: Hỗ trợ kích hoạt nhiều Tool đồng thời khi Agent cần tra cứu nhiều nguồn dữ liệu độc lập.
5. **Observability & Metrics**: Thu thập các chỉ số: Số lượng Tool Call/phút, Tỷ lệ lỗi theo từng Tool, Thời gian phản hồi P95 và P99.

---

## 3. KIẾN TRÚC & INTERFACE CỦA TOOL DISPATCHER

```csharp
namespace SmartSupermarket.Backend.Features.AI.Dispatcher;

public interface IToolDispatcher
{
    Task<TResult> DispatchAsync<TResult>(string toolMethod, Dictionary<string, object> parameters, CancellationToken ct = default);
    Task<Dictionary<string, object>> DispatchBatchParallelAsync(List<ToolCallRequest> requests, CancellationToken ct = default);
}

public class ToolDispatcher : IToolDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ToolDispatcher> _logger;

    public ToolDispatcher(IServiceProvider serviceProvider, ILogger<ToolDispatcher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<TResult> DispatchAsync<TResult>(string toolMethod, Dictionary<string, object> parameters, CancellationToken ct = default)
    {
        _logger.LogInformation("[ToolDispatcher] Gọi tool: {ToolMethod}", toolMethod);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Thực thi với chính sách Timeout 1500ms
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromMilliseconds(1500));

            var result = await ExecuteWithRetryAsync<TResult>(toolMethod, parameters, timeoutCts.Token);
            stopwatch.Stop();
            _logger.LogInformation("[ToolDispatcher] {ToolMethod} thành công trong {ElapsedMs}ms", toolMethod, stopwatch.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ToolDispatcher] {ToolMethod} thất bại", toolMethod);
            throw;
        }
    }
}
```

---

## 4. KỸ THUẬT GỌI CÔNG CỤ SONG SONG (PARALLEL TOOL CALLING)

Khi khách hàng hỏi: *"Tôi muốn ăn lẩu bò nấm, có gì giảm giá không?"*, `ShoppingAgent` cần:
1. `RecipeTool.GetIngredients("Lẩu bò nấm")`
2. `InventoryTool.GetNearExpiry(daysThreshold=3)`
3. `PromotionTool.GetVouchers(customerId)`

Tool Dispatcher sử dụng `Task.WhenAll` để kích hoạt cả 3 Tool cùng lúc:
- Thay vì gọi tuần tự tốn $3 \times 200\text{ms} = 600\text{ms}$.
- Gọi song song chỉ tốn $\max(200\text{ms}, 180\text{ms}, 150\text{ms}) = 200\text{ms}$ (Tiết kiệm 66% thời gian chờ đợi của khách).

---

## 5. ĐẶC TẢ CÁC TOOLS TRONG HỆ THỐNG

| Tool | Methods Khả Dụng | Service Đích | Ghi Chú Logic |
|---|---|---|---|
| `ProductTool` | `Search()`, `GetById()`, `GetAlternative()` | `ProductService` | Tuyệt đối tuân thủ Category Isolation |
| `InventoryTool` | `GetStock()`, `GetNearExpiry()`, `GetBatches()` | `InventoryService` | Lọc hàng $\le 3$ ngày để gắn cờ Flash Sale 30% |
| `PromotionTool` | `GetVouchers()`, `CheckFlashSale()`, `ApplyDiscount()` | `PromotionService` | Áp dụng theo thứ tự ưu tiên trong knowledge base |
| `RecipeTool` | `GetRecipe()`, `GetIngredients()` | `RecipeService` | Đọc dữ liệu từ cẩm nang Cookpad |
| `CartTool` | `AddToCart()`, `EstimateCart()` | `OrderService` | Nạp 1-Click vào Session giỏ hàng của khách |

---

## 6. CƠ CHẾ BẪY LỖI & FALLBACK CỦA TOOL

1. **Lỗi hết hàng (Out of Stock)**: Tool tự động kích hoạt tìm kiếm `GetAlternative()` cùng CategoryId để Agent kịp thời đề xuất cho khách.
2. **Lỗi ngắt kết nối CSDL**: Tool trả về kết quả cache gần nhất từ `IMemoryCache` (thời hạn 5 phút) để không làm gián đoạn trải nghiệm của người dùng.
