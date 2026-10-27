# ADR-0002: ÁP DỤNG BASEAGENT PATTERN & BOUNDED CONTEXT

## Trạng Thái (Status)
Đã chấp thuận (Accepted)

## Bối Cảnh (Context)
Nếu để một Agent duy nhất phụ trách toàn bộ tác vụ (tìm kiếm món ăn, tính tiền, kiểm tra tồn kho, áp dụng voucher), Prompt sẽ bị phình to (Context Bloat), dẫn đến hiện tượng ảo giác (Hallucination), tính toán sai ngân sách và không kiểm soát được ranh giới trách nhiệm.

## Quyết Định (Decision)
1. Áp dụng mô hình **BaseAgent Pattern**: Mọi Agent đều kế thừa từ `BaseAgent` để chia sẻ chung cơ chế nạp Prompt, Logging, bẫy lỗi và điều phối qua `IToolDispatcher`.
2. Phân chia **Bounded Context** nghiêm ngặt:
   - `ShoppingAgent`: Chỉ lập kế hoạch và tổng hợp kết quả.
   - `InventoryAgent`: Chỉ kiểm tra tồn kho, số lô, hạn dùng (Cấm tính tiền, cấm gợi ý món).
   - `PromotionAgent`: Chỉ xử lý voucher, Flash Sale (Cấm tự tạo mã giảm giá).
   - `BudgetAgent`: Chỉ cân đối ngân sách và đề xuất thay thế nguyên liệu rẻ hơn.
   - `RecipeAgent`: Chỉ tra cứu cẩm nang Cookpad (Cấm sinh món ăn ảo).
   - `CartAgent`: Chỉ thao tác giỏ hàng.

## Lý Do (Reason)
- Prompt của từng Agent ngắn gọn, tập trung cao độ vào một miền nghiệp vụ duy nhất.
- Hạn chế tối đa ảo giác và triệt tiêu xung đột dữ liệu.
- Giúp hệ thống dễ mở rộng khi bổ sung thêm tác nhân mới (ví dụ: `NutritionAgent`, `CustomerServiceAgent`).

## Hệ Quả (Consequences)
- Cần có `AgentOrchestrator` để đóng vai trò nhạc trưởng điều phối thứ tự kích hoạt giữa các Agent.
