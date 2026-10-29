# 03_Tool_Calling.md

# Tool Calling Architecture

Version: 1.1

---

# 1. Mục tiêu

Tool Layer là lớp duy nhất cho phép Agent gọi sang Business Layer.

Agent không gọi Service trực tiếp.
Agent không gọi Repository trực tiếp.
Agent không gọi DbContext trực tiếp.

Mọi lệnh gọi đều đi qua ToolDispatcher.

---

# 2. Nguyên tắc cốt lõi

- Tool không chứa Business Logic
- Tool chỉ gọi Service
- Tool phải có khả năng ghi log
- Tool phải có khả năng đo thời gian thực thi
- Tool phải trả về kết quả đóng gói an toàn
- Tool phải hỗ trợ fallback mềm khi có lỗi

---

# 3. ToolDispatcher Contract

```csharp
/// <summary>
/// Điều phối gọi tool an toàn, đo độ trễ và trả về wrapper DTO
/// để tránh crash luồng Agent khi Tool lỗi.
/// </summary>
public interface IToolDispatcher
{
    Task<ToolExecutionResultDto<T>> DispatchAsync<T>(
        string toolMethod,
        Dictionary<string, object?> parameters,
        CancellationToken cancellationToken = default);

    Task<ToolExecutionResultDto<T>> DispatchBatchParallelAsync<T>(
        IEnumerable<ToolExecutionRequest> requests,
        CancellationToken cancellationToken = default);
}
```

---

# 4. ToolExecutionResultDto<T>

```csharp
public sealed class ToolExecutionResultDto<T>
{
    public bool IsSuccess { get; init; }
    public T? Data { get; init; }
    public string? ErrorMessage { get; init; }
    public long ExecutionTimeMs { get; init; }
    public string? ToolName { get; init; }
    public string? ToolMethod { get; init; }
    public string? CorrelationId { get; init; }
}
```

---

# 5. Chuẩn hóa tên gọi Tool

ToolDispatcher hỗ trợ 2 kiểu gọi:

1. Namespaced key chuẩn hóa:
- `inventorytool.getstock`
- `promotiontool.getvouchers`
- `recipetool.getingredients`

2. Alias ngắn:
- `getstock`
- `checkinventory`
- `getvouchers`
- `getingredients`
- `budgettool.evaluatebudget`
- `carttotal`

---

# 6. Bảng alias chấp nhận

| Tool Name | Method | Namespaced Key | Alias chấp nhận |
|---|---|---|---|
| ProductTool | SearchAsync | producttool.search | search, findproduct |
| InventoryTool | GetStockAsync | inventorytool.getstock | getstock, checkinventory |
| InventoryTool | GetNearExpiryBatchesAsync | inventorytool.getnearexpiry | getnearexpiry, nearexpiry |
| PromotionTool | GetActiveVouchersAsync | promotiontool.getvouchers | getvouchers, promotions |
| RecipeTool | GetRecipeIngredientsAsync | recipetool.getingredients | getingredients, ingredients |
| BudgetTool | EvaluateBudgetAsync | budgettool.evaluatebudget | evaluatebudget, budget |
| CartTool | EstimateCartTotalAsync | carttool.estimatecart | estimatecart, carttotal |

---

# 7. Audit Logging

Mỗi lần gọi Tool cần log:
- ToolName
- ToolMethod
- Parameters summary
- ExecutionTimeMs
- IsSuccess
- CorrelationId

---

# 8. Retry Policy

ToolDispatcher có thể retry có kiểm soát khi gặp lỗi tạm thời:
- network timeout
- transient failure
- dependency throttling

Không retry vô hạn.

Áp dụng exponential backoff.

---

# 9. Timeout Guard

Mỗi tool call cần có timeout guard.

Mức mặc định:
- 1500ms cho tool nội bộ
- có thể tinh chỉnh theo từng tool

Nếu quá thời gian:
- trả wrapper lỗi
- không làm sập luồng Agent

---

# 10. Parallel Execution

ToolDispatcher hỗ trợ chạy song song các tool độc lập.

Ví dụ:
- kiểm tra tồn kho
- kiểm tra voucher
- kiểm tra recipe

Có thể chạy bằng `Task.WhenAll(...)`.

---

# 11. Metrics Observability

ToolDispatcher phải ghi metric:
- ai.tool.duration
- ai.tool.success
- ai.tool.failure

---

# 12. Quy tắc triển khai Tool

Mỗi Tool:
- chỉ nhận input DTO / parameter dictionary
- chỉ gọi Service
- không chứa logic nghiệp vụ
- không truy cập DbContext
- không truy cập Repository

Ví dụ:
- `InventoryTool` → `InventoryService`
- `PromotionTool` → `PromotionService`
- `RecipeTool` → `RecipeService`
- `BudgetTool` → `BudgetService`
- `CartTool` → `CartService`
