# 10_AI - TÀI LIỆU 06: QUY TRÌNH NGHIỆP VỤ & LUỒNG THI CÔNG (WORKFLOWS)

---

## MỤC LỤC
1. [Quy trình 1: Tư vấn thực đơn & Nạp mâm cơm vào giỏ hàng](#1-quy-trình-1-tư-vấn-thực-đơn--nạp-mâm-cơm-vào-giỏ-hàng)
2. [Quy trình 2: Tự động phát hiện hàng cận date & kích hoạt Flash Sale](#2-quy-trình-2-tự-động-phát-hiện-hàng-cận-date--kích-hoạt-flash-sale)
3. [Quy trình 3: AI Copilot phân tích rủi ro kho & Lập phiếu nhập kho nháp](#3-quy-trình-3-ai-copilot-phân-tích-rủi-ro-kho--lập-phiếu-nhập-kho-nháp)
4. [Sơ đồ tuần tự chi tiết (End-to-End Sequence Diagram)](#4-sơ-đồ-tuần-tự-chi-tiết)

---

## 1. QUY TRÌNH 1: TƯ VẤN THỰC ĐƠN & NẠP MÂM CƠM VÀO GIỎ HÀNG

Đây là luồng tương tác chủ đạo giữa khách hàng mua sắm trên Web và Trợ lý AI:

```mermaid
flowchart TD
    A([Khách hàng nhập câu hỏi chat]) --> B{Phân tích ý định<br/>Intent Recognition}
    
    B -->|Chào hỏi| C[Trả về lời chào & Gợi ý mâm cơm]
    B -->|Mẹo ẩm thực thuần túy| D[Tư vấn bí quyết Cookpad: luộc rau, ướp bò...]
    B -->|Lên thực đơn / Hỏi món| E[Trích xuất Servings & TargetBudget]

    E --> F[Xác định Chủ đề món ăn<br/>DetermineDishTheme]
    F --> G[Truy vấn CSDL Kho hàng<br/>Products & InventoryBatches]
    G --> H[Khớp nguyên liệu chính xác theo CategoryId<br/>MatchProductsForQueries]
    H --> I[Xây dựng 3 Option mâm cơm Cookpad]
    
    I --> J{Gọi LLM Engine}
    J -->|Gemini Key hợp lệ| K[Google Gemini 2.5 Flash]
    J -->|Gemini lỗi/timeout| L[Groq Llama 3.3 70B]
    J -->|Không có mạng / API lỗi| M[Local Rule Fallback]

    K --> N[Tạo thông điệp hoàn chỉnh]
    L --> N
    M --> N

    N --> O[Render giao diện Chat Web<br/>Lời chào + 3 Thẻ Mâm Cơm Căn Trái]
    O --> P([Khách nhấn 'Chọn mâm này'])
    P --> Q[Mở Modal mâm nguyên liệu<br/>Tùy chỉnh thêm/bớt định lượng]
    Q --> R[Bấm 'Nạp vào giỏ hàng']
    R --> S[Lưu giỏ hàng & Điều hướng /cart]
```

---

## 2. QUY TRÌNH 2: TỰ ĐỘNG PHÁT HIỆN HÀNG CẬN DATE & KÍCH HOẠT FLASH SALE

Quy trình vận hành ngầm (Background Service) đảm bảo thực phẩm tươi sống được tiêu thụ trước khi hết hạn:

1. **Quét định kỳ (Daily Schedule)**: `ShelfLifeBackgroundService` kích hoạt mỗi sáng sớm.
2. **So khớp hạn sử dụng**: Duyệt qua toàn bộ lô hàng `InventoryBatches`, tính toán:
   $$\text{DaysLeft} = \text{ExpiryDate} - \text{DateTime.UtcNow}$$
3. **Áp dụng quy tắc chiết khấu**:
   - Nếu $\text{DaysLeft} \le 7$ ngày: Gán nhãn `isPromotionOrNearExpiry = true`, áp dụng giảm giá 30% (`DiscountPercent = 30%`).
   - Nếu $\text{DaysLeft} \le 0$ ngày: Tự động chuyển trạng thái lô hàng thành `Expired` và khóa bán.
4. **Đưa vào mâm cơm AI**: Khi khách hàng bật tùy chọn *"Ưu tiên hàng Flash Sale"*, AI ưu tiên chọn các lô hàng này vào thực đơn hàng ngày để khách hàng được hưởng mức giá tốt nhất, đồng thời giải phóng tồn kho cho siêu thị.

---

## 3. QUY TRÌNH 3: AI COPILOT PHÂN TÍCH RỦI RO KHO & LẬP PHIẾU NHẬP KHO NHÁP

Quy trình dành cho Quản lý siêu thị trên ứng dụng Desktop WinForms:

```mermaid
sequenceDiagram
    actor Manager as Quản lý Siêu thị
    participant WinForms as Desktop (AiCopilotView)
    participant API as Backend (AiRetailController)
    participant Svc as AiRetailService
    participant DB as CSDL Siêu thị

    Manager->>WinForms: Bấm "Phân tích rủi ro kho & Đề xuất nhập hàng"
    WinForms->>API: POST /api/v1/ai/retail/draft-import-order?supplierId=SUP001
    API->>Svc: GenerateDraftImportOrderAsync(supplierId)
    Svc->>DB: Truy vấn sản phẩm có tồn kho < MinStockLevel
    DB-->>Svc: Danh sách sản phẩm cạn kho & đơn giá hợp đồng
    Svc->>Svc: Tính số lượng cần nhập bù = (MinStockLevel * 2) - CurrentStock
    Svc-->>API: Trả về AiDraftImportOrder (Mã phiếu nháp, danh sách mặt hàng, tổng tiền)
    API-->>WinForms: Hiển thị bảng duyệt phiếu nhập trên WinForms
    Manager->>WinForms: Nhấn nút "1-Click Xác nhận lưu phiếu nhập"
    WinForms->>API: POST /api/v1/ai/retail/commit-draft-import
    API->>DB: INSERT ImportReceipts & ImportDetails (Status = Draft)
    DB-->>API: Ghi nhận thành công
    API-->>WinForms: Thông báo: "Đã lưu phiếu nhập kho PN-20261024-XXX thành công"
```

---

## 4. SƠ ĐỒ TUẦN TỰ CHI TIẾT (END-TO-END SEQUENCE)

```mermaid
sequenceDiagram
    actor User as Khách hàng
    participant Web as React Web Assistant
    participant API as /api/v1/ai/shopping/meal-plan
    participant Concierge as AiShoppingConciergeService
    participant LLM as Google Gemini 2.5 Flash
    participant Cart as Giỏ hàng (/cart)

    User->>Web: Nhập prompt: "Mình có 30k 1 người ăn thì nấu món gì"
    Web->>API: POST payload: { userPrompt, targetBudget: 30000, numberOfPeople: 1 }
    API->>Concierge: PlanMealAndShopAsync(request)
    Concierge->>Concierge: Nhận diện: servings=1, budget=30k -> Theme = "Bữa Ăn Tiết Kiệm"
    Concierge->>Concierge: Ghép 3 mâm: Trứng chiên canh rau (30.8k), Mì xúc xích (44.3k), Cá hộp dưa leo (28.9k)
    Concierge->>LLM: Gửi prompt tóm tắt + yêu cầu viết câu trả lời xưng "mình" gọi "bạn"
    LLM-->>Concierge: Trả về lời thoại đầu bếp thân thiện
    Concierge-->>API: MealPlanChatResponse (AssistantMessage + 3 MealOptions)
    API-->>Web: Trả về JSON kết quả
    Web->>User: Hiển thị lời thoại + 3 Option Cards (Căn lề trái)
    User->>Web: Nhấn "Chọn mâm này" tại Option 1
    Web->>Web: Mở Modal kiểm tra nguyên liệu: Trứng Ba Huân (1 vỉ), Rau muống (1 bó)
    User->>Web: Nhấn "Nạp nhanh vào giỏ hàng"
    Web->>Cart: Đẩy 2 sản phẩm vào LocalStorage / Redux Store
    Web-->>User: Toast: "Đã thêm thành công 2 nguyên liệu món Cơm Trứng Chiên vào giỏ hàng!"
```
