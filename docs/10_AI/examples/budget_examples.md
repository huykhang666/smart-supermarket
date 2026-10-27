# EXAMPLES: CÂN ĐỐI NGÂN SÁCH & ĐỀ XUẤT THAY THẾ (BUDGET BALANCING EXAMPLES)

---

## 1. TÌNH HUỐNG: TỔNG CHI VƯỢT NGÂN SÁCH (OVER BUDGET REPLACEMENT)

### Ngữ cảnh
- Khách hàng yêu cầu: `"Tôi có ngân sách 300.000 đ, muốn nấu bữa cơm tối đầy đặn cho 4 người với thịt bò và canh chua."`
- Khảo sát ban đầu nếu chọn nguyên liệu cao cấp:
  - Thịt Thăn Bò Úc Pacow (500g): 185,000 đ
  - Cá Hú Cắt Khúc Nấu Canh Chua (400g): 52,000 đ
  - Rau Muống, Cà Chua, Bạc Hà, Đậu Bắp: 48,000 đ
  - Gói Gia Vị Canh Chua + Hành Tỏi: 20,000 đ
  - Trái Cây Tráng Miệng Nho Ninh Thuận: 45,000 đ
  - **Tổng ban đầu**: **350,000 đ** $\rightarrow$ *Vượt 50,000 đ so với ngân sách 300,000 đ.*

### Xử lý của BudgetAgent
1. So sánh: $\text{CurrentTotal} (350,000) > \text{Budget} (300,000)$.
2. Kích hoạt `ProductTool.GetAlternativeAsync()` để tìm món có độ co giãn giá lớn nhất (Thịt thăn bò Úc).
3. Tìm thấy món thay thế tương đương: `Thịt Bò Phi-lê Việt Nam Tươi (500g) - 135,000 đ` (Tiết kiệm 50,000 đ).
4. Tính lại: $350,000 - 50,000 = 300,000\text{ đ}$ (Khớp chuẩn 100% ngân sách).

### AI phản hồi
> Dạ, để mâm cơm gia đình 4 người vừa có thịt bò xào thơm mềm, vừa có canh chua thanh mát mà **không vượt quá 300,000 đ**, mình đã chủ động cân đối lại nguyên liệu:
> 
> 🥩 Thay vì chọn Bò Úc Pacow nhập khẩu (185k), mình đổi sang **Thịt Bò Phi-lê Việt Nam Tươi (500g)** với giá **135,000 đ** (tiết kiệm được đúng 50,000 đ mà thịt xào vẫn rất mềm ngọt chuẩn vị).
> 
> 🍲 **Mâm cơm trọn gói sau cân đối**:
> 1. Thịt bò phi-lê xào cần tỏi: 135,000 đ
> 2. Canh chua cá hú bạc hà cà chua: 92,000 đ
> 3. Tráng miệng trái cây tươi: 45,000 đ
> 4. Gia vị & rau thơm: 20,000 đ
> 
> 💰 **Tổng chi phí**: **292,000 đ** (Vừa vặn trong ngân sách 300,000 đ của bạn).  
> Bạn bấm **"Chọn mâm này"** để kiểm tra danh sách nguyên liệu nhé!

---

## 2. PHÂN BỔ BỮA ĂN SINH VIÊN 30K - 50K (ULTRA LOW BUDGET)

**Khách hàng**: `"Sinh viên cuối tháng còn 35k ăn gì cho đủ chất?"`

**Xử lý của BudgetAgent**:
- Khóa chặt các nhóm hàng Category 9 đắt tiền.
- Ưu tiên nhóm đạm giá trị sinh học cao nhưng rẻ: Trứng gà, đậu hũ, cá hộp.

**AI phản hồi**:
> Dạ sinh viên cuối tháng đừng lo nhé, với **35,000 đ** mình gợi ý bạn mâm cơm nóng sốt và no bụng:
> 
> 🍳 **Cơm Đậu Hũ Nhồi Thịt Sốt Cà & Canh Cải**:
> - 2 miếng Đậu hũ trắng tươi: 6,000 đ
> - 100g Thịt nạc xay MeatDeli: 14,000 đ
> - 2 quả Cà chua Đà Lạt: 5,000 đ
> - 1 bó Cải xanh nấu canh: 8,000 đ
> 
> 💰 **Tổng chi phí**: **33,000 đ** (Vẫn còn dư 2,000 đ mua hành lá).  
> Vừa đủ đạm từ thịt đậu, vừa có chất xơ từ rau cải tươi xanh bạn nhé!

---

## 3. TIỆC LẨU GIA ĐÌNH 500K CHO 6 NGƯỜI (PREMIUM BUDGET)

**Khách hàng**: `"Cuối tuần có 500k làm tiệc lẩu gì cho 6 người ăn thoải mái?"`

**AI phản hồi**:
> Dạ tuyệt vời quá! Với ngân sách **500,000 đ** cho **6 người**, mình thiết kế nồi **Lẩu Thái Hải Sản Bò Nấm** cực kỳ đầy đặn:
> 
> 🥘 **Nguyên liệu chính**:
> - Ba chỉ bò Mỹ cuộn lẩu (500g): 135,000 đ
> - Tôm thẻ tươi sống (300g): 65,000 đ
> - Mực ống tươi làm sạch (300g): 85,000 đ
> - Nghêu Bến Tre sống (500g): 25,000 đ
> - Nấm kim châm, nấm đùi gà, ngô ngọt: 45,000 đ
> - Rau muống, cải thảo, hoa chuối: 40,000 đ
> - Gói gia vị lẩu Thái Aji-Quick (2 gói) + sả ớt cốt dừa: 28,000 đ
> - Bún tươi sạch Ba Khánh (1kg): 20,000 đ
> 
> 💰 **Tổng thanh toán**: **443,000 đ** (Tiết kiệm được 57,000 đ so với dự tính 500k để bạn mua thêm nước ngọt hoặc bia lạnh).  
> Bạn bấm **"Chọn mâm này"** để nạp thẳng combo tiệc lẩu vào giỏ hàng nhé!
