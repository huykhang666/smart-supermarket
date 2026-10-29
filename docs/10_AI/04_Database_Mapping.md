# 04_Database_Mapping.md

# Database Mapping

Version: 1.1

---

# 1. Database Platform

Target DBMS:
- PostgreSQL 15+ (Production)
- Entity Framework Core In-Memory (Testing)

Connection Driver:
- Npgsql.EntityFrameworkCore.PostgreSQL

Timestamp & Date Handling:
- Sử dụng UTC timestamp
- Dùng `NOW()` hoặc `NOW() + INTERVAL '3 days'` khi cần tính khoảng thời gian

JSON Mapping:
- Sử dụng kiểu dữ liệu `JSONB` để lưu metadata và context mở rộng

---

# 2. Mapping Rule

Cấu trúc chuẩn:

Table → Entity → Repository → Service → DTO → Tool → Agent

Ví dụ:

Products (Table) → Product (Entity) → ProductRepository → ProductService → ProductDTO → ProductTool → ShoppingAgent

---

# 3. Mapping theo tầng

## Products
- Table: `products`
- Entity: `Product`
- Repository: `ProductRepository`
- Service: `ProductService`
- DTO: `ProductDto`
- Tool: `ProductTool`
- Agent: `ShoppingAgent`

## Inventory
- Table: `inventories`, `inventory_batches`
- Entity: `Inventory`, `InventoryBatch`
- Repository: `InventoryRepository`
- Service: `InventoryService`
- DTO: `InventoryDto`, `InventoryBatchDto`
- Tool: `InventoryTool`
- Agent: `InventoryAgent`

## Promotion
- Table: `promotions`, `discount_rules`, `vouchers`
- Entity: `Promotion`, `DiscountRule`, `Voucher`
- Repository: `PromotionRepository`
- Service: `PromotionService`
- DTO: `PromotionDto`
- Tool: `PromotionTool`
- Agent: `PromotionAgent`

## Recipe
- Table: knowledge / recipe source / recipe store
- Entity: `Recipe`, `RecipeIngredient`
- Repository: `RecipeRepository`
- Service: `RecipeService`
- DTO: `RecipeDto`
- Tool: `RecipeTool`
- Agent: `RecipeAgent`

## Budget
- Table: derived from order/cart data
- Entity: `BudgetSnapshot`
- Repository: `BudgetRepository`
- Service: `BudgetService`
- DTO: `BudgetDto`
- Tool: `BudgetTool`
- Agent: `BudgetAgent`

## Cart
- Table: `orders`, `order_details`
- Entity: `CartItem`, `Order`, `OrderDetail`
- Repository: `CartRepository`, `OrderRepository`
- Service: `CartService`, `OrderService`
- DTO: `CartDto`
- Tool: `CartTool`
- Agent: `CartAgent`

---

# 4. PostgreSQL-specific Notes

## 4.1 Date Handling
- Dùng `NOW()` cho timestamp hiện tại
- Dùng `NOW() + INTERVAL '3 days'` cho ngưỡng cận date
- Không dùng T-SQL `GETDATE()` hay `DATEADD(...)`

## 4.2 JSONB
- Dùng `JSONB` cho:
  - metadata
  - conversation context mở rộng
  - prompt snapshot
  - tool call payload

## 4.3 Identifier & Naming
- Dùng snake_case cho table/column theo chuẩn PostgreSQL
- Entity vẫn giữ PascalCase theo C#

---

# 5. Database Boundary Rules

- Agent không đọc table trực tiếp
- Tool không đọc table trực tiếp
- Service mới đọc Repository
- Repository mới đọc DbContext
- DbContext mới truy cập PostgreSQL

---

# 6. Domain-to-Tool Mapping Examples

## Product search flow
`products` → `ProductRepository` → `ProductService` → `ProductTool` → `ShoppingAgent`

## Inventory check flow
`inventories` + `inventory_batches` → `InventoryRepository` → `InventoryService` → `InventoryTool` → `InventoryAgent`

## Promotion flow
`promotions` + `discount_rules` → `PromotionRepository` → `PromotionService` → `PromotionTool` → `PromotionAgent`
