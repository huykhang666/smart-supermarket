# User-Facing AI Tests
# Smart SuperMarket AI — Kiểm thử Tính năng Khách hàng

---

## 1. Mục tiêu & Nguyên tắc
Xác thực các kịch bản thực tế của người dùng cuối khi tương tác với Trợ lý AI Đi Chợ & Mua Sắm:
- Lên kế hoạch mâm cơm theo số người và ngân sách.
- Cập nhật, thay thế nguyên liệu, điều chỉnh ngân sách đa lượt (multi-turn).
- Kiểm tra khuyến mãi và tình trạng tồn kho thực tế.
- Tư vấn danh mục hàng bán lẻ siêu thị (Retail Shopping Intelligence) theo 4 phân khúc giá trị.
- Hỏi lại để làm rõ khi thông tin người dùng chưa đủ dữ kiện.

---

## 2. Kịch bản Kiểm thử Chi tiết

### 2.1 Lên Thực đơn & Tối ưu Ngân sách (Meal / Shopping Planning)
- **Chuỗi hội thoại mẫu**:
  1. `User`: "Mình có 300k, nấu lẩu bò cho 4 người."
     - *Kỳ vọng*: Nhận diện đúng món (Lẩu bò), số lượng (4 người), ngân sách (300,000 đ). Các phương án trong ngân sách xếp đầu tiên.
  2. `User`: "Đổi bò Úc thành bò ta."
     - *Kỳ vọng*: Giữ nguyên mâm lẩu bò, thay thế nguyên liệu bò Úc thành bò ta, tính toán lại tổng tiền.
  3. `User`: "Bỏ nấm."
     - *Kỳ vọng*: Loại bỏ nguyên liệu nấm khỏi mâm, trừ tiền nấm, cập nhật mô tả món ăn sạch sẽ.
  4. `User`: "Giảm còn 250k."
     - *Kỳ vọng*: Trần ngân sách mới là 250,000 đ; tính lại chênh lệch ngân sách, phương án nào vượt 250k được gắn nhãn `Tham khảo - Vượt ngân sách`.
  5. `User`: "Thêm tất cả vào giỏ."
     - *Kỳ vọng*: Nhận diện intent `AddToCart`, nạp toàn bộ nguyên liệu vào giỏ hàng phiên làm việc.

---

### 2.2 Khuyến mãi & Tồn kho Thực tế (Promotion & Inventory Truth)
- **Kịch bản**:
  - `User`: "Có flash sale nào không?"
    - *Kỳ vọng*: Truy vấn danh mục khuyến mãi thực tế từ CSDL siêu thị; không bịa mã voucher không tồn tại.
  - `User`: "Món này còn hàng không?"
    - *Kỳ vọng*: Truy vấn tổng tồn từ bảng `InventoryBatches`; báo chính xác số lượng tồn trên kệ.

---

### 2.3 Tư vấn Ngành hàng Bán lẻ (Retail Shopping Intelligence)
- **Kịch bản**:
  - `User`: "Mua dầu gội nào rẻ mà ổn?"
    - *Kỳ vọng*: Phát hiện ngành hàng `Chăm sóc cá nhân`, trả về sản phẩm Bậc 1 (Tiết kiệm chấp nhận được) và Bậc 2 (Cân bằng giá trị).
  - `User`: "Nồi cơm nào dùng lâu, giá hợp lý?"
    - *Kỳ vọng*: Ngành hàng `Gia dụng nhà bếp`, đề xuất Bậc 2 và Bậc 3 (Bền bỉ hiệu năng cao).
  - `User`: "Mua đồ cho em bé thì nên chọn phân khúc nào?"
    - *Kỳ vọng*: Khuyến nghị ưu tiên tiêu chuẩn an toàn (Bậc 3 & 4), giải thích rõ trade-off giá thành vs sự an tâm.

---

### 2.4 Hỏi lại Làm rõ (Clarification / Fallback)
- **Kịch bản**:
  - `User`: "Làm món gì ngon?" hoặc "Mua cái nào tốt?"
  - *Kỳ vọng*: Không bịa ngân sách hay tự đoán category bừa bãi. Phản hồi lịch sự đề nghị khách hàng chia sẻ thêm về số lượng người ăn, ngân sách dự kiến hoặc ngành hàng quan tâm.

---

### 2.5 Retail-First Intent Routing (Prompt Fix)
- **Kịch bản**:
  1. `User`: "có nồi cơm 500k không"
     - *Kỳ vọng*: Hệ thống nhận diện đây là câu hỏi mua sắm thiết bị gia dụng bán lẻ, ngân sách 500k là thuộc tính lọc sản phẩm, KHÔNG được chuyển thành Meal Planning (không sinh mâm cơm hay công thức nấu ăn). Đề xuất Nồi cơm Comet 0.8L (250k, trong ngân sách) và Nồi cơm Sharp 1.8L (690k, vượt ngân sách kèm giải thích).
  2. `User`: "mua chảo 300k"
     - *Kỳ vọng*: Điều hướng sang `RetailShopping` (nhóm Đồ dùng gia đình & Nhà bếp), đề xuất Chảo Sunhouse 24cm (165k) và Chảo Elmich đáy từ (245k), cả hai đều nằm trong ngân sách 300k.
  3. `User`: "giấy vệ sinh loại nào đáng tiền"
     - *Kỳ vọng*: Phân loại vào `ValueForMoneyShopping` thuộc ngành hàng Giấy vệ sinh & Khăn giấy, phân tích trade-off giữa Pulppy 3 lớp và An An không lõi.
  4. `User`: "Mình có 300k, nấu lẩu bò cho 4 người."
     - *Kỳ vọng*: Có từ khóa ngữ cảnh nấu ăn rõ ràng ("nấu", "lẩu", "4 người") nên bảo toàn điều hướng sang `MealPlanning`.

---

## 3. Kết quả Xác thực
- Mã kiểm thử tự động: [`AiTestChecklistTests.cs`](file:///D:/Laptrinhtrucquan/Backend.Tests/AI/AiTestChecklistTests.cs), [`RetailFirstRoutingTests.cs`](file:///D:/Laptrinhtrucquan/Backend.Tests/AI/RetailFirstRoutingTests.cs), [`RetailShoppingScenarioTests.cs`](file:///D:/Laptrinhtrucquan/Backend.Tests/AI/RetailShoppingScenarioTests.cs)
- Trạng thái: **PASS 100% (300/300 unit & integration tests passing).**
