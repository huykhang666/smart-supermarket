# 02_Agent_Design.md

# AI Agent Design

Version: 1.1

---

# 1. Core Principle

Mọi Agent đều kế thừa từ `BaseAgent`.

Mọi Agent chỉ làm nhiệm vụ lập luận và điều phối.

Agent không truy cập Repository trực tiếp.
Agent không truy cập DbContext trực tiếp.
Agent không chứa Business Logic.

---

# 2. Agent Hierarchy

- ShoppingAgent
- InventoryAgent
- PromotionAgent
- RecipeAgent
- BudgetAgent
- CartAgent

---

# 3. BaseAgent Responsibilities

`BaseAgent` chịu trách nhiệm:
- Logging
- OpenTelemetry tracing
- Memory access
- Prompt loading
- Tool dispatch
- Exception wrapping
- Context serialization/deserialization

---

# 4. ShoppingAgent

## Purpose
Điều phối toàn bộ luồng tư vấn mua sắm.

## Responsibilities
- Hiểu nhu cầu người dùng
- Phân tích intent
- Xác định thứ tự gọi Agent / Tool
- Tổng hợp câu trả lời cuối cùng

## Cannot
- Không query database
- Không tính chiết khấu thô
- Không truy cập DbContext
- Không sinh giá giả

---

# 5. InventoryAgent

## Purpose
Kiểm tra tồn kho, batch, cận date.

## Responsibilities
- Get stock
- Get near expiry
- Check batch
- Suggest substitution based on stock availability

## Cannot
- Không tính tiền
- Không tìm recipe
- Không tạo voucher
- Không xử lý cart

---

# 6. PromotionAgent

## Purpose
Tìm voucher, flash sale, khuyến mãi khả dụng.

## Responsibilities
- Check active promotions
- Check voucher priority
- Compute discount candidates

## Cannot
- Không query inventory
- Không tự tạo mã voucher
- Không quyết định vượt quyền business rules

---

# 7. RecipeAgent

## Purpose
Tra cứu recipe / ingredient plan.

## Responsibilities
- Tìm recipe phù hợp
- Bóc tách nguyên liệu
- Đề xuất công thức theo ngân sách / số người

## Cannot
- Không sinh công thức ảo giác
- Không thay thế dữ liệu thực bằng suy đoán

---

# 8. BudgetAgent

## Purpose
So sánh tổng tiền và ngân sách.

## Responsibilities
- Compute estimated total
- Compare with budget
- Suggest cheaper replacements
- Request recalculation when needed

## Cannot
- Không tự sửa giá sản phẩm
- Không tự sửa promotion
- Không tự ý thay đổi dữ liệu thực

---

# 9. CartAgent

## Purpose
Xử lý giỏ hàng.

## Responsibilities
- Add
- Remove
- Update
- Estimate
- Prepare checkout summary

## Cannot
- Không truy cập repository trực tiếp
- Không thanh toán
- Không sinh dữ liệu order giả

---

# 10. Agent Execution Pattern

1. Agent nhận context từ Orchestrator
2. Agent gọi ToolDispatcher
3. ToolDispatcher gọi Tool
4. Tool gọi Service
5. Service gọi Repository
6. Repository gọi DbContext
7. Kết quả quay ngược lại Agent
8. Agent tổng hợp phản hồi

---

# 11. BaseAgent Required Features

- CorrelationId
- CancellationToken
- Prompt loading
- Memory access
- Execution metrics
- Safe JSON deserialization
- SharedData support
