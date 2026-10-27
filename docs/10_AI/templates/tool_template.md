# TEMPLATE: KHAI BÁO & ĐĂNG KÝ TOOL / FUNCTION CALLING (TOOL SPECIFICATION TEMPLATE)

---

## 1. THÔNG TIN ĐỊNH DANH TOOL

- **Tên Tool (Function Name)**: `[Tên hàm tiếng Anh chuẩn camelCase, ví dụ: queryInventoryStock]`
- **Mô tả chức năng (Description)**: `[Mô tả rõ ràng để LLM biết thời điểm nào nên gọi tool này]`
- **Cơ chế gọi (Invocation Type)**: `Read-Only Database Query / Write Action / External Service`

---

## 2. JSON SCHEMA KHAI BÁO CHO GEMINI API

```json
{
  "name": "queryInventoryStock",
  "description": "Tra cứu số lượng tồn kho thực tế và hạn sử dụng của sản phẩm dựa trên SKU hoặc ID.",
  "parameters": {
    "type": "object",
    "properties": {
      "productId": {
        "type": "integer",
        "description": "Mã ID duy nhất của sản phẩm trong hệ thống siêu thị"
      },
      "branchId": {
        "type": "integer",
        "description": "Mã định danh chi nhánh siêu thị (mặc định chi nhánh gần nhất)"
      }
    },
    "required": ["productId"]
  }
}
```

---

## 3. MẪU IMPLEMENTATION C# (BACKEND SERVICE)

```csharp
public async Task<ToolExecutionResult> ExecuteAsync(Dictionary<string, object> parameters)
{
    if (!parameters.TryGetValue("productId", out var idObj) || !int.TryParse(idObj.ToString(), out int productId))
    {
        return ToolExecutionResult.Failed("Mã sản phẩm không hợp lệ.");
    }

    var product = await _context.Products
        .AsNoTracking()
        .FirstOrDefaultAsync(p => p.Id == productId);

    if (product == null)
    {
        return ToolExecutionResult.NotFound($"Không tìm thấy sản phẩm có mã {productId}");
    }

    return ToolExecutionResult.Success(new
    {
        productId = product.Id,
        productName = product.Name,
        stockQuantity = product.StockQuantity,
        unitPrice = product.Price,
        isAvailable = product.StockQuantity > 0
    });
}
```

---

## 4. QUY TẮC BẢO MẬT & HIỆU NĂNG
1. Luôn dùng `.AsNoTracking()` cho các truy vấn Read-Only từ Tool Calling.
2. Có giới hạn phân trang `Take(20)` tránh LLM bị quá tải context token.
3. Không expose các trường nhạy cảm như giá vốn nhập (`CostPrice`) hoặc thông tin khách hàng cá nhân ra LLM trừ khi có role quản lý tương ứng.
