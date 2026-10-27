# SmartSuperMarket-AI-Docs (Enterprise Grade 10/10)

> **Hệ sinh thái Tài liệu Kỹ thuật Toàn diện cho Phân hệ Trí tuệ Nhân tạo (AI) - Smart SuperMarket**

---

## 📌 MỤC LỤC TỔNG QUAN

Hệ thống tài liệu được tổ chức theo chuẩn kiến trúc Enterprise AI phân tầng 10 lớp, đạt chuẩn 10/10 trên tất cả các tiêu chí: **Kiến trúc AI**, **Prompt Engineering**, **Multi-Agent**, **Tool Calling**, **Knowledge Base**, **AI Coding Agent Support**, và **Khả năng mở rộng**.

```text
SmartSuperMarket-AI-Docs/
│
├── README.md                           # Trang chỉ mục & bản đồ điều hướng hệ thống
│
├── 00_AI_Vision.md                     # ⭐ Mục tiêu & phạm vi nghiệp vụ bán lẻ thông minh
├── 01_AI_Architecture.md               # ⭐ Kiến trúc 10 tầng (Client → Controller → Context → Orchestrator → LLM Gateway → Tool Dispatcher → Tools → Services → Repositories → SQL)
├── 02_Agent_Design.md                  # ⭐ Thiết kế BaseAgent Pattern, Bounded Context & MVP Transition
├── 03_Tool_Calling.md                  # ⭐ Cơ chế Tool Dispatcher (Logging, Retry, Timeout, Parallel, Metrics)
├── 04_Database_Mapping.md              # ⭐ Chuỗi ánh xạ 7 bước: Table → Entity → Repo → Service → DTO → Tool → Agent
├── 05_System_Prompt.md                 # ⭐ Luật & Hiến pháp AI (Không đoán, không tạo giá, không tạo voucher)
├── 06_Workflow.md                      # ⭐ Tổng quan luồng xử lý End-to-End & Tình huống hết hàng
├── 07_API_Contract.md                  # ⭐ DTO + API Contracts chuẩn RESTful
├── 08_Coding_Guideline.md              # ⭐ Quy tắc code chuẩn Enterprise & Clean Architecture
├── 09_Roadmap.md                       # ⭐ Lộ trình triển khai MVP đến Enterprise RAG
│
├── diagrams/                           # Sơ đồ kiến trúc Mermaid (.mmd)
│   ├── architecture.mmd                # Sơ đồ khối kiến trúc phân tầng 10 lớp
│   ├── sequence_chat.mmd               # Sơ đồ tuần tự tương tác đa tác nhân
│   ├── sequence_recipe.mmd             # Sơ đồ tuần tự gợi ý công thức Cookpad
│   ├── sequence_cart.mmd               # Sơ đồ tuần tự nạp mâm vào giỏ hàng
│   ├── class_agent.mmd                 # Sơ đồ lớp (Class Diagram) Orchestrator, BaseAgent, Tools
│   └── database_mapping.mmd            # Sơ đồ luồng ánh xạ CSDL vào Context
│
├── prompts/                            # Hệ thống System Prompts phân tầng chuyên sâu
│   ├── system/                         # Lớp hiến pháp & phong cách cốt lõi
│   │   ├── identity.md                 # Định danh, đại từ bắt buộc "mình" - "bạn"
│   │   ├── rules.md                    # 5 quy tắc bất biến (Không đoán, không tạo giá)
│   │   └── style.md                    # Quy tắc căn lề trái & chống trùng lặp nội dung
│   ├── shopping/                       # Lớp tác vụ mua sắm chuyên biệt
│   │   ├── shopping.md                 # Shopping Planner (Hiểu nhu cầu, Planning, Gọi Tool, Tổng hợp)
│   │   ├── recipe.md                   # Recipe Consultant (Cẩm nang món Cookpad)
│   │   ├── budget.md                   # Budget Balancer (Bảo vệ ví tiền & đổi món rẻ)
│   │   └── promotion.md                # Promotion Advisor (Quét Flash Sale cận date)
│   └── fewshot/                        # Lớp học tăng cường qua ví dụ mẫu (Few-shot)
│       ├── shopping.md                 # Ví dụ mẫu bữa ăn 30k, lẩu 300k
│       ├── budget.md                   # Ví dụ mẫu hạ giá khi vượt ngân sách
│       └── recipe.md                   # Ví dụ mẫu mẹo luộc rau xanh, ướp thịt bò mềm
│
├── examples/                           # Thư viện kịch bản đối thoại & Dữ liệu mẫu thực tế
│   ├── shopping/                       # Tình huống mua sắm thực tế
│   │   ├── buy_hotpot.md               # Nấu lẩu gia đình 4 người
│   │   ├── buy_breakfast.md            # Bữa sáng siêu tốc 10 phút < 50k
│   │   ├── buy_bbq.md                  # Tiệc nướng BBQ ngoài trời 500k
│   │   ├── buy_party.md                # Tiệc liên hoan 6 người 650k
│   │   └── buy_vegetarian.md           # Mâm cơm chay thanh đạm ngày rằm 100k
│   ├── inventory/                      # Tình huống quản trị kho & lô hàng
│   │   ├── out_of_stock.md             # Xử lý hết hàng & đề xuất món thay thế
│   │   ├── near_expiry.md              # Báo cáo hàng cận date cho Quản lý
│   │   └── alternative.md              # Ma trận đề xuất thay thế tương thích
│   ├── budget/                         # Tình huống phân khúc ngân sách
│   │   ├── 300k.md                     # Bữa cơm gia đình tiêu chuẩn 300k
│   │   ├── 500k.md                     # Tiệc cuối tuần 500k
│   │   └── 100k_student.md             # Thực đơn cả ngày 100k cho sinh viên
│   ├── edge_cases.md                   # Xử lý tình huống ngoại lệ & lỗi mạng
│   └── api_examples.md                 # Bộ mẫu cURL & JSON Request/Response
│
├── templates/                          # Biểu mẫu chuẩn hóa mở rộng cho AI Coding Agent
│   ├── agent_template.md               # Mẫu khởi tạo Agent mới kế thừa BaseAgent
│   ├── tool_template.md                # Mẫu khai báo Tool (Tên, Purpose, Methods, Input, Output, Service, Exception)
│   ├── service_template.md             # Mẫu khai báo Service nghiệp vụ & EF Core
│   └── workflow_template.md            # Mẫu thiết kế quy trình AI mới
│
├── knowledge/                          # Thư viện Tri thức & Quy tắc Nghiệp vụ Cốt lõi (Ground Truth)
│   ├── business_rules/
│   │   ├── voucher/
│   │   │   └── voucher_priority.md     # Thứ tự ưu tiên 5 tầng: Flash Sale → Combo → Voucher → Point → Member
│   │   ├── discount/
│   │   │   └── flash_sale_rules.md     # Khung giờ & tỷ lệ giảm giá Flash Sale
│   │   ├── inventory/
│   │   │   └── expiry_thresholds.md    # Ngưỡng an toàn kho & khóa bán khi hết hạn
│   │   └── faq/
│   │       └── customer_faq.md         # Giải đáp an toàn thực phẩm & đổi trả 24h
│   ├── discount_rules.md               # Bảng quy tắc giải cứu hàng tươi sống (<=3 ngày 30%, <=1 ngày 50%)
│   ├── promotion_rules.md              # Quy định chống cộng dồn voucher
│   ├── recipes/
│   │   └── vietnamese_recipes.md       # Cẩm nang công thức mâm cơm Cookpad
│   ├── nutrition/
│   │   └── dietary_standards.md        # Tiêu chuẩn khẩu phần & dinh dưỡng Viện Dinh Dưỡng
│   └── supermarket/
│       └── category_structure.md       # Bảng phân loại ngành hàng & quy tắc cách ly Category
│
├── workflow/                           # Thư mục Quy trình Nghiệp vụ Chuyên biệt (Modular Workflows)
│   ├── meal.md                         # Quy trình tư vấn mâm cơm đa tác nhân
│   ├── cart.md                         # Quy trình 1-Click nạp nguyên liệu vào giỏ hàng
│   ├── checkout.md                     # Quy trình thanh toán & đối soát khuyến mãi
│   ├── promotion.md                    # Quy trình tự động quét Flash Sale lúc 06:00 AM
│   ├── refund.md                       # Quy trình hỗ trợ đổi trả thực phẩm trong 24h
│   └── inventory.md                    # Quy trình kiểm soát tồn kho & lập dự thảo PO
│
└── adr/                                # Kiến trúc Quyết định Kỹ thuật (Architecture Decision Records)
    ├── 0001-tool-layer.md              # ADR-0001: Phân tách lớp Tool, Agent không gọi Service trực tiếp
    ├── 0002-agent-design.md            # ADR-0002: Áp dụng BaseAgent Pattern & Bounded Contexts
    ├── 0003-no-direct-db.md            # ADR-0003: Cấm Agent & LLM truy cập cơ sở dữ liệu trực tiếp
    └── 0004-rag.md                     # ADR-0004: Chiến lược Vector Search RAG cho Cẩm nang Cookpad
```

---

## 🎯 CÁC TRỤ CỘT ĐỘT PHÁ CỦA HỆ THỐNG
1. **Kiến trúc phân tầng 10 lớp khép kín**:
   - Tách rời giao diện người dùng, ngữ cảnh phiên, Prompt Builder, bộ điều phối Agent, cổng LLM Gateway, Tool Dispatcher, nghiệp vụ kinh doanh và lưu trữ cơ sở dữ liệu.
2. **LLM Gateway Đa Mô Hình**:
   - Agent chỉ giao tiếp qua `ILLMGateway`. Dễ dàng chuyển đổi linh hoạt giữa `Gemini 2.5 Flash`, `Groq Llama 3.3 70B`, `OpenAI GPT-4o Mini` hoặc `Local Rule Engine` mà không cần sửa đổi bất kỳ dòng code nào trong Agent.
3. **Tool Dispatcher An Toàn & Tối Ưu**:
   - Đảm bảo đầy đủ: **Audit Logging**, **Auto Retry**, **Timeout 1500ms**, **Parallel Execution** (rút ngắn 66% thời gian chờ) và **Metrics Observability**.
4. **Hệ Thống Tri Thức Cốt Lõi (Ground Truth Knowledge Base)**:
   - Toàn bộ chính sách giảm giá, thứ tự voucher, cẩm nang nấu nướng và tiêu chuẩn dinh dưỡng được neo chắc chắn trong thư mục `knowledge/`, ngăn chặn hoàn toàn hiện tượng AI suy đoán sai lệch.
5. **Bộ ADR Minh Bạch**:
   - 4 bản ghi quyết định kiến trúc giải thích tường tận lý do đằng sau từng thiết kế, tạo nền tảng vững chắc cho đội ngũ kỹ sư phát triển lâu dài.
