# 07_API_Contract.md

# API Contract

Version: 1.1

---

# 1. Unified Conversational Agent API

## Endpoint
`POST /api/v1/ai/chat`

## Request Body
`AiChatRequestDto`

```json
{
  "message": "Tôi muốn nấu canh chua cá lóc cho 4 người với ngân sách 150k",
  "conversationId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "modelOverride": "gemini-2.5-flash"
}
```

## Response Body
`ApiResult<AiChatResponseDto>`

```json
{
  "success": true,
  "message": "Đề xuất phù hợp đã được tạo.",
  "data": {
    "conversationId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "intent": "MealPlanning",
    "answer": "Bạn có thể nấu...",
    "agentsUsed": [
      "ShoppingAgent",
      "RecipeAgent",
      "InventoryAgent",
      "PromotionAgent",
      "BudgetAgent"
    ],
    "executionTimeMs": 842
  }
}
```

---

# 2. Request DTO

```csharp
public sealed class AiChatRequestDto
{
    public string Message { get; init; } = string.Empty;
    public Guid? ConversationId { get; init; }
    public string? ModelOverride { get; init; }
}
```

---

# 3. Response DTO

```csharp
public sealed class AiChatResponseDto
{
    public Guid ConversationId { get; init; }
    public string Intent { get; init; } = string.Empty;
    public string Answer { get; init; } = string.Empty;
    public List<string> AgentsUsed { get; init; } = [];
    public long ExecutionTimeMs { get; init; }
    public bool IsFromCache { get; init; }
}
```

---

# 4. Telemetry & Monitoring API

## 4.1 Dashboard
`GET /api/v1/ai/monitoring/dashboard`

Mục đích:
- Lấy số liệu tổng hợp tokens
- Độ trễ
- Chi phí USD/VND
- Tool đã gọi
- Cache hit rate

## 4.2 Metrics
`GET /api/v1/ai/monitoring/metrics`

Mục đích:
- Xuất OpenTelemetry metrics thô

## 4.3 Reset
`POST /api/v1/ai/monitoring/reset`

Mục đích:
- Đặt lại bộ đếm telemetry

Yêu cầu:
- Admin only

---

# 5. Monitoring Response Example

```json
{
  "totalRequests": 235,
  "successfulRequests": 230,
  "failedRequests": 5,
  "cacheHits": 102,
  "averageLatencyMs": 910,
  "promptTokens": 12450,
  "completionTokens": 6811,
  "estimatedCostUsd": 0.84,
  "estimatedCostVnd": 21000
}
```

---

# 6. Backward Compatibility

Các endpoint cũ chỉ được giữ lại nếu còn client sử dụng.

Chuẩn khuyến nghị hiện tại:
- `/api/v1/ai/chat`
- `/api/v1/ai/monitoring/dashboard`
- `/api/v1/ai/monitoring/metrics`
- `/api/v1/ai/monitoring/reset`

---

# 7. Contract Rules

- Response luôn bọc trong `ApiResult<T>`
- DTO không chứa business logic
- DTO không chứa DbContext
- DTO không chứa Entity Framework navigation object
- DTO chỉ truyền dữ liệu cần thiết
