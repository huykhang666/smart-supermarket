# 10_AI - TÀI LIỆU 01: KIẾN TRÚC TỔNG THỂ HỆ THỐNG AI (AI ARCHITECTURE)

---

## MỤC LỤC
1. [Mô hình kiến trúc phân tầng 10 lớp chuẩn Enterprise](#1-mô-hình-kiến-trúc-phân-tầng-10-lớp-chuẩn-enterprise)
2. [Chi tiết các tầng mở rộng then chốt](#2-chi-tiết-các-tầng-mở-rộng-then-chốt)
   - [2.1. ConversationService & PromptBuilder](#21-conversationservice--promptbuilder)
   - [2.2. LLM Gateway Layer (Infrastructure/External/AI)](#22-llm-gateway-layer)
   - [2.3. Tool Dispatcher (Logging, Retry, Parallel, Metrics)](#23-tool-dispatcher)
   - [2.4. Agent Subsystem & BaseAgent Pattern](#24-agent-subsystem--baseagent-pattern)
   - [2.5. Business Services, Repositories & SQL Server](#25-business-services-repositories--sql-server)
3. [Luồng thực thi đầy đủ (End-to-End Execution Flow)](#3-luồng-thực-thi-đầy-đủ)
4. [Cấu trúc thư mục Backend chuẩn Clean Architecture](#4-cấu-trúc-thư-mục-backend-chuẩn-clean-architecture)
5. [Lộ trình tiến hóa: Bản MVP tối giản vs Enterprise Multi-Agent](#5-lộ-trình-tiến-hóa-bản-mvp-tối-giản-vs-enterprise-multi-agent)

---

## 1. MÔ HÌNH KIẾN TRÚC PHÂN TẦNG 10 LỚP CHUẨN ENTERPRISE

Hệ thống AI thực tế của **Smart SuperMarket** áp dụng kiến trúc phân tầng chuyên sâu 10 lớp. Sự bổ sung của **ConversationService**, **PromptBuilder**, **LLM Gateway** và **Tool Dispatcher** đảm bảo hệ thống có khả năng mở rộng tối đa, dễ dàng thay đổi Model AI mà không cần sửa đổi logic nghiệp vụ của Agent.

```text
User
  │
  ▼
Angular / Web Chat UI
  │
  ▼
AIController
  │
  ▼
ConversationService       ◄── (Quản lý Session Memory & History)
  │
  ▼
PromptBuilder             ◄── (Ghép nối Identity + Rules + Fewshot + Context)
  │
  ▼
AgentOrchestrator         ◄── (Phân tích Intent & Điều phối Multi-Agent)
  │
  ▼
LLM Gateway               ◄── (ILLMGateway: Gemini, Groq, OpenAI, Claude)
  │
  ▼
Tool Dispatcher           ◄── (Logging, Retry, Timeout, Parallel, Metrics)
  │
  ▼
Tools                     ◄── (ProductTool, InventoryTool, PromotionTool, RecipeTool)
  │
  ▼
Business Services         ◄── (Toàn bộ Business Logic tại đây)
  │
  ▼
Repositories              ◄── (EF Core Queries: Where, Include, AsNoTracking)
  │
  ▼
SQL Server Database
```

---

## 2. CHI TIẾT CÁC TẦNG MỞ RỘNG THEN CHỐT

### 2.1. ConversationService & PromptBuilder
- **ConversationService**:
  - Quản lý phiên làm việc (`SessionId`), lưu trữ và bóc tách lịch sử hội thoại gần nhất (`Sliding Window Memory`).
  - Đảm bảo AI luôn nhận biết trạng thái giỏ hàng hiện tại của khách hàng.
- **PromptBuilder**:
  - Không nhúng hardcode prompt vào code C#.
  - Tự động nạp và ghép nối nhiều lớp prompt từ thư mục `prompts/`:
    $$\text{FinalPrompt} = \text{Identity} + \text{Rules} + \text{Style} + \text{TaskPrompt} + \text{FewShotExamples} + \text{GroundTruthKnowledge}$$

### 2.2. LLM Gateway Layer (Infrastructure/External/AI)
**Nguyên tắc vàng**: Agent **tuyệt đối KHÔNG gọi trực tiếp Gemini hay OpenAI**. Agent chỉ tương tác với interface `ILLMGateway`.
```text
Features/AI/Infrastructure/External/AI/
├── ILLMGateway.cs
├── GeminiGateway.cs        # Kết nối Gemini 2.5 Flash
├── GroqGateway.cs          # Kết nối Groq Llama 3.3 70B LPU
├── OpenAIGateway.cs        # Kết nối GPT-4o Mini
└── LocalGateway.cs         # Dự phòng Offline Deterministic
```
- **Lợi ích**: Khi đổi từ Gemini sang Groq hoặc Claude, toàn bộ code của các Agent giữ nguyên 100%.
- Tự động bẫy lỗi HTTP 429 (ResourceExhausted) hoặc Timeout $>4\text{s}$ để chuyển đổi luân phiên giữa các nhà cung cấp (*Smart Fallback Routing*).

### 2.3. Tool Dispatcher (Logging, Retry, Parallel, Metrics)
Thay vì Agent gọi Tool một cách tùy tiện, mọi lời gọi Tool đều đi qua **Tool Dispatcher**:
- **Audit Logging**: Ghi nhận toàn bộ tham số đầu vào và đầu ra của Tool Call để theo dõi hành vi của LLM.
- **Resilience & Retry**: Tự động thử lại (Retry with exponential backoff) khi truy vấn cơ sở dữ liệu gặp deadlock hoặc gián đoạn.
- **Timeout Guard**: Giới hạn thời gian thực thi Tool trong vòng $1.5\text{s}$, ngăn chặn Agent bị treo.
- **Parallel Tool Calling**: Thực thi đồng thời nhiều Tool độc lập (ví dụ: vừa kiểm tra tồn kho vừa kiểm tra voucher khuyến mãi cùng lúc qua `Task.WhenAll`).
- **Metrics**: Đo lường tần suất gọi Tool và độ trễ trung bình.

### 2.4. Agent Subsystem & BaseAgent Pattern
Mọi Agent chuyên trách đều kế thừa từ lớp cơ sở `BaseAgent`:
```csharp
public abstract class BaseAgent : IAgent
{
    protected readonly ILLMGateway _llmGateway;
    protected readonly IToolDispatcher _toolDispatcher;
    protected readonly ILogger _logger;
    // Tái sử dụng logic: Logging, Parse JSON, Build Context, Dispatch Tool
}
```
- Chia tách Bounded Context nghiêm ngặt:
  - `ShoppingAgent`: Planning & Synthesis.
  - `InventoryAgent`: Kiểm tra tồn kho, số lô, hạn dùng. *Không tính tiền, không gợi ý món ăn.*
  - `PromotionAgent`: Xử lý voucher, flash sale, combo giảm giá.
  - `BudgetAgent`: Cân đối ngân sách, tự động đề xuất thay thế nguyên liệu rẻ hơn khi tổng chi vượt ngân sách.
  - `RecipeAgent`: Tra cứu công thức Cookpad. *Không sinh món ăn ảo.*
  - `CartAgent`: Thực hiện thêm, sửa, xóa giỏ hàng.

### 2.5. Business Services, Repositories & SQL Server
- **Tool**: Chỉ là cổng trung gian (Facade), không có logic.
- **Business Service**: Chứa toàn bộ Business Logic (áp dụng chính sách Flash Sale giảm 30% cho hàng cận date $\le 3$ ngày, tính mức chiết khấu).
- **Repository**: Chỉ chứa các câu lệnh LINQ sạch với EF Core, có `.AsNoTracking()`.
- **SQL Server**: Nguồn dữ liệu duy nhất lưu trữ sản phẩm, lô hàng, đơn hàng, hóa đơn.

---

## 3. LUỒNG THỰC THI ĐẦY ĐỦ (END-TO-END EXECUTION FLOW)

Tình huống: Khách hàng nhập `"Tôi có 300k, nấu lẩu cho 5 người, ưu tiên giảm giá"`:

```text
User 
  │ (Gửi prompt)
  ▼
Angular Chat UI 
  │ (HTTP POST /api/ai/chat)
  ▼
AIController 
  │ (Validate DTO)
  ▼
ConversationService 
  │ (Nạp lịch sử phiên & trạng thái giỏ)
  ▼
PromptBuilder 
  │ (Ghép Identity + Task + Fewshot + Knowledge)
  ▼
AgentOrchestrator 
  │ (Phân tích Intent: MealPlanning, Hotpot, Budget 300k)
  ▼
ShoppingAgent 
  │ (Lập kế hoạch điều phối)
  ├─► RecipeAgent    ──► ToolDispatcher ──► RecipeTool    ──► RecipeService    ──► DB
  ├─► InventoryAgent ──► ToolDispatcher ──► InventoryTool ──► InventoryService ──► DB
  ├─► PromotionAgent ──► ToolDispatcher ──► PromotionTool ──► PromotionService ──► DB
  └─► BudgetAgent    ──► ToolDispatcher ──► ProductTool   ──► ProductService   ──► DB
  │
  ▼
LLM Gateway (ILLMGateway)
  │ (Định dạng phản hồi bằng Gemini 2.5 Flash / Groq)
  ▼
AIController 
  │ (HTTP 200 OK)
  ▼
Angular Chat UI 
  │ (Hiển thị văn bản & 3 thẻ mâm cơm căn trái)
  ▼
User
```

---

## 4. CẤU TRÚC THƯ MỤC BACKEND CHUẨN CLEAN ARCHITECTURE

```text
Features/
└── AI/
    ├── Controllers/
    │   └── AIController.cs
    │
    ├── Orchestrator/
    │   ├── IAgentOrchestrator.cs
    │   └── AgentOrchestrator.cs
    │
    ├── Agents/
    │   ├── Common/
    │   │   ├── IAgent.cs
    │   │   └── BaseAgent.cs            # Kế thừa chia sẻ logging, tool call, memory
    │   ├── Shopping/
    │   ├── Inventory/
    │   ├── Promotion/
    │   ├── Recipe/
    │   ├── Budget/
    │   └── Cart/
    │
    ├── Dispatcher/
    │   ├── IToolDispatcher.cs
    │   └── ToolDispatcher.cs           # Logging, Retry, Timeout, Parallel
    │
    ├── Tools/
    │   ├── ProductTool.cs
    │   ├── InventoryTool.cs
    │   ├── PromotionTool.cs
    │   ├── RecipeTool.cs
    │   └── CartTool.cs
    │
    ├── Services/
    │   ├── ConversationService.cs      # Session Memory & Context
    │   ├── PromptBuilder.cs            # Ghép nối prompt đa tầng
    │   ├── ProductService.cs
    │   ├── InventoryService.cs
    │   └── PromotionService.cs
    │
    └── Infrastructure/
        └── External/
            └── AI/
                ├── ILLMGateway.cs      # Interface chung kết nối LLM
                ├── GeminiGateway.cs    # Google Gemini 2.5 Flash
                ├── GroqGateway.cs      # Groq Llama 3.3 70B
                └── OpenAIGateway.cs    # OpenAI GPT-4o Mini
```

---

## 5. LỘ TRÌNH TIẾN HÓA: BẢN MVP TỐI GIẢN VS ENTERPRISE MULTI-AGENT

- **Giai đoạn MVP (Khuyên dùng bước đầu)**:
  - 1 Agent duy nhất: `ShoppingAgent` (kế thừa `BaseAgent`).
  - 1 Gateway: `ILLMGateway` (kết nối `GeminiGateway` + `LocalGateway`).
  - 4 Tools: `InventoryTool`, `ProductTool`, `PromotionTool`, `CartTool` thông qua `ToolDispatcher`.
- **Giai đoạn Enterprise**:
  - Đầy đủ 6 Sub-Agents độc lập.
  - Tích hợp `GroqGateway`, `OpenAIGateway`.
  - Tích hợp RAG Vector Database cho hàng triệu công thức ẩm thực.
