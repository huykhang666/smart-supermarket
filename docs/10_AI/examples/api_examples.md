# EXAMPLES: CÁC REQUEST/RESPONSE API THỰC TẾ

Tài liệu này cung cấp các mẫu lệnh cURL và JSON payload thực tế cho 5 endpoint cốt lõi của hệ thống AI Smart SuperMarket.

---

## 1. ENDPOINT: TƯ VẤN THỰC ĐƠN & GỢI Ý MÂM CƠM (`/api/ai/meal-plan`)

### Lệnh cURL
```bash
curl -X POST "https://localhost:7198/api/ai/meal-plan" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <JWT_TOKEN>" \
  -d '{
    "people": 2,
    "budget": 100000,
    "dietaryPreference": "Ăn gia đình truyền thống miền Nam",
    "excludedIngredients": ["mắm tôm", "ớt cay"]
  }'
```

### JSON Response (HTTP 200 OK)
```json
{
  "source": "Gemini-2.5-Flash",
  "totalEstimatedPrice": 96500,
  "options": [
    {
      "optionNumber": 1,
      "title": "Cá Hú Kho Tộ & Canh Chua Rau Muống",
      "subtitle": "Bữa cơm đậm đà phong vị miền Tây sông nước",
      "totalOptionPrice": 92000,
      "servings": 2,
      "items": [
        {
          "productId": 45,
          "productName": "Cá Hú Cắt Khúc Tươi",
          "unit": "400g",
          "quantity": 1,
          "unitPrice": 48000,
          "lineTotal": 48000,
          "category": "Thịt Cá Hải Sản",
          "isNearExpiry": false,
          "inStock": true
        },
        {
          "productId": 112,
          "productName": "Rau Muống Nước VietGAP",
          "unit": "bó 500g",
          "quantity": 1,
          "unitPrice": 14000,
          "lineTotal": 14000,
          "category": "Rau Củ Tươi",
          "isNearExpiry": false,
          "inStock": true
        },
        {
          "productId": 89,
          "productName": "Cà Chua Đà Lạt",
          "unit": "300g",
          "quantity": 1,
          "unitPrice": 12000,
          "lineTotal": 12000,
          "category": "Rau Củ Tươi",
          "isNearExpiry": true,
          "discountPercent": 20,
          "inStock": true
        },
        {
          "productId": 205,
          "productName": "Gói Gia Vị Canh Chua Aji-Quick",
          "unit": "gói 50g",
          "quantity": 1,
          "unitPrice": 8000,
          "lineTotal": 8000,
          "category": "Gia Vị",
          "isNearExpiry": false,
          "inStock": true
        }
      ]
    }
  ]
}
```

---

## 2. ENDPOINT: TRÒ CHUYỆN VỚI AI SHOPPING CONCIERGE (`/api/ai/chat`)

### Lệnh cURL
```bash
curl -X POST "https://localhost:7198/api/ai/chat" \
  -H "Content-Type: application/json" \
  -d '{
    "message": "mình có 30k 1 người ăn thì nấu món gì",
    "conversationHistory": []
  }'
```

### JSON Response (HTTP 200 OK)
```json
{
  "reply": "Dạ, theo yêu cầu của bạn (1 người ăn, ngân sách dự kiến ~30,000 đ), mình đã cân đối và thiết kế 3 mâm cơm chuẩn vị theo cẩm nang Cookpad:\n\n🍲 Option 1: Cơm Trứng Chiên & Canh Rau Muống Luộc (~30,800 đ)\n✨ Option 2: Mì Omachi Xúc Xích & Cải Ngọt Siêu Tốc (~44,300 đ)\n🌿 Option 3: Cá Hộp Ba Cô Gái & Dưa Leo Baby (~28,900 đ)\n\n👉 Bạn có thể xem chi tiết nguyên liệu, giá tiền từng món và tùy ý thêm/bớt ở các thẻ mâm cơm bên dưới, sau đó bấm 'Chọn mâm này' để nạp nhanh vào giỏ hàng nhé!",
  "recommendedOptionCards": [
    {
      "id": 1,
      "title": "Cơm Trứng Chiên & Canh Rau Muống Luộc",
      "price": 30800,
      "itemCount": 3
    }
  ],
  "agentUsed": "ChefMealPlanner"
}
```

---

## 3. ENDPOINT: GỢI Ý BÁN CHÉO TẠI GIỎ HÀNG (`/api/ai/cart-recommendations`)

### Lệnh cURL
```bash
curl -X POST "https://localhost:7198/api/ai/cart-recommendations" \
  -H "Content-Type: application/json" \
  -d '{
    "cartProductIds": [45, 112]
  }'
```

### JSON Response (HTTP 200 OK)
```json
{
  "suggestions": [
    {
      "productId": 205,
      "productName": "Gói Gia Vị Canh Chua Aji-Quick",
      "price": 8000,
      "reason": "Khách thường mua cùng Cá Hú và Rau Muống để nấu canh chua đậm vị."
    },
    {
      "productId": 310,
      "productName": "Ớt Hiểm Tươi (100g)",
      "price": 5000,
      "reason": "Bổ sung vị cay the kích thích vị giác cho món cá kho tộ."
    }
  ]
}
```
