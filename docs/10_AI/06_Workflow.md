# 10_AI - TÀI LIỆU 06: QUY TRÌNH NGHIỆP VỤ & LUỒNG XỬ LÝ (AI WORKFLOWS)

---

## MỤC LỤC
1. [Luồng 1: Xử lý yêu cầu đi chợ & Lên thực đơn mâm cơm (Shopping Concierge Workflow)](#1-luồng-1-xử-lý-yêu-cầu-đi-chợ--lên-thực-đơn-mâm-cơm)
2. [Luồng 2: Tự động giám sát hạn sử dụng & Kích hoạt Flash Sale (Shelf-Life Workflow)](#2-luồng-2-tự-động-giám-sát-hạn-sử-dụng--kích-hoạt-flash-sale)
3. [Luồng 3: Quản trị viên phân tích tồn kho & Lập phiếu nhập kho 1-Click](#3-luồng-3-quản-trị-viên-phân-tích-tồn-kho--lập-phiếu-nhập-kho-1-click)
4. [Luồng 4: Tương tác khay nguyên liệu & Nạp giỏ hàng (Cart Injection Workflow)](#4-luồng-4-tương-tác-khay-nguyên-liệu--nạp-giỏ-hàng)
5. [Sơ đồ tuần tự tổng thể (Comprehensive Sequence Diagram)](#5-sơ-đồ-tuần-tự-tổng-thể)

---

## 1. LUỒNG 1: XỬ LÝ YÊU CẦU ĐI CHỢ & LÊN THỰC ĐƠN MÂM CƠM

Đây là luồng nghiệp vụ trung tâm kết nối người dùng Web với trí tuệ nhân tạo:

```mermaid
flowchart TD
    Start([1. Khách hàng gửi prompt trên Web]) --> IntentCheck{2. Nhận diện ý định<br/>Intent Recognition}
    
    IntentCheck -->|Chào hỏi| Greet[Trả về lời chào thân thiện & gợi ý mâm cơm]
    IntentCheck -->|Mẹo nấu ăn| Advice[Trả về cẩm nang bí quyết Cookpad]
    IntentCheck -->|Hỏi món / Lên thực đơn| ParseParams[3. Bóc tách tham số tự nhiên<br/>Servings & TargetBudget]

    ParseParams --> Theme[4. Xác định chủ đề món ăn<br/>DetermineDishTheme]
    Theme --> FetchDB[5. Truy vấn CSDL kho hàng<br/>Products & InventoryBatches]
    
    FetchDB --> CategoryMatch[6. Khớp nguyên liệu cách ly CategoryId<br/>MatchProductsForQueries]
    CategoryMatch --> BuildOptions[7. Xây dựng 3 Option mâm cơm Cookpad<br/>Option 1, Option 2, Option 3]

    BuildOptions --> LLMCall{8. Gọi LLM Engine}
    LLMCall -->|Gemini Key hợp lệ| Gemini[Google Gemini 2.5 Flash]
    LLMCall -->|Gemini lỗi/404/Quota| Groq[Groq Llama 3.3 70B]
    LLMCall -->|Không có API Key / Offline| LocalEngine[Local Cookpad Rule Engine]

    Gemini --> FormatMsg[9. Định dạng thông điệp hoàn chỉnh<br/>Tóm tắt 3 mâm + giá tiền, căn lề trái]
    Groq --> FormatMsg
    LocalEngine --> FormatMsg

    FormatMsg --> ReturnClient([10. Trả về MealPlanChatResponse cho Client])
```

---

## 2. LUỒNG 2: TỰ ĐỘNG GIÁM SÁT HẠN SỬ DỤNG & KÍCH HOẠT FLASH SALE

Quy trình tự động chạy ngầm bảo vệ hàng hóa và giải cứu thực phẩm tươi sống:

```mermaid
flowchart TD
    Timer([06:00 AM Hàng ngày]) --> Worker[ShelfLifeBackgroundService thức dậy]
    Worker --> ScanBatches[Quét toàn bộ lô hàng InventoryBatches]
    
    ScanBatches --> CalcDays[Tính DaysLeft = ExpiryDate - Today]
    
    CalcDays --> CheckDays{Kiểm tra DaysLeft}
    CheckDays -->|DaysLeft <= 0| LockBatch[Khóa bán lô hàng: Status = 'Expired']
    CheckDays -->|0 < DaysLeft <= 7| ApplyDiscount[Áp dụng DiscountRule: Giảm giá 30%]
    CheckDays -->|DaysLeft > 7| Safe[Trạng thái An toàn: Good]

    ApplyDiscount --> TagPromo[Gán nhãn Flash Sale cận date giảm 30%]
    TagPromo --> ConciergePriority[AI Concierge ưu tiên đưa vào thực đơn tiết kiệm]
```

---

## 3. LUỒNG 3: QUẢN TRỊ VIÊN PHÂN TÍCH TỒN KHO & LẬP PHIẾU NHẬP KHO 1-CLICK

Dành cho nhà quản lý siêu thị tại màn hình Desktop WinForms:

```mermaid
sequenceDiagram
    autonumber
    actor Manager as Quản lý Siêu thị
    participant WinForms as Desktop (AiCopilotView.cs)
    participant API as AiRetailController
    participant Svc as AiRetailService
    participant DB as CSDL Siêu thị

    Manager->>WinForms: Bấm "Phân tích rủi ro kho & Đề xuất nhập hàng"
    WinForms->>API: POST /api/v1/ai/retail/draft-import-order?supplierId=1
    API->>Svc: GenerateDraftImportOrderAsync(supplierId)
    Svc->>DB: Tìm sản phẩm có tồn kho < MinStockLevel
    DB-->>Svc: Danh sách mặt hàng thiếu hụt & giá hợp đồng
    Svc->>Svc: Tính số lượng cần bù = (MinStockLevel * 2) - CurrentStock
    Svc-->>API: Trả về AiDraftImportOrder
    API-->>WinForms: Hiển thị bảng duyệt phiếu nhập trên WinForms
    Manager->>WinForms: Kiểm tra & bấm "1-Click Xác nhận lưu phiếu nhập"
    WinForms->>API: POST /api/v1/ai/retail/commit-draft-import
    API->>DB: INSERT ImportReceipts & ImportDetails (Status = Draft)
    DB-->>API: Ghi nhận thành công
    API-->>WinForms: Thông báo: "Đã lưu phiếu nhập kho PN-20261027-XXX thành công"
```

---

## 4. LUỒNG 4: TƯƠNG TÁC KHAY NGUYÊN LIỆU & NẠP GIỎ HÀNG (CART INJECTION)

Luồng tương tác trực tiếp trên giao diện [AiShoppingAssistant.tsx](file:///D:/Laptrinhtrucquan/Frontend/src/components/chat/AiShoppingAssistant.tsx):

1. **Hiển thị thẻ mâm cơm**: Client nhận `MealPlanChatResponse`, hiển thị lời chào tóm tắt kèm 3 thẻ mâm cơm căn lề trái (mỗi thẻ có huy hiệu giá tiền xanh và danh sách nguyên liệu rút gọn).
2. **Kích hoạt khay mâm**: Khách bấm nút `"👉 Chọn mâm này (XX.000 đ)"`.
3. **Mở Modal khay nguyên liệu (`handleOpenMealModal`)**:
   - Hiển thị danh sách đầy đủ các nguyên liệu kèm hình ảnh sản phẩm.
   - Cho phép khách hàng:
     * Tăng / giảm số lượng từng món (+ / -).
     * Bỏ bớt món nếu nhà đã có sẵn (ví dụ: đã có nước mắm hoặc rau).
     * Đọc bí quyết nấu ăn chuẩn Cookpad (`CookingTips`).
4. **Nạp giỏ hàng 1-Click (`handleApplyMealToCart`)**:
   - Khách bấm `"🛒 Thêm nguyên liệu đã chọn vào giỏ hàng"`.
   - Hệ thống tự động đẩy các mặt hàng vào giỏ hàng `CartPage`, hiển thị Toast thông báo và nút bấm điều hướng nhanh `/cart`.

---

## 5. SƠ ĐỒ TUẦN TỰ TỔNG THỂ (COMPREHENSIVE SEQUENCE)

```mermaid
sequenceDiagram
    autonumber
    actor User as Khách hàng
    participant Web as React Web (AiShoppingAssistant)
    participant API as ASP.NET Core Backend
    participant Concierge as AiShoppingConciergeService
    participant LLM as Google Gemini 2.5 Flash
    participant Cart as Giỏ hàng (/cart)

    User->>Web: Nhập prompt: "Mình có 30k 1 người ăn thì nấu món gì"
    Web->>API: POST /api/v1/ai/shopping/meal-plan
    API->>Concierge: PlanMealAndShopAsync(request)
    Concierge->>Concierge: Nhận diện: servings=1, budget=30k -> Theme="Bữa Ăn Tiết Kiệm"
    Concierge->>Concierge: Ghép 3 mâm Cookpad (Trứng chiên 30.8k, Mì xúc xích 44.3k, Cá hộp 28.9k)
    Concierge->>LLM: Gửi prompt tóm tắt + yêu cầu viết câu trả lời xưng "mình" gọi "bạn"
    LLM-->>Concierge: Trả về lời thoại đầu bếp thân thiện
    Concierge-->>API: Trả về MealPlanChatResponse
    API-->>Web: HTTP 200 OK
    Web->>User: Hiển thị lời nhắn + 3 Thẻ Mâm Cơm Căn Trái
    User->>Web: Bấm "Chọn mâm này" ở Option 1 (Cơm Trứng Chiên & Canh Rau Muống)
    Web->>Web: Mở Modal xem chi tiết nguyên liệu
    User->>Web: Bấm "Thêm vào giỏ hàng"
    Web->>Cart: Đẩy 2 sản phẩm vào Giỏ hàng
    Web-->>User: Toast: "Đã thêm thành công 2 nguyên liệu vào giỏ hàng!"
```
