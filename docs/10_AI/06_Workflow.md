# 06_Workflow.md

# Conversation Update Workflow

Version: 1.1

---

## A. Luồng tổng quát

User message  
→ Read current conversation state  
→ Detect intent  
→ Classify update type  
→ Load relevant tools  
→ Recalculate plan  
→ Validate budget  
→ Render response

---

## B. Phân loại intent

### 1. CreatePlan
Người dùng muốn tạo mâm cơm / kế hoạch mới.

Ví dụ:
- “Mình có 300k, nấu lẩu bò cho 4 người.”

Xử lý:
- gọi RecipeTool
- gọi InventoryTool
- gọi PromotionTool
- gọi BudgetTool
- tạo plan mới
- chọn phương án tốt nhất trong ngân sách

---

### 2. ReplaceIngredient
Người dùng muốn thay nguyên liệu.

Ví dụ:
- “Đổi bò Úc thành bò ta.”

Xử lý:
- lấy plan hiện tại
- xác định item cần thay
- gọi ProductTool / InventoryTool nếu cần
- thay nguyên liệu
- tính lại total
- render plan mới

---

### 3. RemoveIngredient
Người dùng muốn bỏ nguyên liệu.

Ví dụ:
- “Bỏ nấm.”

Xử lý:
- lấy plan hiện tại
- xóa nguyên liệu khỏi list
- tính lại total
- nếu recipe bị thiếu thành phần quan trọng thì đề xuất thay thế

---

### 4. UpdateBudget
Người dùng thay đổi ngân sách.

Ví dụ:
- “Giảm còn 250k.”

Xử lý:
- lấy plan hiện tại
- cập nhật budget mới
- chạy lại budget optimization
- sắp xếp lại options
- ưu tiên option trong ngân sách

---

### 5. QueryPromotion
Người dùng hỏi khuyến mãi.

Ví dụ:
- “Có flash sale nào không?”

Xử lý:
- gọi PromotionTool
- lọc promotion đang active
- kiểm tra điều kiện áp dụng
- gắn kết quả vào plan hiện tại

---

### 6. QueryInventory
Người dùng hỏi hàng còn hay không.

Ví dụ:
- “Món này còn hàng không?”

Xử lý:
- gọi InventoryTool
- kiểm tra stock / batch / near-expiry
- trả về dữ liệu thật
- không đoán

---

### 7. AddToCart
Người dùng muốn thêm vào giỏ.

Ví dụ:
- “Thêm tất cả vào giỏ.”

Xử lý:
- lấy plan hiện tại
- gọi CartTool
- thêm toàn bộ item hợp lệ
- xác nhận lại kết quả

---

### 8. EstimateCart
Người dùng muốn tính lại giỏ.

Ví dụ:
- “Tính lại tổng tiền.”

Xử lý:
- gọi CartTool / BudgetTool
- tính tổng mới
- so sánh ngân sách
- render kết quả

---

## C. Workflow chi tiết cho hội thoại nhiều lượt

### Bước 1: Nhận message mới
Ví dụ:
- “Mình có 300k, nấu lẩu bò cho 4 người.”

### Bước 2: Tạo plan ban đầu
- RecipeTool tìm recipe phù hợp
- InventoryTool kiểm tra tồn kho
- PromotionTool kiểm tra khuyến mãi
- BudgetTool tính tổng

### Bước 3: Người dùng sửa plan
Ví dụ:
- “Đổi bò Úc thành bò ta.”

Xử lý:
- không tạo plan mới từ đầu
- chỉ cập nhật ingredient liên quan
- tính lại tổng

### Bước 4: Người dùng xoá thành phần
Ví dụ:
- “Bỏ nấm.”

Xử lý:
- xóa nấm khỏi plan hiện tại
- nếu thiếu cân bằng món thì đề xuất thay thế

### Bước 5: Người dùng đổi ngân sách
Ví dụ:
- “Giảm còn 250k.”

Xử lý:
- chạy lại budget optimization
- sắp xếp lại options
- option nào phù hợp ngân sách đưa lên đầu

### Bước 6: Người dùng hỏi flash sale
Ví dụ:
- “Có flash sale nào không?”

Xử lý:
- kiểm tra promotion
- trả kết quả thật
- không làm mất plan hiện tại

### Bước 7: Người dùng chốt giỏ
Ví dụ:
- “Thêm tất cả vào giỏ.”

Xử lý:
- thêm item hợp lệ
- trả tổng tiền cuối cùng
- giữ context cho checkout

---

## D. Output order rule

Mỗi response phải theo thứ tự:

1. Phương án khuyến nghị chính
2. Phương án thay thế
3. Phương án vượt ngân sách nếu có
4. Tổng tiền
5. Trạng thái ngân sách
6. Gợi ý hành động tiếp theo

---

## E. Error handling workflow

### Nếu thiếu context
- hỏi lại người dùng
- không tự đoán

### Nếu tool lỗi
- thông báo ngắn gọn
- không bịa dữ liệu

### Nếu không có phương án phù hợp ngân sách
- nói rõ không có phương án phù hợp
- đề xuất giảm chi phí hoặc thay nguyên liệu

---

## F. Validation checklist trước khi render

Trước khi trả response, phải kiểm tra:
- có đang dùng plan hiện tại không
- có cập nhật đúng yêu cầu người dùng không
- có bỏ nguyên liệu đúng chưa
- có tính lại total chưa
- có ưu tiên đúng ngân sách chưa
- có gắn nhãn `Vượt ngân sách` chưa
- có bịa dữ liệu không

---

## G. Required behavior summary

- Không reset plan nếu user chỉ sửa một phần
- Không giữ nguyên nguyên liệu cũ sau khi user yêu cầu thay thế
- Không trả option vượt ngân sách như option chính
- Không bỏ qua intent mới
- Không trả lời như một request độc lập nếu đang có hội thoại trước đó
