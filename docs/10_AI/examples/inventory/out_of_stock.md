# INVENTORY EXAMPLE: XỬ LÝ HẾT HÀNG (OUT OF STOCK)

**Tình huống**: Khách muốn mua `Thịt Bò Úc Pacow 300g` nhưng hệ thống báo `StockQuantity = 0`.

**Xử lý nội bộ**:
1. `InventoryAgent` phát hiện `Stock = 0`.
2. Kích hoạt `ProductTool.GetAlternativeAsync(productId=104, categoryId=9)`.
3. Tìm thấy `Thịt Bò Ta Tươi Cắt Lát (300g) - Còn tồn: 18 khay`.

**AI phản hồi**:
> Dạ hiện tại mặt hàng **Thịt Bò Ba Chỉ Úc Pacow** tại chi nhánh vừa tạm hết hàng. Để bữa ăn của bạn không bị gián đoạn, mình xin phép thay thế bằng **Thịt Bò Ta Tươi Cắt Lát (300g)** với giá mềm hơn (chỉ **75,000 đ** so với 105,000 đ của Bò Úc).  
> Thịt bò ta thái mỏng vẫn đảm bảo độ tươi ngọt và mềm khi xào hoặc nhúng lẩu bạn nhé!
