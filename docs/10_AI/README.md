# SmartSuperMarket-AI-Docs

> **Hệ sinh thái Tài liệu Kỹ thuật Toàn diện cho Phân hệ Trí tuệ Nhân tạo (AI) - Smart SuperMarket**

---

## 📌 MỤC LỤC TỔNG QUAN

Hệ thống tài liệu được tổ chức theo chuẩn Enterprise Multi-Layer AI Architecture, bao gồm:
- **10 tài liệu nền tảng** (00 đến 09).
- **6 sơ đồ kiến trúc Mermaid** (.mmd).
- **7 bộ System Prompts** chuyên trách cho LLM.
- **7 bộ dữ liệu mẫu thực tế** (Happy path, Multi-turn, Edge cases, Budget, Inventory, Promotion, API).
- **4 biểu mẫu chuẩn hóa** (Agent, Tool, Service, Workflow).
- **5 tài liệu Tri thức Cốt lõi** (Knowledge Base) định hình quy tắc giảm giá, khuyến mãi và công thức ẩm thực.

```text
SmartSuperMarket-AI-Docs/
│
├── README.md                       # Trang chỉ mục & hướng dẫn điều hướng
│
├── 00_AI_Vision.md                 # ⭐ Mục tiêu & phạm vi nghiệp vụ
├── 01_AI_Architecture.md           # ⭐ Kiến trúc 7 lớp (Frontend → Controller → Orchestrator → Agents → Tools → Services → DB)
├── 02_Agent_Design.md              # ⭐ Thiết kế Bounded Context Multi-Agent & MVP Transition
├── 03_Tool_Calling.md              # ⭐ Cơ chế Tool Dispatcher & Nguyên tắc cách ly Logic
├── 04_Database_Mapping.md          # ⭐ Mapping DB → Service → Tool & Category Isolation
├── 05_System_Prompt.md             # ⭐ Luật & Hiến pháp của AI (Không đoán dữ liệu, không tạo giá)
├── 06_Workflow.md                  # ⭐ Luồng xử lý End-to-End & Tình huống hết hàng
├── 07_API_Contract.md              # ⭐ DTO + API Contracts chuẩn RESTful
├── 08_Coding_Guideline.md          # ⭐ Quy tắc code chuẩn Enterprise & Clean Code
├── 09_Roadmap.md                   # ⭐ Lộ trình triển khai MVP đến Enterprise RAG
│
├── diagrams/                       # Thư mục sơ đồ kiến trúc Mermaid (.mmd)
│   ├── architecture.mmd            # Sơ đồ khối kiến trúc phân tầng 7 lớp
│   ├── sequence_chat.mmd           # Sơ đồ tuần tự tương tác đa tác nhân
│   ├── sequence_recipe.mmd         # Sơ đồ tuần tự gợi ý công thức Cookpad
│   ├── sequence_cart.mmd           # Sơ đồ tuần tự nạp mâm vào giỏ hàng
│   ├── class_agent.mmd             # Sơ đồ lớp (Class Diagram) Orchestrator & Tools
│   └── database_mapping.mmd        # Sơ đồ luồng ánh xạ CSDL vào Context
│
├── prompts/                        # Thư mục System Prompts chuyên trách cho LLM
│   ├── system.md                   # Hiến pháp chung của hệ thống AI (Identity, Rules, Output)
│   ├── shopping.md                 # Shopping Planner (Hiểu nhu cầu, Planning, Gọi Tool, Tổng hợp)
│   ├── recipe.md                   # Prompt đầu bếp Cookpad Việt Nam
│   ├── inventory.md                # Inventory Assistant (Kiểm tra tồn, Batch, Flash Sale - Không tính tiền)
│   ├── promotion.md                # Prompt Flash Sale giải cứu cận date
│   ├── budget.md                   # Budget Assistant (Cân đối ngân sách, đề xuất thay thế tiết kiệm)
│   └── nutrition.md                # Prompt cân bằng dinh dưỡng gia đình
│
├── examples/                       # Thư mục kịch bản & dữ liệu mẫu thực tế
│   ├── shopping_examples.md        # Happy Path, Multi-turn hội thoại giữ ngữ cảnh & Invalid input
│   ├── recipe_examples.md          # Các mâm cơm Cookpad mẫu
│   ├── promotion_examples.md       # Tình huống gợi ý hàng cận date giảm 30% Flash Sale
│   ├── inventory_examples.md       # Báo cáo rủi ro kho & dự thảo phiếu nhập PO
│   ├── budget_examples.md          # Tình huống cân đối ngân sách khi vượt chi (Over Budget)
│   ├── api_examples.md             # Bộ mẫu cURL & JSON Request/Response hoàn chỉnh
│   └── edge_cases.md               # Xử lý hết hàng (Out of Stock), ngân sách thấp, timeout
│
├── templates/                      # Thư mục biểu mẫu chuẩn hóa mở rộng
│   ├── agent_template.md           # Mẫu khai báo Agent mới
│   ├── tool_template.md            # Mẫu chuẩn (Tên, Purpose, Methods, Input, Output, Service, Exception)
│   ├── service_template.md         # Mẫu khai báo Service nghiệp vụ (Business logic & EF Core)
│   └── workflow_template.md        # Mẫu thiết kế quy trình AI mới
│
└── knowledge/                      # Thư mục Tri thức & Quy tắc Nghiệp vụ Cốt lõi
    ├── discount_rules.md           # Quy tắc Flash Sale cận date (<=3 ngày 30%, <=1 ngày 50%)
    ├── promotion_rules.md          # Thứ tự ưu tiên & chống cộng dồn voucher
    ├── recipes/
    │   └── vietnamese_recipes.md   # Cẩm nang mâm cơm gia đình Cookpad
    ├── nutrition/
    │   └── dietary_standards.md    # Tiêu chuẩn khẩu phần & dinh dưỡng Viện Dinh Dưỡng
    └── supermarket/
        └── category_structure.md   # Bảng phân loại ngành hàng & quy tắc cách ly
```

---

## 🎯 CÁC TRỤ CỘT KIẾN TRÚC ĐẶC BIỆT

1. **Ranh giới phân tầng bất biến**:
   $$\text{User} \longrightarrow \text{Frontend} \longrightarrow \text{AIController} \longrightarrow \text{AgentOrchestrator} \longrightarrow \text{Agents} \longrightarrow \text{Tools} \longrightarrow \text{Services} \longrightarrow \text{Repositories} \longrightarrow \text{SQL Database}$$
   - Agent **tuyệt đối không gọi Service trực tiếp** và không truy cập Database.
   - Tool **hoàn toàn không chứa Business Logic**.
   - Toàn bộ Business Logic được tập trung tại **Service Layer**.

2. **Chiến lược tiến hóa từ MVP đến Enterprise**:
   - **Giai đoạn MVP**: Chỉ 1 `ShoppingAgent` điều phối trung tâm kết hợp 4 Tools (`InventoryTool`, `ProductTool`, `PromotionTool`, `CartTool`).
   - **Giai đoạn Enterprise**: Mở rộng toàn bộ 6 Sub-Agents độc lập, tích hợp RAG Vector Database cho hàng triệu công thức Cookpad.

3. **Knowledge Base làm Ground Truth**:
   - Mọi mức giảm giá và quy tắc Flash Sale được neo cố định trong thư mục `knowledge/`, ngăn chặn hoàn toàn việc AI suy đoán giá sai lệch.
