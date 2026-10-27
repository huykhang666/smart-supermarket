# 10_AI - TÀI LIỆU 04: ÁNH XẠ DỮ LIỆU CSDL → REPOSITORY → SERVICE → TOOL → AGENT (DATABASE MAPPING)

---

## MỤC LỤC
1. [Chuỗi chuyển đổi dữ liệu 7 bước chuẩn Enterprise](#1-chuỗi-chuyển-đổi-dữ-liệu-7-bước-chuẩn-enterprise)
2. [Bảng ánh xạ toàn phần từng thực thể cốt lõi](#2-bảng-ánh-xạ-toàn-phần-từng-thực-thể-cốt-lõi)
   - [2.1. Chuỗi ánh xạ Sản Phẩm (Products Mapping)](#21-chuỗi-ánh-xạ-sản-phẩm-products-mapping)
   - [2.2. Chuỗi ánh xạ Tồn Kho & Lô Hàng Cận Date (InventoryBatches Mapping)](#22-chuỗi-ánh-xạ-tồn-kho--lô-hàng-cận-date-inventorybatches-mapping)
   - [2.3. Chuỗi ánh xạ Khuyến Mãi & Voucher (Promotions Mapping)](#23-chuỗi-ánh-xạ-khuyến-mãi--voucher-promotions-mapping)
   - [2.4. Chuỗi ánh xạ Công Thức Ẩm Thực (Recipes Mapping)](#24-chuỗi-ánh-xạ-công-thức-ẩm-thực-recipes-mapping)
   - [2.5. Chuỗi ánh xạ Giỏ Hàng (Cart & Orders Mapping)](#25-chuỗi-ánh-xạ-giỏ-hàng-cart--orders-mapping)
3. [Quy tắc cách ly Danh mục (Category Isolation Matrix)](#3-quy-tắc-cách-ly-danh-mục-category-isolation-matrix)
4. [Tối ưu hóa Token & Compact Context](#4-tối-ưu-hóa-token--compact-context)

---

## 1. CHUỖI CHUYỂN ĐỔI DỮ LIỆU 7 BƯỚC CHUẨN ENTERPRISE

Để đảm bảo tính toàn vẹn dữ liệu, hiệu năng và tránh rò rỉ các trường thông tin nhạy cảm của hệ thống, dữ liệu không bao giờ nhảy cóc trực tiếp từ CSDL sang Agent mà phải đi qua chuỗi ánh xạ 7 bước khép kín:

```text
Table (SQL Server)
  │
  ▼
Entity (C# Model)
  │
  ▼
Repository (EF Core Query: AsNoTracking, Where, Select)
  │
  ▼
Business Service (Business Rules, Tính toán Flash Sale)
  │
  ▼
DTO (Clean Data Transfer Object, No Comments)
  │
  ▼
Tool (Interface cho LLM / Function Calling)
  │
  ▼
Agent (ShoppingAgent / InventoryAgent / BudgetAgent)
```

---

## 2. BẢNG ÁNH XẠ TOÀN PHẦN TỪNG THỰC THỂ CỐT LÕI

### 2.1. Chuỗi ánh xạ Sản Phẩm (Products Mapping)

$$\text{Products (Table)} \longrightarrow \text{Product (Entity)} \longrightarrow \text{ProductRepository} \longrightarrow \text{ProductService} \longrightarrow \text{ProductDto} \longrightarrow \text{ProductTool} \longrightarrow \text{ShoppingAgent}$$

| Tầng | Tên Thành Phần | Trường Dữ Liệu / Trách Nhiệm |
|---|---|---|
| **SQL Table** | `dbo.Products` | `Id`, `Name`, `Price`, `CostPrice`, `StockQuantity`, `CategoryId`, `IsActive` |
| **Entity** | `Product.cs` | Entity Framework mapping có navigation property `Category` |
| **Repository** | `ProductRepository.cs` | `_context.Products.AsNoTracking().Where(p => p.IsActive && p.StockQuantity > 0)` |
| **Service** | `ProductService.cs` | Kiểm tra điều kiện kinh doanh, lọc bỏ sản phẩm tạm ngưng bán |
| **DTO** | `ProductDto.cs` | `Id`, `Name`, `Price`, `CategoryName`, `InStock` *(Loại bỏ CostPrice)* |
| **Tool** | `ProductTool.cs` | Cung cấp method `Search(keyword, categoryId)`, `GetAlternative(id)` |
| **Agent** | `ShoppingAgent` | Sử dụng thông tin sản phẩm để lắp ghép vào mâm cơm Cookpad |

---

### 2.2. Chuỗi ánh xạ Tồn Kho & Lô Hàng Cận Date (InventoryBatches Mapping)

$$\text{ProductBatches (Table)} \longrightarrow \text{ProductBatch (Entity)} \longrightarrow \text{InventoryRepository} \longrightarrow \text{InventoryService} \longrightarrow \text{NearExpiryBatchDto} \longrightarrow \text{InventoryTool} \longrightarrow \text{InventoryAgent}$$

| Tầng | Tên Thành Phần | Trường Dữ Liệu / Trách Nhiệm |
|---|---|---|
| **SQL Table** | `dbo.ProductBatches` | `Id`, `ProductId`, `BatchCode`, `Quantity`, `ExpiryDate`, `ImportDate` |
| **Entity** | `ProductBatch.cs` | Entity mapping có khóa ngoại tới `Product` |
| **Repository** | `InventoryRepository.cs` | Query các lô hàng: `Where(b => b.ExpiryDate <= targetDate && b.Quantity > 0)` |
| **Service** | `InventoryService.cs` | **Business Logic cốt lõi**: Tính $\text{DaysLeft} = \text{ExpiryDate} - \text{Now}$. Nếu $\le 1$ ngày $\rightarrow$ giảm 50%; nếu $\le 3$ ngày $\rightarrow$ giảm 30% Flash Sale. |
| **DTO** | `NearExpiryBatchDto.cs` | `ProductId`, `ProductName`, `DiscountPercent`, `FlashSalePrice`, `DaysLeft` |
| **Tool** | `InventoryTool.cs` | Cung cấp method `GetNearExpiry(daysThreshold)` |
| **Agent** | `InventoryAgent` | Báo cáo danh sách cứu trợ tồn kho cho `ShoppingAgent` |

---

### 2.3. Chuỗi ánh xạ Khuyến Mãi & Voucher (Promotions Mapping)

$$\text{Promotions (Table)} \longrightarrow \text{Promotion (Entity)} \longrightarrow \text{PromotionRepository} \longrightarrow \text{PromotionService} \longrightarrow \text{VoucherDto} \longrightarrow \text{PromotionTool} \longrightarrow \text{PromotionAgent}$$

- **Service Logic**: Kiểm tra đơn hàng có đủ điều kiện áp mã `FRESH20` (tối thiểu 100k thịt cá tươi Category 9) không.
- **DTO**: Trả về `Code`, `DiscountAmount`, `MinOrderValue`, `IsEligible`.
- **Tool**: `PromotionTool.GetApplicableVouchers()`.

---

### 2.4. Chuỗi ánh xạ Công Thức Ẩm Thực (Recipes Mapping)

$$\text{Recipes (Table / Seed)} \longrightarrow \text{Recipe (Entity)} \longrightarrow \text{RecipeRepository} \longrightarrow \text{RecipeService} \longrightarrow \text{RecipeDto} \longrightarrow \text{RecipeTool} \longrightarrow \text{RecipeAgent}$$

- **Nhiệm vụ**: Bóc tách mâm cơm theo số người ăn (`servings`), tính định lượng nguyên liệu chính xác, tránh hoàn toàn ảo giác.

---

### 2.5. Chuỗi ánh xạ Giỏ Hàng (Cart & Orders Mapping)

$$\text{CartItems (Table)} \longrightarrow \text{CartItem (Entity)} \longrightarrow \text{OrderRepository} \longrightarrow \text{OrderService} \longrightarrow \text{CartStatusDto} \longrightarrow \text{CartTool} \longrightarrow \text{CartAgent}$$

- **Nhiệm vụ**: Cho phép 1-Click nạp trọn bộ nguyên liệu của mâm cơm vào giỏ hàng của người dùng.

---

## 3. QUY TẮC CÁCH LY DANH MỤC (CATEGORY ISOLATION MATRIX)

Để ngăn chặn việc xếp nhầm thực phẩm (như xếp nhầm mì gói hoặc hạt nêm vào danh mục thịt tươi):

| Category ID | Tên Ngành Hàng | Đại Diện CSDL | Quy Tắc Ràng Buộc AI |
|---|---|---|---|
| **9** | **Thịt, Trứng & Thủy Hải Sản** | `MeatDeli`, `Thịt bò Úc`, `Cá hú`, `Trứng Ba Huân` | Tuyệt đối cấm đưa sản phẩm khô hoặc đồ hộp vào nhóm này. |
| **4** | **Rau Củ Tươi** | `Rau muống`, `Cải ngọt VietGAP`, `Cà chua` | Chỉ chứa rau củ tươi sống có hạn dùng ngắn. |
| **10** | **Đồ Hộp & Ăn Liền** | `Mì Hảo Hảo`, `Cá hộp Ba Cô Gái` | Nhóm đồ khô, tiện lợi cho sinh viên. |
| **5** | **Gia Vị** | `Nước mắm Nam Ngư`, `Dầu ăn Simply`, `Aji-Quick` | Chỉ tính phụ gia nấu nướng. |

---

## 4. TỐI ƯU HÓA TOKEN & COMPACT CONTEXT

Khi nạp dữ liệu từ DTO vào prompt cho LLM:
- **Loại bỏ trường thừa**: Không truyền `CreatedAt`, `UpdatedAt`, `Barcode`, `SupplierId` vào LLM.
- **Compact JSON Template**:
  ```json
  [{"id":105,"name":"Nạc Dăm Heo C.P","price":50400,"discount":30,"cat":9}]
  ```
  Giúp tiết kiệm 70% Token Context, giảm thời gian xử lý xuống còn $<1.2\text{s}$.
