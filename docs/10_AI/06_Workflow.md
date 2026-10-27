# 10_AI - TÀI LIỆU 06: QUY TRÌNH NGHIỆP VỤ & LUỒNG XỬ LÝ (AI WORKFLOWS)

---

## MỤC LỤC
1. [Luồng đầy đủ: Tư vấn mâm cơm đa tác nhân (End-to-End Meal Planning Workflow)](#1-luồng-đầy-đủ-tư-vấn-mâm-cơm-đa-tác-nhân)
2. [Luồng nạp giỏ hàng: 1-Click Add Cart (Add Cart Workflow)](#2-luồng-nạp-giỏ-hàng-1-click-add-cart)
3. [Luồng xử lý khi hết hàng & Đề xuất thay thế (Out of Stock & Alternative Workflow)](#3-luồng-xử-lý-khi-hết-hàng--đề-xuất-thay-thế)
4. [Luồng tự động giám sát hạn dùng & Flash Sale (Shelf-Life Workflow)](#4-luồng-tự-động-giám-sát-hạn-dùng--flash-sale)

---

## 1. LUỒNG ĐẦY ĐỦ: TƯ VẤN MÂM CƠM ĐA TÁC NHÂN

Áp dụng cho tình huống khách hàng gửi yêu cầu mua sắm và nấu ăn (Ví dụ: *"Tôi có 300k, muốn nấu lẩu cho 5 người, có giảm giá thì ưu tiên"*).

```mermaid
flowchart TD
    User([Khách Hàng]) -->|1. '300k nấu lẩu 5 người'| UI[Frontend Chat UI]
    UI -->|2. POST /api/ai/chat| Ctrl[AIController]
    Ctrl -->|3. Validate & Chuyển giao| Orch[AgentOrchestrator]
    
    Orch -->|4. Phân tích Intent| ShopAgent[ShoppingAgent]
    
    subgraph Execution_Pipeline["Chuỗi Thực Thi Đa Tác Nhân"]
        ShopAgent -->|5. Tìm công thức| RecAgent[RecipeAgent]
        RecAgent -->|6. Call Tool| RecTool[RecipeTool]
        RecTool -->|7. Query Service| RecSvc[RecipeService]
        RecSvc -->|8. Query Repo| RecRepo[RecipeRepository]
        RecRepo -->|9. Đọc CSDL Cookpad| RecData[(Recipe Data)]
        RecData -.->|Trả nguyên liệu| ShopAgent

        ShopAgent -->|10. Kiểm tra tồn & date| InvAgent[InventoryAgent]
        InvAgent -->|11. Call Tool| InvTool[InventoryTool]
        InvTool -->|12. Query Service| InvSvc[InventoryService]
        InvSvc -->|13. Query Repo| InvRepo[InventoryRepository]
        InvRepo -->|14. Đọc kho hàng| InvData[(Inventory Batches)]
        InvData -.->|Lọc lô cận date| ShopAgent

        ShopAgent -->|15. Kiểm tra khuyến mãi| PromoAgent[PromotionAgent]
        PromoAgent -->|16. Call Tool| PromoTool[PromotionTool]
        PromoTool -->|17. Query Service| PromoSvc[PromotionService]
        PromoSvc -->|18. Query Repo| PromoRepo[PromotionRepository]
        PromoRepo -->|19. Đọc Voucher/Promo| PromoData[(Promotions)]
        PromoData -.->|Áp dụng giảm 30%| ShopAgent

        ShopAgent -->|20. Cân đối chi phí| BudAgent[BudgetAgent]
        BudAgent -->|21. Call Tool| BudTool[ProductTool]
        BudTool -->|22. Query Service| BudSvc[ProductService]
        BudSvc -.->|Tổng chi <= 300k| ShopAgent
    end

    ShopAgent -->|23. Tổng hợp DTO| RespBuilder[Response Builder]
    RespBuilder -->|24. Return DTO| Ctrl
    Ctrl -->|25. HTTP 200 OK| UI
    UI -->|26. Hiển thị mâm cơm| User
```

---

## 2. LUỒNG NẠP GIỎ HÀNG: 1-CLICK ADD CART

Áp dụng khi khách hàng bấm nút **"Chọn mâm này"** hoặc gửi tin nhắn *"Tôi lấy toàn bộ mâm lẩu này"*.

```mermaid
flowchart TD
    User([Khách Hàng]) -->|'Tôi lấy toàn bộ mâm này'| UI[Frontend UI]
    UI -->|POST /api/ai/cart/inject| Ctrl[AIController]
    Ctrl --> Orch[AgentOrchestrator]
    Orch --> ShopAgent[ShoppingAgent]
    ShopAgent --> CartAgent[CartAgent]
    
    CartAgent --> CartTool[CartTool.AddToCartAsync]
    CartTool --> OrderSvc[OrderService / CartService]
    OrderSvc --> OrderRepo[OrderRepository]
    OrderRepo --> DB[(SQL Server - Cart & CartItems)]
    
    DB -.->|Thêm thành công| OrderSvc
    OrderSvc -.->|Giỏ hàng cập nhật| CartAgent
    CartAgent -.->|Cart Status OK| ShopAgent
    ShopAgent --> Resp[Thông báo: 'Đã thêm trọn bộ nguyên liệu vào giỏ hàng!']
    Resp --> Ctrl --> UI --> User
```

---

## 3. LUỒNG XỬ LÝ KHI HẾT HÀNG & ĐỀ XUẤT THAY THẾ (OUT OF STOCK & ALTERNATIVE)

Khi một nguyên liệu trong công thức nấu ăn bị hết hàng tại siêu thị, hệ thống tự động tìm kiếm món thay thế tương đương cùng ngành hàng:

```mermaid
flowchart TD
    InvCheck[Kiểm tra tồn kho nguyên liệu] --> StockCheck{StockQuantity == 0?}
    StockCheck -- Không (Còn hàng) --> NormalFlow[Sử dụng nguyên liệu gốc]
    StockCheck -- Có (Hết hàng) --> TriggerInv[Kích hoạt InventoryAgent]
    
    TriggerInv --> CallAltTool[Gọi ProductTool.GetAlternativeAsync]
    CallAltTool --> ProdSvc[ProductService]
    
    subgraph Alt_Resolution["Thuật Toán Tìm Sản Phẩm Thay Thế"]
        ProdSvc --> MatchCategory[1. Khóa chặt cùng CategoryId]
        MatchCategory --> MatchNutrition[2. Tương đồng giá trị đạm / calo]
        MatchNutrition --> CheckStock[3. Sản phẩm thay thế phải có Stock > 0]
        CheckStock --> CheckPrice[4. Đơn giá tương đương hoặc rẻ hơn]
    end

    ProdSvc --> ReturnAlt[Trả về sản phẩm thay thế]
    ReturnAlt --> ShopAgent[ShoppingAgent gắn nhãn: 'Đã thay thế nguyên liệu']
    ShopAgent --> NotifyUser[Thông báo minh bạch lý do thay thế cho người dùng]
```

---

## 4. LUỒNG TỰ ĐỘNG GIÁM SÁT HẠN DÙNG & FLASH SALE (SHELF-LIFE WORKFLOW)

Quy trình tự động chạy ngầm bảo vệ hàng hóa và giải cứu thực phẩm tươi sống:
1. **06:00 AM Hàng ngày**: `ShelfLifeBackgroundService` quét toàn bộ bảng `ProductBatches`.
2. **Tính số ngày còn lại**: $\text{DaysLeft} = \text{ExpiryDate} - \text{Today}$.
3. **Phân loại**:
   - $\text{DaysLeft} \le 0$: Khóa bán ngay lập tức (`Status = Expired`).
   - $0 < \text{DaysLeft} \le 1$: Kích hoạt Flash Sale xả hàng **giảm 50%**.
   - $1 < \text{DaysLeft} \le 3$: Kích hoạt Flash Sale **giảm 30%**.
   - $\text{DaysLeft} > 3$: Trạng thái an toàn bình thường.
4. `PromotionAgent` và `ShoppingAgent` tự động nhận diện các lô hàng này để đưa vào danh mục mâm cơm gợi ý cho khách hàng trong ngày.
