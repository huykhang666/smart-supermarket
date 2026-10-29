# 01_AI_Architecture.md

# AI Architecture

Version: 1.1

---

# 1. Overview

AI Shopping Concierge là một module độc lập trong hệ thống Smart SuperMarket.

AI không thay thế Business Layer hiện có mà đóng vai trò điều phối (Orchestrator) giữa người dùng và hệ thống.

Mọi dữ liệu được sử dụng để trả lời khách hàng phải đến từ dữ liệu thật trong hệ thống.

AI không được phép truy cập trực tiếp Database hoặc Repository.

---

# 2. Architecture Goals

Kiến trúc AI được xây dựng với các mục tiêu sau:

- Dễ mở rộng (Extensible)
- Dễ bảo trì (Maintainable)
- Dễ kiểm thử (Testable)
- Tái sử dụng Business Logic hiện có
- Hạn chế Hallucination
- Không trùng lặp Business Logic
- Hỗ trợ nhiều AI Agent trong tương lai
- Hỗ trợ Multi-tier LLM Gateway và Tool Dispatcher an toàn

---

# 3. Architecture Principles

Toàn bộ AI Module phải tuân thủ các nguyên tắc sau:

## 3.1 Modular Architecture

AI được chia thành nhiều module độc lập.

Ví dụ:

- Shopping
- Inventory
- Promotion
- Recipe
- Budget
- Cart

Mỗi module chỉ chịu trách nhiệm một nghiệp vụ.

---

## 3.2 Single Responsibility Principle

Một Agent chỉ có một nhiệm vụ.

Ví dụ:

- ShoppingAgent: hiểu yêu cầu và lập kế hoạch tổng thể
- InventoryAgent: kiểm tra tồn kho, batch, cận date
- PromotionAgent: kiểm tra voucher, flash sale, chiết khấu
- RecipeAgent: tra cứu món ăn và nguyên liệu
- BudgetAgent: so sánh tổng tiền với ngân sách
- CartAgent: thao tác giỏ hàng

Không Agent nào được làm thay nhiệm vụ của Agent khác.

---

## 3.3 Tool Calling Architecture

Agent không được phép truy cập Service trực tiếp.

Mọi nghiệp vụ phải đi qua Tool Layer.

Luồng chuẩn:

Agent

↓

Tool

↓

Business Service

↓

Repository

↓

Database

---

## 3.4 Business Logic Reuse

AI không được viết lại Business Logic.

Nếu `InventoryService` hoặc `PromotionService` đã tồn tại thì phải tái sử dụng.

Không tạo `InventoryServiceAI` hay `PromotionServiceAI`.

---

## 3.5 No Direct Database Access

AI không được phép:

- Query SQL
- Query DbContext
- Query Repository

Chỉ Tool mới được gọi Service.

---

## 3.6 Dependency Injection

Toàn bộ component phải sử dụng Dependency Injection.

Không khởi tạo object bằng `new` trong Business Flow.

---

# 4. High-Level Architecture

Luồng tổng thể của hệ thống AI:

User  
→ Angular Chat UI  
→ AIController  
→ ConversationService  
→ PromptBuilder  
→ AgentOrchestrator  
→ LLM Gateway  
→ Tool Dispatcher  
→ Tools  
→ Business Services  
→ Repositories  
→ SQL Server

---

# 5. AI Module Structure

AI Module được tổ chức theo Feature-Based Architecture.

Features/

└── AI/

├── Controllers/

├── DTOs/

├── Agents/

├── Tools/

├── Services/

├── Prompts/

├── Memory/

├── Contracts/

├── Models/

├── Constants/

---

# 6. Layer Responsibilities

## Controller
- Nhận HTTP Request
- Validate Request
- Gọi ConversationService / AgentOrchestrator
- Trả Response

Không chứa Business Logic.

## ConversationService
- Quản lý session hội thoại
- Giữ context ngắn hạn
- Ghép dữ liệu hội thoại vào pipeline xử lý

## PromptBuilder
- Ghép Identity
- Ghép Rules
- Ghép Few-shot
- Ghép Knowledge
- Ghép Conversation Context
- Ghép User Input

## AgentOrchestrator
- Phân tích intent
- Chọn Agent phù hợp
- Điều phối Agent
- Tổng hợp kết quả

Không chứa Business Logic.

## Agent
- Lập luận
- Lập kế hoạch
- Gọi Tool phù hợp
- Tổng hợp kết quả

Agent không được:
- Query Database
- Query Repository
- Gọi DbContext
- Viết Business Logic

## Tool
- Là cầu nối giữa AI và Business Layer
- Gọi Service
- Chuyển đổi DTO
- Chuẩn hóa dữ liệu

Tool không chứa Business Logic.

## Business Service
- Là nơi duy nhất chứa Business Logic

Ví dụ:
- ProductService
- InventoryService
- PromotionService
- CustomerService
- OrderService

## Repository
- Chỉ thực hiện Query / Insert / Update / Delete
- Không chứa Business Logic

## Database
- Là nguồn dữ liệu duy nhất

AI không được phép truy cập trực tiếp.

---

# 7. Request Flow

Ví dụ người dùng hỏi:

“Tôi có 300.000 đồng, hãy gợi ý bữa tối.”

Luồng xử lý:

User  
→ Angular  
→ POST /api/v1/ai/chat  
→ AIController  
→ ConversationService  
→ PromptBuilder  
→ AgentOrchestrator  
→ ShoppingAgent  
→ RecipeTool  
→ RecipeService  
→ InventoryTool  
→ InventoryService  
→ PromotionTool  
→ PromotionService  
→ BudgetTool  
→ Response Builder  
→ AIController  
→ Angular  
→ User

---

# 8. Data Flow

Luồng dữ liệu chuẩn:

Question  
→ Intent Analysis  
→ Planning  
→ Tool Calling  
→ Business Service  
→ Repository  
→ Database  
→ Business Service  
→ Tool  
→ Agent  
→ Response

---

# 9. Dependency Rules

Được phép:

Controller → ConversationService → PromptBuilder → Orchestrator → Agent → Tool → Service → Repository → DbContext

Không được phép:

- Controller → DbContext
- Agent → Repository
- Agent → DbContext
- Tool → Repository
- Tool → DbContext

---

# 10. Communication Rules

Agent chỉ giao tiếp với:
- ConversationService
- PromptBuilder
- AgentOrchestrator
- Tool

Tool chỉ giao tiếp với:
- Business Service
- Tool Dispatcher

Business Service chỉ giao tiếp với:
- Repository

Repository chỉ giao tiếp với:
- DbContext

Không được bỏ qua bất kỳ tầng nào.

---

# 11. Dependency Injection

Các component phải đăng ký trong DI Container.

Ví dụ:

- IConversationService
- IPromptBuilder
- IAgentOrchestrator
- IToolDispatcher
- IShoppingAgent
- IInventoryAgent
- IPromotionAgent
- IRecipeAgent
- IBudgetAgent
- ICartAgent

Không được khởi tạo trực tiếp bằng `new` trong Business Flow.

---

# 12. Error Handling

Nếu Tool lỗi:
- Không retry vô hạn
- Trả lỗi thân thiện
- Ghi log

Nếu Database lỗi:
- Không được đoán dữ liệu

Ví dụ sai:
“Có khoảng 20 sản phẩm.”

Đúng:
“Hiện tại tôi chưa thể truy cập dữ liệu sản phẩm.”

---

# 13. Logging

AI phải ghi nhận:
- Conversation Id
- User Id (nếu có)
- Agent được gọi
- Tool được gọi
- Thời gian xử lý
- Lỗi phát sinh

Không log:
- Password
- JWT
- API Key
- Connection String

---

# 14. Security

AI không được gửi cho LLM:
- Password
- API Key
- Connection String
- JWT Token
- Entity Framework Entity
- Internal Exception

LLM chỉ nhận DTO đã được chuẩn hóa.

---

# 15. Performance

Mục tiêu hiệu năng:

- Average Response Time ≤ 3 giây
- Tool Calling ≤ 500ms mỗi Tool
- Database Query ≤ 200ms

Không gọi Tool trùng lặp.

Không Query N+1.

Ưu tiên tái sử dụng dữ liệu trong cùng một Request.

---

# 16. Scalability

Kiến trúc phải hỗ trợ mở rộng.

Trong tương lai có thể bổ sung:
- NutritionAgent
- RecommendationAgent
- VoiceAgent
- OCRAgent
- ForecastAgent
- ImageSearchAgent

Không cần sửa ShoppingAgent để hỗ trợ Agent mới.

---

# 17. Future Architecture

Trong các phiên bản tiếp theo sẽ bổ sung:
- Conversation Memory
- Redis Cache
- Vector Database
- RAG
- Semantic Search
- Recommendation Engine
- Nutrition Knowledge Base
- Voice Assistant
- Multi-language Support
- AI Analytics Dashboard
