# 10_AI - TÀI LIỆU 01: KIẾN TRÚC TỔNG THỂ HỆ THỐNG AI (AI ARCHITECTURE)

---

## MỤC LỤC
1. [Mô hình kiến trúc phân tầng chuẩn Enterprise (Multi-Layer Architecture)](#1-mô-hình-kiến-trúc-phân-tầng-chuẩn-enterprise)
2. [Chi tiết từng tầng trong kiến trúc (Layer Breakdown)](#2-chi-tiết-từng-tầng-trong-kiến-trúc)
3. [Luồng thực thi đầy đủ (End-to-End Execution Flow)](#3-luồng-thực-thi-đầy-đủ)
4. [Lộ trình tiến hóa: Bản MVP tối giản vs Enterprise Multi-Agent](#4-lộ-trình-tiến-hóa-bản-mvp-tối-giản-vs-enterprise-multi-agent)
5. [Cấu trúc thư mục Backend đề xuất (Clean Architecture)](#5-cấu-trúc-thư-mục-backend-đề-xuất)
6. [Cơ chế dự phòng 3 lớp (3-Tier Fallback Chain)](#6-cơ-chế-dự-phòng-3-lớp)

---

## 1. MÔ HÌNH KIẾN TRÚC PHÂN TẦNG CHUẨN ENTERPRISE

Hệ thống AI của **Smart SuperMarket** áp dụng mô hình phân tách trách nhiệm nghiêm ngặt (*Separation of Concerns*). Mọi tương tác từ người dùng đều đi qua cấu trúc phân tầng 7 lớp khép kín, tuyệt đối ngăn chặn việc Agent gọi trực tiếp Database hoặc nhúng Business Logic vào Controller.

```text
                                ┌──────────────────────────┐
                                │      Frontend Web        │
                                │    (AI Chat Component)   │
                                └─────────────┬────────────┘
                                              │
                                    HTTP / REST API
                                              │
                                              ▼
                          ┌─────────────────────────────────┐
                          │         AIController            │
                          └─────────────────────────────────┘
                                              │
                                              ▼
                          ┌─────────────────────────────────┐
                          │      Agent Orchestrator         │
                          │  (Điều phối toàn bộ Agent)      │
                          └─────────────────────────────────┘
            ┌────────────────────┼──────────────────────┐
            ▼                    ▼                      ▼
      ShoppingAgent        InventoryAgent        PromotionAgent
            │                    │                      │
            ├────────────┬───────┴──────────────┬───────┤
            ▼            ▼                      ▼
      RecipeAgent   BudgetAgent            CartAgent
                         │
                         ▼
                  Tool Dispatcher
                         │
     ┌───────────────────┼─────────────────────┐
     ▼                   ▼                     ▼
 ProductTool       InventoryTool         PromotionTool
     ▼                   ▼                     ▼
 ProductService    InventoryService      PromotionService
     ▼                   ▼                     ▼
 Repository        Repository            Repository
     ▼                   ▼                     ▼
                 SQL Server Database
```

---

## 2. CHI TIẾT TỪNG TẦNG TRONG KIẾN TRÚC

### Layer 1: Frontend Client (Angular / React Web)
- **Nhiệm vụ duy nhất**:
  - Render giao diện hội thoại (Chat UI).
  - Gửi `POST /api/ai/chat` kèm payload `{ message, conversationHistory }`.
  - Hiển thị phản hồi văn bản và các thẻ mâm cơm (Meal Option Cards) căn trái (`text-align: left`).
- **Ranh giới**: Tuyệt đối **không** xử lý logic AI, không parse giá tiền hay can thiệp vào quy tắc kinh doanh.

### Layer 2: AIController (Thin Controller)
- **Nhiệm vụ duy nhất**:
  - Tiếp nhận HTTP Request từ Client.
  - Validate tính hợp lệ của DTO đầu vào (Model Validation).
  - Chuyển giao trực tiếp cho `AgentOrchestrator`.
  - Đóng gói DTO phản hồi trả về HTTP 200 OK.
- **Ranh giới**: Controller cực kỳ mỏng, **không chứa bất kỳ Business Logic nào**.

### Layer 3: Agent Orchestrator (Bộ não điều phối trung tâm)
- Phân tích ý định người dùng (Intent Classification): Bữa ăn gia đình, tìm món lẩu, kiểm tra khuyến mãi, tra cứu calo hay đặt hàng vào giỏ.
- Lập lịch điều phối thứ tự gọi các chuyên gia Agent chuyên trách.
- Tổng hợp dữ liệu từ các Agent thành câu trả lời hoàn chỉnh và thống nhất.

### Layer 4: Multi-Agent Subsystem (Các Agent chuyên trách)
Mỗi Agent có một **Bounded Context** độc lập, tuyệt đối không lấn sân:
1. **ShoppingAgent**: Đóng vai trò tổng quản lập kế hoạch (Planner & Synthesizer). Tiếp nhận yêu cầu món ăn, xác định nguyên liệu cần tìm, yêu cầu khuyến mãi và gọi Tool để tổng hợp.
2. **InventoryAgent**: Chuyên trách kiểm tra tồn kho (`StockQuantity`), hạn sử dụng của từng lô (`Batch`), cảnh báo hàng cận date ($\le 3$ ngày) và đề xuất sản phẩm thay thế tương đương. *Không tính tiền, không tìm Recipe.*
3. **PromotionAgent**: Chuyên trách kiểm tra Voucher, chiến dịch Flash Sale, chiết khấu giờ vàng và giảm giá combo.
4. **BudgetAgent**: Chuyên trách tính toán tổng chi phí mâm cơm, so sánh với ngân sách kỳ vọng của khách hàng. Nếu vượt ngân sách, đề xuất thay thế nguyên liệu đắt bằng nguyên liệu giá rẻ hơn.
5. **RecipeAgent**: Tra cứu công thức nấu nướng từ cơ sở dữ liệu cẩm nang ẩm thực (Cookpad). *Tuyệt đối không bịa công thức hay sinh ngẫu nhiên.*
6. **CartAgent**: Quản lý thao tác nạp mâm cơm vào giỏ, cập nhật số lượng, xóa món hoặc ước tính tạm tính.

### Layer 5: Tool Layer (Lớp công cụ chuẩn hóa)
- **Quy tắc bất biến**: **Agent KHÔNG ĐƯỢC gọi Service trực tiếp.** Agent phải thông qua Tool Layer.
- Tool đóng vai trò là Function Calling Interface cho LLM.
- Các Tool (`ProductTool`, `InventoryTool`, `PromotionTool`, `RecipeTool`, `CartTool`):
  - Chỉ chứa chữ ký phương thức: `GetStock()`, `GetNearExpiry()`, `GetAlternative()`, `GetAvailableBatch()`.
  - **Không chứa Business Logic.**

### Layer 6: Service Layer (Nghiệp vụ cốt lõi)
- Chứa toàn bộ Business Logic của hệ thống bán lẻ:
  - `InventoryService`: Lọc các lô hàng $\le 3$ ngày hạn sử dụng để kích hoạt trạng thái Flash Sale.
  - `PromotionService`: Tính toán phần trăm chiết khấu, kiểm tra điều kiện áp mã voucher `FRESH20`.
  - `RecipeService`: Khớp các từ khóa món ăn và chuẩn hóa định lượng nguyên liệu.

### Layer 7: Repository & Database Layer
- **Repository**: Chỉ thực thi các truy vấn Entity Framework Core sạch: `.Where()`, `.Include()`, `.AsNoTracking()`, `.Select()`.
- **Database**: CSDL quan hệ SQL Server lưu trữ bảng dữ liệu nguồn: `Products`, `ProductBatches`, `Categories`, `Promotions`, `Orders`, `Recipes`.

---

## 3. LUỒNG THỰC THI ĐẦY ĐỦ (END-TO-END EXECUTION FLOW)

### Tình huống: Khách có 300,000 đ muốn nấu lẩu cho 5 người, ưu tiên giảm giá

```text
User: "Tôi có 300k, muốn nấu lẩu cho 5 người, có giảm giá thì ưu tiên nhé"
  │
  ▼
[Angular / React Frontend] ──(POST /api/ai/chat)──► [AIController]
                                                           │
                                                           ▼
                                                 [AgentOrchestrator]
                                                           │
                                                           ▼
                                                   [ShoppingAgent]
                                                           │
      ┌────────────────────────────────────────────────────┤
      ▼ Bước 1: Tra cứu công thức                          │
[RecipeAgent] ──► [RecipeTool] ──► [RecipeService] ──► [RecipeRepo] ──► Trả về nguyên liệu lẩu
                                                           │
      ┌────────────────────────────────────────────────────┤
      ▼ Bước 2: Kiểm tra tồn kho & cận date                │
[InventoryAgent] ──► [InventoryTool] ──► [InventoryService] ──► [InventoryRepo] ──► Lọc lô thịt bò cận date
                                                           │
      ┌────────────────────────────────────────────────────┤
      ▼ Bước 3: Áp dụng khuyến mãi & voucher               │
[PromotionAgent] ──► [PromotionTool] ──► [PromotionService] ──► [PromotionRepo] ──► Giảm 30% thịt cận date
                                                           │
      ┌────────────────────────────────────────────────────┤
      ▼ Bước 4: Kiểm tra & Cân đối ngân sách               │
[BudgetAgent] ──► [BudgetTool] ──► [BudgetService] ──► Tổng chi: 285k (<= 300k, đạt yêu cầu!)
                                                           │
                                                           ▼
[ShoppingAgent] ◄── Tổng hợp kết quả & định dạng 3 mâm cơm Cookpad
      │
      ▼
[AgentOrchestrator] ──► [AIController] ──(HTTP 200 OK)──► [Frontend Client] ──► Hiển thị cho User
```

---

## 4. LỘ TRÌNH TIẾN HÓA: BẢN MVP TỐI GIẢN VS ENTERPRISE MULTI-AGENT

Để tránh phức tạp hóa hệ thống trong giai đoạn phát triển ban đầu, dự án chia thành 2 mốc kiến trúc rõ ràng:

### Giai đoạn MVP (Khuyên dùng cho bước đầu)
Chỉ tập trung vào 1 Agent điều phối trung tâm và 4 Tools cốt lõi:
1. **ShoppingAgent** (Đảm nhiệm điều phối quy trình và tổng hợp)
2. **InventoryTool** (Tra cứu tồn kho, kiểm tra cận date)
3. **ProductTool** (Tìm kiếm danh mục, thông tin sản phẩm)
4. **PromotionTool** (Tra cứu voucher, flash sale giảm giá)
5. **CartTool** (Nạp nhanh nguyên liệu vào giỏ hàng)

### Giai đoạn Enterprise (Multi-Agent hoàn chỉnh)
- Tách rời `InventoryAgent`, `PromotionAgent`, `BudgetAgent`, `RecipeAgent` thành các micro-services hoặc background workers độc lập.
- Bổ sung RAG (Retrieval-Augmented Generation) kết hợp Vector Database cho hàng chục ngàn công thức nấu ăn.

---

## 5. CẤU TRÚC THƯ MỤC BACKEND ĐỀ XUẤT

```text
Features/
└── AI/
    ├── Controllers/
    │   └── AIController.cs             # Thin Controller cho AI Endpoint
    │
    ├── Orchestrator/
    │   ├── IAgentOrchestrator.cs       # Interface điều phối
    │   └── AgentOrchestrator.cs        # Bộ não phân tích intent & điều phối
    │
    ├── Agents/
    │   ├── Shopping/
    │   │   ├── IShoppingAgent.cs
    │   │   └── ShoppingAgent.cs
    │   ├── Inventory/
    │   │   ├── IInventoryAgent.cs
    │   │   └── InventoryAgent.cs
    │   ├── Promotion/
    │   │   ├── IPromotionAgent.cs
    │   │   └── PromotionAgent.cs
    │   ├── Recipe/
    │   │   ├── IRecipeAgent.cs
    │   │   └── RecipeAgent.cs
    │   ├── Budget/
    │   │   ├── IBudgetAgent.cs
    │   │   └── BudgetAgent.cs
    │   └── Cart/
    │       ├── ICartAgent.cs
    │       └── CartAgent.cs
    │
    ├── Tools/
    │   ├── ProductTool.cs              # Search(), GetById(), GetAlternative()
    │   ├── InventoryTool.cs            # GetStock(), GetNearExpiry(), GetAvailableBatch()
    │   ├── PromotionTool.cs            # GetVouchers(), CheckFlashSale()
    │   ├── RecipeTool.cs               # GetRecipe(), GetIngredients()
    │   ├── CartTool.cs                 # AddToCart(), EstimateCart()
    │   └── CustomerTool.cs             # GetPreferences(), GetOrderHistory()
    │
    ├── Services/
    │   ├── AiService.cs                # Core AI business logic
    │   ├── ConversationService.cs      # Quản lý phiên hội thoại & Memory
    │   ├── PromptService.cs            # Nạp & định dạng System Prompts
    │   └── LlmGatewayService.cs        # Kết nối Gemini 2.5 Flash / Groq / Fallback
    │
    ├── Prompts/                        # Lưu trữ file markdown system prompt
    ├── DTOs/                           # Data Transfer Objects (Strictly no comments)
    ├── Memory/                         # Session & Context Cache
    ├── Contracts/                      # Event & Message Contracts
    └── Models/                         # Domain Entities cho AI
```

---

## 6. CƠ CHẾ DỰ PHÒNG 3 LỚP (3-TIER FALLBACK CHAIN)

Dù ở phiên bản MVP hay Enterprise, cơ chế phòng hộ vẫn hoạt động với 3 lớp kiên cố:
- **Lớp 1 (Chính)**: `Google Gemini 2.5 Flash` (Thời gian phản hồi ~1.2s, độ chính xác ngôn ngữ cao).
- **Lớp 2 (Dự phòng đám mây)**: `Groq LPU Llama-3.3-70b-versatile` (Kích hoạt khi Gemini gặp lỗi 429 hoặc timeout > 4s).
- **Lớp 3 (Dự phòng cục bộ)**: `Local Pre-compiled Rule Engine` (Chạy hoàn toàn bằng code C# và cache dữ liệu sẵn trong RAM, đảm bảo hệ thống phản hồi trong 150ms ngay cả khi rớt mạng Internet).
