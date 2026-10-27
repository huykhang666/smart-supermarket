# 10_AI - TÀI LIỆU 07: HỢP ĐỒNG API & ĐẶC TẢ DTO (API CONTRACT & DTOS)

---

## MỤC LỤC
1. [Quy ước chung API](#1-quy-ước-chung-api)
2. [Đặc tả các lớp DTO (Data Transfer Objects)](#2-đặc-tả-các-lớp-dto)
   - 2.1. [DTOs Yêu cầu (Request DTOs)](#21-dtos-yêu-cầu-request-dtos)
   - 2.2. [DTOs Phản hồi (Response DTOs)](#22-dtos-phản-hồi-response-dtos)
   - 2.3. [DTOs Nghiệp vụ Bán lẻ & Quản trị kho](#23-dtos-nghiệp-vụ-bán-lẻ--quản-trị-kho)
3. [Đặc tả chi tiết các RESTful Endpoints](#3-đặc-tả-chi-tiết-các-restful-endpoints)
   - 3.1. [POST /api/v1/ai/shopping/meal-plan](#31-post-apiv1aishoppingmeal-plan)
   - 3.2. [POST /api/v1/ai/retail/product-risks](#32-post-apiv1airetailproduct-risks)
   - 3.3. [POST /api/v1/ai/retail/market-basket](#33-post-apiv1airetailmarket-basket)
   - 3.4. [POST /api/v1/ai/retail/draft-import-order](#34-post-apiv1airetaildraft-import-order)
   - 3.5. [POST /api/v1/ai/retail/commit-draft-import](#35-post-apiv1airetailcommit-draft-import)
4. [Bảng mã lỗi HTTP & Định dạng lỗi chuẩn (Standard Error Format)](#4-bảng-mã-lỗi-http--định-dạng-lỗi-chuẩn)

---

## 1. QUY ƯỚC CHUNG API

- **Base URL**: `http://localhost:5137/api/v1/ai`
- **Giao thức**: HTTP/1.1 & HTTPS, trao đổi dữ liệu định dạng JSON UTF-8.
- **Tiêu chuẩn DTO C#**: Toàn bộ file DTO trong thư mục `Features/AI/DTOs/` tuân thủ nguyên tắc **Strictly NO comments** (Không chứa bất kỳ chú thích nào trong mã nguồn DTO).
- **Định dạng bao bọc (Standard Result Envelope)**:
```json
{
  "isSuccess": true,
  "message": "Thông báo trạng thái",
  "data": { ... },
  "errors": []
}
```

---

## 2. ĐẶC TẢ CÁC LỚP DTO (DATA TRANSFER OBJECTS)

### 2.1. DTOs Yêu cầu (Request DTOs)

Mã nguồn C# chuẩn (`AiMealPlanRequests.cs`):
```csharp
namespace SmartSupermarket.Backend.Features.AI.DTOs.Requests;

public class ChatMessageDto
{
    public string Role { get; set; } = "user";
    public string Content { get; set; } = string.Empty;
}

public class MealPlanChatRequest
{
    public string UserPrompt { get; set; } = string.Empty;
    public decimal? TargetBudget { get; set; }
    public int? NumberOfPeople { get; set; }
    public bool PrioritizePromotions { get; set; } = true;
    public List<ChatMessageDto>? ConversationHistory { get; set; }
}
```

### 2.2. DTOs Phản hồi (Response DTOs)

Mã nguồn C# chuẩn (`AiMealPlanResponses.cs`):
```csharp
namespace SmartSupermarket.Backend.Features.AI.DTOs.Responses;

public class MealIngredientItemDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public int QuantityToBuy { get; set; }
    public decimal RegularPrice { get; set; }
    public decimal FinalPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public bool IsPromotionOrNearExpiry { get; set; }
    public string PromotionNote { get; set; } = string.Empty;
    public int InStockQuantity { get; set; }
}

public class MealOptionDto
{
    public string OptionName { get; set; } = string.Empty;
    public string DishName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal TotalEstimatedCost { get; set; }
    public decimal TotalSavedAmount { get; set; }
    public List<MealIngredientItemDto> Ingredients { get; set; } = new();
    public string CookingTips { get; set; } = string.Empty;
}

public class MealPlanChatResponse
{
    public string AssistantMessage { get; set; } = string.Empty;
    public string DishName { get; set; } = string.Empty;
    public int Servings { get; set; }
    public decimal TotalEstimatedCost { get; set; }
    public decimal TotalSavedAmount { get; set; }
    public bool IsWithinBudget { get; set; }
    public bool HasMealSuggestion { get; set; }
    public List<MealIngredientItemDto> Ingredients { get; set; } = new();
    public List<MealOptionDto> MealOptions { get; set; } = new();
    public string CookingTips { get; set; } = string.Empty;
}
```

### 2.3. DTOs Nghiệp vụ Bán lẻ & Quản trị kho

```csharp
namespace SmartSupermarket.Backend.Features.AI.DTOs;

public class AiProductRiskReport
{
    public int TotalProductsScanned { get; set; }
    public int HighRiskCount { get; set; }
    public List<ShelfLifeItemDetailResponse> NearExpiryItems { get; set; } = new();
    public List<ProductStockRiskItem> OutOfStockRiskItems { get; set; } = new();
}

public class AiDraftImportItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int MinStockLevel { get; set; }
    public int SuggestedImportQuantity { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal LineTotal { get; set; }
}

public class AiDraftImportOrder
{
    public string DraftCode { get; set; } = string.Empty;
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public int BranchId { get; set; } = 1;
    public decimal TotalEstimatedAmount { get; set; }
    public List<AiDraftImportItem> Items { get; set; } = new();
}
```

---

## 3. ĐẶC TẢ CHI TIẾT CÁC RESTFUL ENDPOINTS

### 3.1. POST /api/v1/ai/shopping/meal-plan
- **Quyền hạn**: Public (`[AllowAnonymous]`).
- **Mục đích**: Nhận prompt tự nhiên từ khách hàng, sinh mâm cơm và nguyên liệu tương thích kho hàng.

#### Request Body:
```json
{
  "userPrompt": "mình có 30k 1 người ăn thì nấu món gì",
  "targetBudget": 30000,
  "numberOfPeople": 1,
  "prioritizePromotions": true,
  "conversationHistory": []
}
```

#### Response Body (200 OK):
```json
{
  "isSuccess": true,
  "message": "Tư vấn thực đơn & giỏ hàng đi chợ thành công.",
  "data": {
    "assistantMessage": "Dạ, theo yêu cầu của bạn (**1 người ăn**, ngân sách dự kiến **~30,000 đ**), mình đã cân đối và thiết kế **3 mâm cơm chuẩn vị theo cẩm nang Cookpad**:\n\n🍲 **Option 1: Cơm Trứng Chiên & Canh Rau Muống Luộc** (~**30,800 đ**)\n   *Trứng gà Ba Huân chiên vàng xốp, ăn kèm canh rau muống xanh giòn dầm chanh thanh mát.*\n✨ **Option 2: Mì Omachi Xúc Xích & Cải Ngọt Siêu Tốc** (~**44,300 đ**)\n   *Mì Omachi ngũ quả kèm xúc xích Vissan và rau cải ngọt VietGAP giòn ngọt, chế biến nhanh trong 5 phút.*\n🌿 **Option 3: Cá Hộp Ba Cô Gái & Dưa Leo Baby** (~**28,900 đ**)\n   *Cá nục sốt cà chua Ba Cô Gái sốt sánh mịn, ăn kèm dưa leo baby giòn mát và cơm nóng siêu đưa cơm.*\n\n👉 Bạn có thể xem chi tiết nguyên liệu, giá tiền từng món và tùy ý thêm/bớt ở các thẻ mâm cơm bên dưới, sau đó bấm **\"Chọn mâm này\"** để nạp nhanh vào giỏ hàng nhé!",
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
        "cookingTips": "1. Đập trứng vào bát, thêm nước mắm tiêu...\n2. Đun nóng chảo dầu...\n3. Luộc rau muống nước ngập mở nắp vung..."
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
    "cookingTips": "1. Đập trứng vào bát, thêm nước mắm...\n2. Luộc rau muống lửa lớn không đậy vung..."
  }
}
```

### 3.2. POST /api/v1/ai/retail/product-risks
- **Mục đích**: Quét tự động danh mục hàng hóa, phân loại sản phẩm có nguy cơ cận date hoặc thiếu hụt tồn kho.

### 3.3. POST /api/v1/ai/retail/market-basket
- **Tham số Query**: `daysBack` (int, mặc định = 30).
- **Mục đích**: Khai phá các cặp sản phẩm thường xuyên được mua cùng nhau để gợi ý combo tại quầy thu ngân.

### 3.4. POST /api/v1/ai/retail/draft-import-order
- **Tham số Query**: `supplierId` (int, mặc định = 1).
- **Mục đích**: Tự động tính toán lượng thiếu hụt theo nhà cung cấp và lập phiếu nháp.

### 3.5. POST /api/v1/ai/retail/commit-draft-import
- **Request Body**: Nhận đối tượng `AiDraftImportOrder` do người quản lý phê duyệt.
- **Hành vi**: INSERT trực tiếp vào bảng `ImportReceipts` và `ImportDetails` với trạng thái `Draft`.

---

## 4. BẢNG MÃ LỖI HTTP & ĐỊNH DẠNG LỖI CHUẨN

| Mã HTTP | Tên mã | Tình huống phát sinh | Giải pháp xử lý |
| :---: | :--- | :--- | :--- |
| **200** | `OK` | Xử lý yêu cầu AI thành công. | Client hiển thị dữ liệu bình thường. |
| **400** | `Bad Request` | JSON payload không đúng cấu trúc (thiếu trường, sai định dạng số). | Kiểm tra lại serializer và kiểu dữ liệu request. |
| **404** | `Not Found` | Không tìm thấy mã nhà cung cấp hoặc sản phẩm liên quan. | Hiển thị thông báo dữ liệu không tồn tại. |
| **500** | `Internal Error` | Lỗi ngoại lệ CSDL hoặc sự cố hệ thống chưa bẫy được. | Ghi log hệ thống, kích hoạt Fallback Rule Engine. |
