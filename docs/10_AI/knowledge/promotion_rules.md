# KNOWLEDGE BASE: QUY TẮC KHUYẾN MÃI TỔNG THỂ (PROMOTION RULES)

---

## 1. NGUYÊN TẮC ÁP DỤNG KHUYẾN MÃI (PROMOTION PRIORITY ORDER)

Khi tính toán giỏ hàng, `PromotionAgent` phải tuân thủ thứ tự ưu tiên áp dụng sau:
1. **Giá Flash Sale theo Lô Hàng Cận Date (FEFO Discount)**: Áp dụng trực tiếp vào đơn giá từng sản phẩm.
2. **Khuyến mãi Bán Kèm Combo (Bundle Discount)**: Áp dụng khi giỏ hàng thỏa mãn điều kiện mua kèm.
3. **Mã Giảm Giá Voucher (Order / Category Voucher)**: Áp dụng sau cùng trên tổng giá trị rổ hàng sau khi đã trừ các khoản chiết khấu trên.

---

## 2. QUY ĐỊNH CHỐNG CỘNG DỒN VOUCHER (STACKING RULES)
- Khách hàng **không được** áp dụng cùng lúc 2 voucher cùng loại (ví dụ: Không dùng đồng thời `FRESH20` và `FRESH30` trên cùng 1 đơn hàng).
- Cho phép áp dụng Voucher ngành hàng kết hợp cùng giá đã giảm Flash Sale cận date (nhằm kích thích tối đa việc giải cứu thực phẩm).
