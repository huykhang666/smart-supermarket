# KNOWLEDGE BASE: QUY TẮC CHIẾT KHẤU & FLASH SALE (DISCOUNT RULES)

> **Mục đích**: Làm nguồn chân lý bất biến (Ground Truth Knowledge Base) cho `PromotionAgent`, `InventoryAgent` và `BudgetAgent`. AI bắt buộc phải tra cứu và tuân thủ các quy tắc này, tuyệt đối không được tự ý suy đoán mức giảm giá.

---

## 1. QUY TẮC GIẢI CỨU HÀNG TƯƠI SỐNG CẬN DATE (SHELF-LIFE RESCUE)

Áp dụng cho các mặt hàng thuộc nhóm thực phẩm tươi sống có hạn sử dụng ngắn: Thịt tươi (Category 9), Cá tươi (Category 9), Rau lá & Nấm tươi (Category 4).

| Hạn Sử Dụng Còn Lại (Shelf-Life) | Mức Giảm Giá (Discount) | Khung Giờ Kích Hoạt | Mục Tiêu Bán Hàng |
|---|---|---|---|
| **$\le$ 3 ngày** | **Giảm 30%** so với giá niêm yết | Sau 16:00 hàng ngày | Kích cầu bữa ăn tối gia đình, giải phóng tồn kho |
| **$\le$ 1 ngày** | **Giảm 50%** so với giá niêm yết | Cả ngày (Flash Sale xả kho) | Thanh lý dứt điểm lô hàng trước 21:00, tránh lãng phí hủy hàng |
| **> 3 ngày** | **0%** (Giá niêm yết chuẩn) | Giờ mở cửa bình thường | Đảm bảo biên lợi nhuận tiêu chuẩn siêu thị |

### Công thức tính giá Flash Sale
$$\text{FinalPrice} = \begin{cases} 
\text{UnitPrice} \times 0.50, & \text{nếu } \text{RemainingDays} \le 1 \\
\text{UnitPrice} \times 0.70, & \text{nếu } 1 < \text{RemainingDays} \le 3 \\
\text{UnitPrice}, & \text{nếu } \text{RemainingDays} > 3
\end{cases}$$

---

## 2. QUY TẮC VOUCHER NGÀNH HÀNG (CATEGORY VOUCHERS)

- **Mã `FRESH20`**:
  - Giảm ngay **20,000 đ** trực tiếp trên hóa đơn.
  - Điều kiện: Tổng giá trị các mặt hàng thuộc Category 9 (Thịt, Trứng, Hải sản tươi) trong đơn hàng phải đạt tối thiểu **100,000 đ**.
  - Giới hạn: 01 lần/khách hàng/ngày.

- **Mã `VEGGIE10`**:
  - Giảm ngay **10,000 đ** trực tiếp trên hóa đơn.
  - Điều kiện: Mua từ 3 món rau củ quả VietGAP (Category 4).

---

## 3. QUY TẮC BÁN KÈM COMBO (CROSS-SELL BUNDLE DISCOUNTS)
- **Combo Gà Luộc / Gà Lá Giang**: Khi khách mua 01 con gà ta làm sạch $\ge 1.2\text{kg}$, giảm giá **40%** cho gói gia vị lẩu gà lá giang hoặc giảm **25%** combo hành tím - sả tươi.
- **Combo Thịt Kho Tàu**: Mua 500g thịt ba chỉ MeatDeli $\rightarrow$ Tặng voucher giảm 5,000 đ khi mua kèm vỉ 10 quả trứng gà Ba Huân.
