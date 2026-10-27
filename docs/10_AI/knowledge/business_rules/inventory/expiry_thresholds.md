# BUSINESS RULE: NGƯỠNG AN TOÀN TỒN KHO & CẢNH BÁO HẠN DÙNG

1. **Ngưỡng Safety Stock**:
   - Khi tồn kho khả dụng $\le \text{MinStockLevel}$ (mặc định 10 đơn vị cho hàng tươi): Hệ thống kích hoạt cảnh báo vàng `LowStockAlert`.
2. **Ngưỡng Expiry Lock**:
   - Khi $\text{ExpiryDate} \le \text{Now}$: Hệ thống tự động khóa bán trên toàn bộ POS và Web, chuyển trạng thái sang `Expired` để chờ lập phiếu tiêu hủy.
