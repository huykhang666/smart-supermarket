# EXAMPLES: BÁO CÁO TỒN KHO & CẢNH BÁO RỦI RO CHO QUẢN LÝ (MANAGER INVENTORY)

---

## TÌNH HUỐNG 1: QUẢN LÝ YÊU CẦU BÁO CÁO MẶT HÀNG CẬN DATE HÔM NAY

### Quản lý nhập lệnh
`"Báo cáo các mặt hàng tươi sống còn dưới 3 ngày hạn sử dụng tại quầy tầng 1"`

### Luồng xử lý AI Retail Risk Agent
1. Gọi API nội bộ `GET /api/inventory/expiry-risk?daysThreshold=3&categoryId=9,4`.
2. Tính toán tổng giá trị tồn vốn, khối lượng và tỷ lệ hư hỏng dự kiến nếu không can thiệp.
3. Đề xuất kịch bản: Giảm giá giờ vàng (Flash Sale 30%) hoặc xuất bếp chế biến thức ăn sẵn.

### AI phản hồi
> **Kính gửi Quản lý ca trực,**
> 
> Hệ thống vừa quét dữ liệu tồn kho thời gian thực (Cập nhật lúc 09:30). Dưới đây là danh sách các mặt hàng tươi sống có **rủi ro cận date ($\le$ 3 ngày)**:
> 
> | Mã SP | Tên Sản Phẩm | Danh Mục | SL Tồn | ĐVT | HSD Còn | Giá Vốn Tồn | Đề Xuất Xử Lý |
> |---|---|---|---|---|---|---|---|
> | `TH-012` | Nạc Heo C.P Thùng | Thịt Tươi | 14 | kg | 1.5 ngày | 1,260,000 đ | Flash Sale 30% sau 16h00 |
> | `CA-005` | Cá Điêu Hồng Sống | Thủy Hải Sản | 8 | kg | 1 ngày | 520,000 đ | Chuyển quầy chế biến cá chiên |
> | `VG-044` | Rau Xà Lách Lô Lô | Rau Lá | 22 | túi | 2 ngày | 286,000 đ | Tặng kèm hóa đơn > 200k |
> 
> 📊 **Tổng kết tài chính rủi ro:**
> - Tổng giá vốn đối mặt nguy cơ hao hụt: **2,066,000 đ**.
> - Nếu áp dụng Flash Sale giảm giá trung bình 25%: Thu hồi tối thiểu **1,550,000 đ** vốn lưu động.
> 
> ⚙️ Quản lý có muốn duyệt lệnh kích hoạt chiến dịch **Flash Sale Cận Hạn 16h00** tự động trên ứng dụng khách hàng không?

---

## TÌNH HUỐNG 2: DỰ BÁO THIẾU HÀNG & DỰ THẢO ĐƠN MUA HÀNG (PURCHASE ORDER DRAFT)

### Quản lý nhập lệnh
`"Kiểm tra mặt hàng gia vị sắp hết để tạo phiếu nhập hàng"`

### Phản hồi của AI
> Dựa trên tốc độ bán trung bình 7 ngày qua (Run-rate) và lượng tồn thực tế trong kho:
> 
> ⚠️ **3 mặt hàng đã chạm ngưỡng đặt hàng lại (Safety Stock Reorder Point):**
> 1. **Nước mắm Nam Ngư Đệ Nhị 900ml**: Còn 12 chai (Bán TB: 25 chai/ngày) $\rightarrow$ *Thiếu hụt sau 11 giờ*.
> 2. **Dầu ăn Simply 1L**: Còn 8 chai (Bán TB: 15 chai/ngày) $\rightarrow$ *Thiếu hụt sau 13 giờ*.
> 3. **Hạt nêm Knorr Nấm 400g**: Còn 5 gói (Bán TB: 10 gói/ngày) $\rightarrow$ *Thiếu hụt sau 8 giờ*.
> 
> 📋 **Dự thảo đơn đặt hàng (PO #20261027-01):**
> ```json
> {
>   "supplierId": "SUPP-MASAN-01",
>   "suggestedDeliveryDate": "2026-10-28T07:00:00Z",
>   "items": [
>     { "sku": "NM-NAMNGU-900", "quantity": 120, "unit": "chai", "estimatedCost": 2880000 },
>     { "sku": "DA-SIMPLY-1L", "quantity": 96, "unit": "chai", "estimatedCost": 4992000 },
>     { "sku": "HN-KNORR-400", "quantity": 60, "unit": "gói", "estimatedCost": 2040000 }
>   ],
>   "totalEstimatedAmount": 9912000
> }
> ```
> Quản lý có thể bấm **"Xuất phiếu nhập sang Nhà cung cấp Masan"** để chuyển lệnh trực tiếp.
