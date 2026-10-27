# EXAMPLES: CÁC ĐỐI THOẠI ĐI CHỢ & MUA SẮM THỰC TẾ

---

## 1. HAPPY PATH: TƯ VẤN BỮA ĂN 30K CHO 1 NGƯỜI

**Khách hàng**: `"mình có 30k 1 người ăn thì nấu món gì"`

**Phân tích của ShoppingAgent**:
- Intent: `MealPlanning`
- Parameters: `Servings = 1`, `TargetBudget = 30000`
- Tool gọi: `RecipeTool.GetRecipeAsync("bình dân")`, `InventoryTool.GetStockAsync()`, `BudgetAgent.BalanceBudget()`

**AI phản hồi**:
> Dạ, theo yêu cầu của bạn (**1 người ăn**, ngân sách dự kiến **~30,000 đ**), mình đã cân đối và thiết kế **3 mâm cơm chuẩn vị theo cẩm nang Cookpad**:
> 
> 🍲 **Option 1: Cơm Trứng Chiên & Canh Rau Muống Luộc** (~**30,800 đ**)  
> *Trứng gà Ba Huân chiên vàng xốp, ăn kèm canh rau muống xanh giòn dầm chanh thanh mát.*
> 
> ✨ **Option 2: Mì Omachi Xúc Xích & Cải Ngọt Siêu Tốc** (~**44,300 đ**)  
> *Mì Omachi ngũ quả kèm xúc xích Vissan và rau cải ngọt VietGAP giòn ngọt, chế biến nhanh trong 5 phút.*
> 
> 🌿 **Option 3: Cá Hộp Ba Cô Gái & Dưa Leo Baby** (~**28,900 đ**)  
> *Cá nục sốt cà chua Ba Cô Gái sốt sánh mịn, ăn kèm dưa leo baby giòn mát và cơm nóng siêu đưa cơm.*
> 
> 👉 Bạn có thể xem chi tiết nguyên liệu, giá tiền từng món và tùy ý thêm/bớt ở các thẻ mâm cơm bên dưới, sau đó bấm **"Chọn mâm này"** để nạp nhanh vào giỏ hàng nhé!

---

## 2. MULTI-TURN CONVERSATION: HỘI THOẠI NHIỀU LƯỢT GIỮ NGỮ CẢNH (CONTEXT PERSISTENCE)

### Lượt 1: Khách yêu cầu ban đầu
**Khách hàng**: `"Tôi có 200.000đ, muốn nấu lẩu bò cho 3 người."`

**AI phản hồi**:
> Dạ, với ngân sách **200,000 đ** cho **3 người**, mình gợi ý mâm lẩu bò ấm cúng như sau:
> 
> 🍲 **Nồi Lẩu Bò Úc Nấm Tươi** (~**198,000 đ**)
> - Thịt Bò Ba Chỉ Úc Pacow (300g): 105,000 đ
> - Gói Nấm Kim Châm Enoki (200g): 18,000 đ
> - Nấm Đùi Gà Baby (200g): 22,000 đ
> - Rau Cải Cúc & Rau Muống: 25,000 đ
> - Gói Gia Vị Lẩu Bò Aji-Quick: 14,000 đ
> - Mì Trứng Vifon (1 lốc): 14,000 đ
> 
> Bạn có thể bấm **"Chọn mâm này"** để xem và chỉnh sửa nguyên liệu bên dưới nhé!

### Lượt 2: Khách yêu cầu đổi thịt sang loại rẻ hơn
**Khách hàng**: `"Đổi thịt bò sang loại rẻ hơn để tiết kiệm thêm."`

**Phân tích của ShoppingAgent**:
- Kế thừa Context: `Dish = Lẩu bò`, `Servings = 3`
- Kích hoạt `BudgetAgent` $\rightarrow$ gọi `ProductTool.GetAlternativeAsync(currentId=105, maxPrice=80000)`
- Tìm thấy: `Thịt Nạm Bò Ta Tươi (300g) - 75,000 đ` (Tiết kiệm 30,000 đ)

**AI phản hồi**:
> Dạ được chứ! Mình đã đổi sang **Thịt Nạm Bò Ta Tươi (300g)** với giá **75,000 đ** (tiết kiệm được 30,000 đ mà thịt nấu lẩu vẫn rất mềm ngọt).
> 
> 💰 **Tổng chi phí nồi lẩu sau điều chỉnh**: **168,000 đ** (Dư ra **32,000 đ** so với ngân sách 200k của bạn).  
> Mình đã cập nhật ngay vào thẻ mâm cơm bên dưới rồi nhé!

### Lượt 3: Khách bỏ nguyên liệu nấm
**Khách hàng**: `"Bỏ nấm đùi gà đi nha."`

**Phân tích của ShoppingAgent**:
- Bỏ item `Nấm Đùi Gà Baby (-22,000 đ)`
- Tính lại tổng: $168,000 - 22,000 = 146,000\text{ đ}$.

**AI phản hồi**:
> Dạ vâng, mình đã bỏ **Nấm Đùi Gà Baby** ra khỏi danh sách lẩu cho bạn.  
> 
> 🍲 **Tổng tiền nồi lẩu hiện tại**: **146,000 đ**.  
> Bạn bấm nút **"Chọn mâm này"** để mình nạp thẳng danh sách đã chọn vào giỏ hàng ngay nhé!

---

## 3. INVALID INPUT: NGƯỜI DÙNG NHẬP THIẾU HOẶC SAI THÔNG TIN

### Tình huống 3.1: Nhập chuỗi vô nghĩa
**Khách hàng**: `"asdasd123456???"`

**AI phản hồi**:
> Dạ chào bạn! Mình là Trợ lý Mua sắm Thông minh của Smart SuperMarket. Mình chưa hiểu rõ nhu cầu của bạn.  
> 
> Bạn có thể cho mình biết:  
> - Bữa ăn hôm nay cho mấy người ăn?  
> - Ngân sách dự kiến khoảng bao nhiêu? (Ví dụ: 30k, 100k, 300k...)  
> - Hoặc bạn đang muốn tìm nguyên liệu cho món ăn cụ thể nào không?

### Tình huống 3.2: Nhập số tiền âm
**Khách hàng**: `"Tôi có -50000 đ muốn đi chợ"`

**AI phản hồi**:
> Dạ, ngân sách mua sắm cần là một số tiền hợp lệ bạn nhé! Bạn có thể chia sẻ lại số tiền dự kiến hoặc món bạn đang muốn nấu để mình cân đối nguyên liệu tươi ngon nhất từ siêu thị cho bạn nhé.

---

## 4. BUSINESS RULE: ÁP DỤNG FLASH SALE CẬN DATE & VOUCHER

**Khách hàng**: `"Hôm nay có gì xả hàng giảm giá để nấu cơm tối không?"`

**Phân tích của PromotionAgent & InventoryAgent**:
- Quét lô hàng $\le 3$ ngày: Thịt nạc heo C.P còn 2 ngày (Flash sale 30%), Cải ngọt VietGAP (Flash sale 30%).

**AI phản hồi**:
> Chào bạn! Siêu thị hôm nay đang có chương trình **Flash Sale Giờ Vàng Cận Date** với mức chiết khấu cực kỳ tiết kiệm cho bữa cơm tối:
> 
> 🥩 **Thịt Nạc Dăm Heo C.P (500g)**: Giảm 30% từ ~~72,000 đ~~ còn **50,400 đ** (HSD: 29/10, bao bì mát chuẩn tiệt trùng).  
> 🥬 **Cải Ngọt VietGAP (500g)**: Giảm 30% từ ~~18,000 đ~~ còn **12,600 đ**.  
> 
> 💡 *Gợi ý mâm cơm tối*: Nạc heo xào cải ngọt giòn thơm kèm canh cà chua trứng chỉ tốn **~75,000 đ** cho cả nhà 3 người! Bạn bấm **"Chọn mâm này"** để giữ suất ưu đãi nhé!
