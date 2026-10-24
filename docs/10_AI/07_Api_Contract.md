# 10_AI - TÀI LIỆU 07: HỢP ĐỒNG GIAO TIẾP DỮ LIỆU (API CONTRACT)

---

## MỤC LỤC
1. [Quy ước chung API](#1-quy-ước-chung-api)
2. [API 1: Tư vấn thực đơn & giỏ hàng đi chợ (Shopping Meal Plan)](#2-api-1-tư-vấn-thực-đơn--giỏ-hàng-đi-chợ)
3. [API 2: Phân tích rủi ro kho & sản phẩm (Product Risks)](#3-api-2-phân-tích-rủi-ro-kho--sản-phẩm)
4. [API 3: Phân tích rổ hàng kết hợp (Market Basket Analysis)](#4-api-3-phân-tích-rổ-hàng-kết-hợp)
5. [API 4: Lập phiếu nhập kho nháp tự động (Draft Import Order)](#5-api-4-lập-phiếu-nhập-kho-nháp-tự-động)
6. [API 5: 1-Click Lưu phiếu nhập kho vào CSDL (Commit Draft Import)](#6-api-5-1-click-lưu-phiếu-nhập-kho-vào-csdl)

---

## 1. QUY ƯỚC CHUNG API
- **Base URL**: `http://localhost:5137/api/v1/ai`
- **Content-Type**: `application/json; charset=utf-8`
- **Định dạng bao bọc kết quả (Response Wrapper)**:
  ```json
  {
    "isSuccess": true,
    "message": "Thông báo kết quả",
    "data": { ... },
    "errors": []
  }
  ```

---

## 2. API 1: TƯ VẤN THỰC ĐƠN & GIỎ HÀNG ĐI CHỢ
- **Endpoint**: `POST /api/v1/ai/shopping/meal-plan`
- **Quyền truy cập**: Public (`[AllowAnonymous]`)
- **Mục đích**: Nhận yêu cầu tự nhiên của khách, phân tích ngân sách và trả về các mâm cơm đề xuất.

### Request Payload:
```json
{
  "userPrompt": "mình có 30k 1 người ăn thì nấu món gì",
  "targetBudget": 30000,
  "numberOfPeople": 1,
  "prioritizePromotions": true,
  "conversationHistory": [
    {
      "role": "user",
      "content": "xin chào"
    },
    {
      "role": "assistant",
      "content": "Chào bạn! Mình là Đầu Bếp & Trợ Lý Đi Chợ Thông Minh của Smart Supermarket 🛒..."
    }
  ]
}
```

### Response Payload (200 OK):
```json
{
  "isSuccess": true,
  "message": "Tư vấn thực đơn & giỏ hàng đi chợ thành công.",
  "data": {
    "assistantMessage": "Dạ, theo yêu cầu của bạn (1 người ăn, ngân sách dự kiến ~30,000 đ), mình đã cân đối và thiết kế 3 mâm cơm chuẩn vị theo cẩm nang Cookpad:...",
    "dishName": "Cơm Trứng Gà Chiên & Canh Rau Muống",
    "servings": 1,
    "totalEstimatedCost": 30800,
    "totalSavedAmount": 13200,
    "isWithinBudget": true,
    "hasMealSuggestion": true,
    "ingredients": [
      {
        "productId": 86,
        "productName": "Trứng gà tươi Ba Huân vỉ 10 quả size L",
        "barcode": "8939011019066",
        "imageUrl": "https://cdnv2.tgdd.vn/.../trung-ga.jpg",
        "unit": "Vỉ 10 quả",
        "quantityToBuy": 1,
        "regularPrice": 33000,
        "finalPrice": 23100,
        "discountPercent": 30,
        "isPromotionOrNearExpiry": true,
        "promotionNote": "Flash Sale cận date giảm 30%",
        "inStockQuantity": 80
      },
      {
        "productId": 131,
        "productName": "Rau muống nước tươi Bách Hóa Xanh bó 500g",
        "barcode": "8934588012131",
        "imageUrl": "https://cdnv2.tgdd.vn/.../rau-muong.jpg",
        "unit": "Bó 500g",
        "quantityToBuy": 1,
        "regularPrice": 11000,
        "finalPrice": 7700,
        "discountPercent": 30,
        "isPromotionOrNearExpiry": true,
        "promotionNote": "Flash Sale cận date giảm 30%",
        "inStockQuantity": 100
      }
    ],
    "mealOptions": [
      {
        "optionName": "Option 1: Cơm Trứng Chiên & Canh Rau Muống Luộc",
        "dishName": "Cơm Trứng Gà Chiên & Canh Rau Muống",
        "description": "Trứng gà Ba Huân chiên vàng xốp, ăn kèm canh rau muống xanh giòn dầm chanh thanh mát.",
        "totalEstimatedCost": 30800,
        "totalSavedAmount": 13200,
        "ingredients": [ ... ],
        "cookingTips": "1. Đập trứng vào bát, thêm nước mắm tiêu...\n2. Đun nóng chảo dầu...\n3. Nấu nước sôi luộc rau..."
      },
      {
        "optionName": "Option 2: Mì Omachi Xúc Xích & Cải Ngọt Siêu Tốc",
        "dishName": "Mì Nấu Xúc Xích Vissan & Cải Ngọt",
        "description": "Mì Omachi ngũ quả kèm xúc xích Vissan và rau cải ngọt VietGAP giòn ngọt, chế biến nhanh trong 5 phút.",
        "totalEstimatedCost": 44300,
        "totalSavedAmount": 4200,
        "ingredients": [ ... ],
        "cookingTips": "..."
      },
      {
        "optionName": "Option 3: Cá Hộp Ba Cô Gái & Dưa Leo Baby",
        "dishName": "Cá Nục Sốt Cà & Dưa Leo Baby",
        "description": "Cá nục sốt cà chua Ba Cô Gái sốt sánh mịn, ăn kèm dưa leo baby giòn mát và cơm nóng siêu đưa cơm.",
        "totalEstimatedCost": 28900,
        "totalSavedAmount": 5100,
        "ingredients": [ ... ],
        "cookingTips": "..."
      }
    ],
    "cookingTips": "1. Đập trứng vào bát, thêm nước mắm...\n2. Luộc rau muống nước ngập mở vung..."
  }
}
```

---

## 3. API 2: PHÂN TÍCH RỦI RO KHO & SẢN PHẨM
- **Endpoint**: `POST /api/v1/ai/retail/product-risks`
- **Mục đích**: Quét các mặt hàng cận date và hàng sắp hết kho để cảnh báo.
- **Response**: Trả về danh sách sản phẩm phân loại rủi ro (Cao, Trung bình, Thấp) kèm hành động đề xuất.

---

## 4. API 3: PHÂN TÍCH RỔ HÀNG KẾT HỢP
- **Endpoint**: `POST /api/v1/ai/retail/market-basket?daysBack=30`
- **Mục đích**: Khai phá các cặp sản phẩm thường xuyên được mua cùng nhau để gợi ý combo.
- **Response mẫu**:
```json
{
  "isSuccess": true,
  "data": {
    "recommendedCombos": [
      {
        "primaryProduct": "Thịt ba chỉ heo MeatDeli",
        "pairedProduct": "Trứng gà tươi Ba Huân",
        "confidenceScore": 0.85,
        "comboReason": "Khách hàng thường mua cùng nhau để nấu món Thịt kho tàu"
      }
    ]
  }
}
```

---

## 5. API 4: LẬP PHIẾU NHẬP KHO NHÁP TỰ ĐỘNG
- **Endpoint**: `POST /api/v1/ai/retail/draft-import-order?supplierId=1`
- **Mục đích**: Tự động tính toán số lượng thiếu hụt theo nhà cung cấp và lập phiếu nháp.
- **Response**: Trả về `AiDraftImportOrder` bao gồm các mặt hàng, số lượng đề xuất nhập bù và tổng kinh phí.

---

## 6. API 5: 1-CLICK LƯU PHIẾU NHẬP KHO VÀO CSDL
- **Endpoint**: `POST /api/v1/ai/retail/commit-draft-import`
- **Request Body**: Nhận đối tượng `AiDraftImportOrder` được phê duyệt từ quầy Quản lý.
- **Hành vi**: Tự động INSERT bản ghi vào bảng `ImportReceipts` và `ImportDetails` với trạng thái `Draft`, trả về `ReceiptCode` hoàn tất.
