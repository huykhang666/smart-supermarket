# SmartSuperMarket-AI-Docs

> **Hệ sinh thái Tài liệu Kỹ thuật Toàn diện cho Phân hệ Trí tuệ Nhân tạo (AI) - Smart SuperMarket**

---

## 📌 MỤC LỤC TỔNG QUAN

Hệ thống tài liệu được tổ chức theo chuẩn Enterprise AI Architecture, bao gồm 10 tài liệu nền tảng, 6 sơ đồ kiến trúc Mermaid, 7 bộ System Prompts chuyên biệt, 6 bộ dữ liệu mẫu thực tế và 3 biểu mẫu chuẩn hóa để mở rộng hệ thống.

```text
SmartSuperMarket-AI-Docs/
│
├── README.md                       # Trang chỉ mục & hướng dẫn điều hướng
│
├── 00_AI_Vision.md                 # ⭐ Mục tiêu & phạm vi
├── 01_AI_Architecture.md           # ⭐ Kiến trúc tổng thể
├── 02_Agent_Design.md              # ⭐ Thiết kế Multi-Agent
├── 03_Tool_Calling.md              # ⭐ Tool Calling Design
├── 04_Database_Mapping.md          # ⭐ Mapping DB → Service → Tool
├── 05_System_Prompt.md             # ⭐ Luật của AI
├── 06_Workflow.md                  # ⭐ Luồng xử lý
├── 07_API_Contract.md              # ⭐ DTO + API
├── 08_Coding_Guideline.md          # ⭐ Quy tắc code
├── 09_Roadmap.md                   # ⭐ Kế hoạch triển khai
│
├── diagrams/                       # Thư mục sơ đồ kiến trúc Mermaid (.mmd)
│   ├── architecture.mmd            # Sơ đồ khối kiến trúc đa tầng
│   ├── sequence_chat.mmd           # Sơ đồ tuần tự tương tác trò chuyện
│   ├── sequence_recipe.mmd         # Sơ đồ tuần tự gợi ý công thức Cookpad
│   ├── sequence_cart.mmd           # Sơ đồ tuần tự nạp mâm vào giỏ hàng
│   ├── class_agent.mmd             # Sơ đồ lớp (Class Diagram) các AI Service
│   └── database_mapping.mmd        # Sơ đồ luồng ánh xạ CSDL vào Context
│
├── prompts/                        # Thư mục System Prompts chuyên biệt
│   ├── system.md                   # Hiến pháp & quy tắc chung của AI
│   ├── shopping.md                 # Prompt trợ lý đi chợ & tìm kiếm hàng hóa
│   ├── recipe.md                   # Prompt đầu bếp Cookpad Việt Nam
│   ├── inventory.md                # Prompt phân tích rủi ro kho & hạn sử dụng
│   ├── promotion.md                # Prompt Flash Sale giải cứu cận date
│   ├── budget.md                   # Prompt cân đối ngân sách (30k - 350k)
│   └── nutrition.md                # Prompt cân bằng dinh dưỡng gia đình
│
├── examples/                       # Thư mục kịch bản & dữ liệu mẫu thực tế
│   ├── shopping_examples.md        # Đối thoại đi chợ thực tế
│   ├── recipe_examples.md          # Các mâm cơm Cookpad mẫu
│   ├── promotion_examples.md       # Tình huống gợi ý hàng cận date giảm 30%
│   ├── inventory_examples.md       # Báo cáo rủi ro kho & phiếu nhập nháp
│   ├── api_examples.md             # Bộ mẫu Request/Response JSON hoàn chỉnh
│   └── edge_cases.md               # Xử lý tình huống ngoại lệ & biên
│
└── templates/                      # Thư mục biểu mẫu chuẩn hóa mở rộng
    ├── agent_template.md           # Mẫu khai báo Agent mới
    ├── tool_template.md            # Mẫu khai báo Tool / Function mới
    └── workflow_template.md        # Mẫu thiết kế quy trình AI mới
```

---

## 🎯 CÁC TRỤ CỘT NGHIỆP VỤ CHÍNH

1. **Đầu Bếp & Trợ Lý Đi Chợ (Shopping Concierge)**:
   - Tự động nhận diện ngân sách từ sinh viên (30k - 50k) đến đại tiệc gia đình (350k).
   - Gợi ý 3 phương án mâm cơm chuẩn vị Cookpad Việt Nam với sản phẩm thực tế từ kho.
   - 1-Click mở khay mâm cơm tùy chỉnh định lượng và nạp trực tiếp vào giỏ hàng Web.

2. **Giám Sát Hạn Dùng & Flash Sale Tự Động (Shelf-Life & Expiry Waste)**:
   - Tự động quét hạn dùng các lô hàng lúc 06:00 AM hàng ngày.
   - Gắn nhãn giảm giá 30% cho thực phẩm $\le 7$ ngày và ưu tiên ghép vào thực đơn tiết kiệm.

3. **Quản Trị Rủi Ro Kho & Lập Phiếu Nhập Nháp (Retail Operations Copilot)**:
   - Phân tích rổ hàng kết hợp (Market Basket Analysis) tìm các combo bán kèm tại quầy POS.
   - Tự động phát hiện sản phẩm dưới ngưỡng an toàn `MinStockLevel` và lập phiếu nhập kho nháp.
   - 1-Click Commit phiếu nhập vào CSDL quản lý.

4. **Kiến Trúc Fallback 3 Lớp (High Resilience)**:
   - `Google Gemini 2.5 Flash` $\rightarrow$ `Groq Cloud LPU Llama 3.3` $\rightarrow$ `Local Cookpad Rule Engine`.
   - Đảm bảo 100% thời gian hoạt động thông suốt ngay cả khi mất kết nối Internet.
