# EXAMPLES: XỬ LÝ CÁC TRƯỜNG HỢP NGOẠI LỆ & TÌNH HUỐNG BIÊN (EDGE CASES)

---

## 1. NGÂN SÁCH QUÁ THẤP (< 20,000 Đ HOẶC KHÔNG THỂ CÂN ĐỐI)

### Tình huống
Khách hàng nhập: `"Mình có đúng 10k thì ăn được gì cho no?"`

### Thách thức
Một suất ăn tối thiểu tại siêu thị gồm tinh bột và đạm/rau hiếm khi dưới 15,000 đ nếu mua lẻ theo gói nguyên.

### Cách AI ứng biến
- Tuyệt đối không bịa ra giá thấp hơn giá niêm yết trong Database.
- Đề xuất các món ăn cứu cánh sinh viên tiện lợi (mì ăn liền, xúc xích ăn liền hoặc bánh mì ngọt).
- Nhắc nhở lịch sự và chân thành:

> "Dạ với ngân sách **10,000 đ**, mình gợi ý bạn 2 lựa chọn no bụng và tiện lợi nhất tại siêu thị:  
> 1. **1 gói Mì Hảo Hảo Tôm Chua Cay (4,500 đ)** + **1 quả Trứng Gà So Ba Huân (3,800 đ)** $\rightarrow$ Tổng: **8,300 đ**.  
> 2. **1 Bánh Mì Hoa Cúc Phô Mai Mini (9,500 đ)** $\rightarrow$ Tổng: **9,500 đ**.  
> Cả 2 món đều sẵn sàng phục vụ bạn ngay tại quầy tiện lợi nhé!"

---

## 2. NGUYÊN LIỆU HẾT HÀNG (OUT OF STOCK) HOẶC THIẾU HÀNG ĐỘT XUẤT

### Tình huống
Khách yêu cầu mâm cơm có `"Thịt bò Úc xào cần tây"`, nhưng hệ thống kiểm tra thấy `Thịt bò Úc` (Product ID #104) có `StockQuantity = 0`.

### Cách AI ứng biến
- Tự động thay thế bằng sản phẩm tương đương cùng nhóm dinh dưỡng (Category 9 - Thịt Đỏ):
  `Thịt nạc thăn heo C.P` hoặc `Thịt phi lê gà tươi`.
- Thông báo rõ ràng lý do thay thế để khách chủ động quyết định:

> "Dạ hiện tại mặt hàng **Thịt Bò Úc** tại chi nhánh vừa tạm hết hàng. Để mâm cơm của bạn vẫn đầy đủ dưỡng chất và thơm ngon, mình xin phép thay thế bằng **Thịt Bò Phi-lê Việt Nam Tươi** (hoặc **Nạc Dăm Heo Tươi C.P**) xào cùng cần tây giòn ngọt. Bạn xem mâm cơm này có vừa ý không nhé!"

---

## 3. TÊN MÓN ĂN LẠ HOẶC TỪ ĐỊA PHƯƠNG KHÔNG CÓ TRONG COOKPAD / DATABASE

### Tình huống
Khách hàng nhập: `"Chỉ mình nấu món canh thụt hoặc lẩu thắng cố"`

### Cách AI ứng biến
- AI nhận diện đây là món ăn đặc sản vùng miền có nguyên liệu chuyên biệt không thuộc danh mục siêu thị bán lẻ thông thường.
- Phân tích thành phần tương đương hoặc khéo léo gợi ý món lẩu/canh đặc trưng có sẵn nguyên liệu tại siêu thị:

> "Dạ món đặc sản bạn vừa nhắc tới đòi hỏi một số nguyên liệu rừng/địa phương đặc thù mà siêu thị hiện chưa phân phối. Tuy nhiên, nếu bạn muốn thưởng thức một món lẩu đậm đà tương tự vào tối nay, mình xin gợi ý **Lẩu Bò Sa Tế Chua Cay** hoặc **Canh Thịt Bò Hầm Rau Củ Thảo Mộc** với đầy đủ thịt bò tươi và nấm tươi có sẵn tại quầy nhé!"

---

## 4. GEMINI API MẤT KẾT NỐI / TIMEOUT / HẾT QUOTA (FALLBACK CHAIN KÍCH HOẠT)

### Tình huống
Google Gemini trả về mã lỗi `HTTP 429 ResourceExhausted` hoặc `Timeout > 5s`.

### Cách hệ thống ứng biến
1. Lớp 1 (Gemini) báo lỗi $\rightarrow$ Kích hoạt ngay Lớp 2 (Local Pre-compiled Rule Engine).
2. Rule Engine quét thông số Request (`people: 2, budget: 100000`).
3. Trả về ngay mâm cơm chuẩn định sẵn từ bảng `LocalRecipeStore` đã được cache sẵn trong RAM (`Cookpad_Option_1_CơmTrứngCanhRau`).
4. Khách hàng nhận được kết quả sau 150ms mà không nhận thấy lỗi gián đoạn.
