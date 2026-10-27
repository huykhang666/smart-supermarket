# BUSINESS RULE: THỨ TỰ ƯU TIÊN ÁP DỤNG CHIẾT KHẤU & VOUCHER (VOUCHER PRIORITY)

> **Mục đích**: Quy định thứ tự trừ tiền trên giỏ hàng, tuyệt đối cấm AI tự ý suy luận hoặc cộng dồn chiết khấu sai quy tắc kế toán bán lẻ.

---

## 1. THỨ TỰ ƯU TIÊN 5 TẦNG (5-LAYER PRIORITY ORDER)

Khi tính toán tổng tiền thanh toán cho giỏ hàng, hệ thống bắt buộc thực thi tuần tự theo chuỗi:

```text
Giá Niêm Yết Gốc (Original Price)
        │
        ▼
[Tầng 1] Flash Sale / Hàng Cận Date (FEFO Discount)
        │
        ▼
[Tầng 2] Khuyến Mãi Combo Bán Kèm (Bundle Discount)
        │
        ▼
[Tầng 3] Mã Giảm Giá Ngành Hàng / Đơn Hàng (Voucher FRESH20)
        │
        ▼
[Tầng 4] Điểm Tích Lũy Khách Hàng Thân Thiết (Loyalty Points)
        │
        ▼
[Tầng 5] Chiết Khấu Hạng Thành Viên Siêu Thị (Member Discount)
        │
        ▼
Số Tiền Thanh Toán Thực Tế (Final Payable Amount)
```

---

## 2. NGUYÊN TẮC CHI TIẾT TỪNG TẦNG

1. **Tầng 1 - Flash Sale / Cận Date**:
   - Trừ trực tiếp vào đơn giá từng mặt hàng trước khi tính tổng rổ.
   - Hạn dùng $\le 1$ ngày: Giảm 50%.
   - Hạn dùng $\le 3$ ngày: Giảm 30%.
2. **Tầng 2 - Combo Mua Kèm**:
   - Áp dụng khi đơn có đủ cặp sản phẩm (Mua gà tặng giảm 40% gia vị).
3. **Tầng 3 - Voucher**:
   - Tính toán trên tổng giá trị rổ sau khi đã trừ Tầng 1 và Tầng 2.
   - Ví dụ: `FRESH20` chỉ áp dụng nếu tổng thịt cá sau Flash Sale vẫn $\ge 100,000\text{ đ}$.
4. **Tầng 4 - Điểm Tích Lũy**:
   - Quy đổi: 1,000 điểm = 1,000 đ. Tối đa khấu trừ không quá 20% tổng giá trị đơn hàng.
5. **Tầng 5 - Chiết Khấu Thành Viên**:
   - Thành viên Vàng (Gold): Giảm thêm 2%.
   - Thành viên Kim Cương (Diamond): Giảm thêm 5%.
