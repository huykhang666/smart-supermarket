# WORKFLOW: QUY TRÌNH THAO TÁC GIỎ HÀNG (CART INJECTION WORKFLOW)

```mermaid
flowchart TD
    Click([Khách bấm 'Chọn mâm này' hoặc yêu cầu bằng lời]) --> Ctrl[AIController]
    Ctrl --> Orch[AgentOrchestrator]
    Orch --> CartAgent[CartAgent]
    CartAgent --> Dispatcher[ToolDispatcher]
    Dispatcher --> CartTool[CartTool.AddToCartAsync]
    CartTool --> OrderSvc[OrderService]
    OrderSvc --> OrderRepo[OrderRepository]
    OrderRepo --> DB[(SQL Server - CartItems)]
    
    DB -.->|Ghi nhận thành công| OrderSvc
    OrderSvc -.-> CartAgent
    CartAgent --> Notif[Thông báo: 'Đã thêm nguyên liệu vào giỏ hàng!']
    Notif --> UI[Cập nhật giỏ hàng góc phải màn hình]
```

---

## Các Bước Thực Thi Chi Tiết
1. **Trích xuất nguyên liệu**: Nhận danh sách các `ProductId` và `Quantity` từ thẻ mâm cơm được khách chọn.
2. **Kiểm tra kho lần cuối**: Đảm bảo toàn bộ các món trong mâm vẫn còn đủ hàng trong kho ngay tại thời điểm bấm nút.
3. **Nạp vào giỏ**: Ghi các bản ghi vào bảng `CartItems` gắn liền với `SessionId` của khách hàng.
4. **Phản hồi**: Hiển thị badge số lượng sản phẩm trên icon Giỏ hàng và mở slide-over kiểm tra.
