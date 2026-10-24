using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using SmartSupermarket.POS.Models;

namespace SmartSupermarket.POS.Services;

public class ApiService
{
    private readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(10) };
    public string BaseUrl { get; set; } = "http://localhost:5137";

    public async Task<List<ProductItem>> GetProductsAsync()
    {
        try
        {
            var res = await _httpClient.GetAsync($"{BaseUrl}/api/v1/products?page=1&pageSize=100");
            if (res.IsSuccessStatusCode)
            {
                var content = await res.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;
                if (root.TryGetProperty("data", out var data))
                {
                    var itemsProp = data.TryGetProperty("items", out var items) ? items : data;
                    if (itemsProp.ValueKind == JsonValueKind.Array)
                    {
                        var list = new List<ProductItem>();
                        foreach (var el in itemsProp.EnumerateArray())
                        {
                            list.Add(new ProductItem
                            {
                                ProductId = el.GetProperty("productId").GetInt32(),
                                ProductName = el.TryGetProperty("productName", out var pn) ? pn.GetString() ?? "" : "",
                                Barcode = el.TryGetProperty("barcode", out var bc) ? bc.GetString() ?? "" : "",
                                CategoryName = el.TryGetProperty("categoryName", out var cn) ? cn.GetString() ?? "Khác" : "Khác",
                                Price = el.GetProperty("price").GetDecimal(),
                                CostPrice = el.TryGetProperty("costPrice", out var cp) && cp.ValueKind != JsonValueKind.Null ? cp.GetDecimal() : null,
                                ImageUrl = el.TryGetProperty("imageUrl", out var iu) ? iu.GetString() ?? "" : "",
                                Unit = el.TryGetProperty("unit", out var un) ? un.GetString() ?? "Cái" : "Cái",
                                Stock = el.TryGetProperty("stock", out var st) ? st.GetInt32() : 25,
                                Status = "Đang bán"
                            });
                        }
                        return list;
                    }
                }
            }
        }
        catch { }

        return GetMockProducts();
    }

    public async Task<List<OrderItem>> GetOrdersAsync()
    {
        try
        {
            var res = await _httpClient.GetAsync($"{BaseUrl}/api/v1/orders?page=1&pageSize=50");
            if (res.IsSuccessStatusCode)
            {
                var content = await res.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;
                if (root.TryGetProperty("data", out var data))
                {
                    var itemsProp = data.TryGetProperty("items", out var items) ? items : data;
                    if (itemsProp.ValueKind == JsonValueKind.Array)
                    {
                        var list = new List<OrderItem>();
                        foreach (var el in itemsProp.EnumerateArray())
                        {
                            int id = el.GetProperty("orderId").GetInt32();
                            string cust = el.TryGetProperty("customerName", out var cn) && cn.ValueKind != JsonValueKind.Null ? cn.GetString() ?? "Khách Lẻ" : "Khách Lẻ";
                            string emp = el.TryGetProperty("employeeName", out var en) && en.ValueKind != JsonValueKind.Null ? en.GetString() ?? "Thu Ngân" : "Thu Ngân";
                            decimal total = el.GetProperty("totalAmount").GetDecimal();
                            decimal discount = el.TryGetProperty("discountAmount", out var da) ? da.GetDecimal() : 0;
                            decimal final_ = el.GetProperty("finalAmount").GetDecimal();
                            int payInt = el.TryGetProperty("paymentMethod", out var pm) ? pm.GetInt32() : 1;
                            string payStr = payInt == 2 ? "VietQR" : payInt == 3 ? "Thẻ POS" : "Tiền mặt";
                            string dateStr = el.TryGetProperty("orderDate", out var od) ? od.GetString() ?? "" : "";
                            DateTime oDate = DateTime.TryParse(dateStr, out var dt) ? dt.ToLocalTime() : DateTime.Now;

                            list.Add(new OrderItem
                            {
                                OrderId = id,
                                CustomerName = cust,
                                EmployeeName = emp,
                                TotalAmount = total,
                                DiscountAmount = discount,
                                FinalAmount = final_,
                                PaymentMethod = payStr,
                                OrderDate = oDate,
                                Status = "Hoàn thành"
                            });
                        }
                        return list;
                    }
                }
            }
        }
        catch { }

        return GetMockOrders();
    }

    public async Task<bool> CreateOrderAsync(int employeeId, int? customerId, decimal totalAmount, decimal finalAmount, int paymentMethod, List<CartItem> items)
    {
        try
        {
            var req = new
            {
                employeeId = employeeId > 0 ? employeeId : 1,
                customerId = customerId,
                branchId = 1,
                paymentMethod = paymentMethod,
                payments = new[]
                {
                    new { paymentMethod = paymentMethod, amountPaid = finalAmount }
                },
                items = items.Select(i => new
                {
                    productId = i.ProductId,
                    quantity = i.Quantity,
                    unitPrice = i.UnitPrice
                }).ToArray()
            };

            var json = JsonSerializer.Serialize(req);
            using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var res = await _httpClient.PostAsync($"{BaseUrl}/api/v1/orders", content);
            return res.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private List<ProductItem> GetMockProducts()
    {
        return new List<ProductItem>
        {
            new() { ProductId = 1, ProductName = "Sữa Tươi Tiệt Trùng Vinamilk 100% 1L", Barcode = "893467312301", CategoryName = "Sữa & Sản phẩm từ sữa", Price = 38000, Unit = "Hộp", Stock = 45, ImageUrl = "https://cdn.tgdd.vn/Products/Images/2386/75470/bhx/sua-tuoi-tiet-trung-khong-duong-vinamilk-100-sua-tuoi-hop-1-lit-202303031024317189.jpg" },
            new() { ProductId = 2, ProductName = "Nước Ngọt Coca Cola Chai 1.5L", Barcode = "893467312302", CategoryName = "Nước giải khát & Đồ uống", Price = 22000, Unit = "Chai", Stock = 80, ImageUrl = "https://cdn.tgdd.vn/Products/Images/2443/76451/bhx/nuoc-ngot-coca-cola-15-lit-202302211516087568.jpg" },
            new() { ProductId = 3, ProductName = "Bánh Quy Bơ Danisa Hộp Thiếc 454g", Barcode = "893467312303", CategoryName = "Bánh kẹo & Ăn vặt", Price = 135000, Unit = "Hộp", Stock = 18, ImageUrl = "https://cdn.tgdd.vn/Products/Images/3364/79313/bhx/banh-quy-bo-danisa-hop-454g-202301051515286591.jpg" },
            new() { ProductId = 4, ProductName = "Mì Tôm Hảo Hảo Chua Cay Gói 75g", Barcode = "893467312304", CategoryName = "Đồ ăn liền & Đóng hộp", Price = 4500, Unit = "Gói", Stock = 150, ImageUrl = "https://cdn.tgdd.vn/Products/Images/2565/78648/bhx/mi-hao-hao-tom-chua-cay-75g-202301051016556534.jpg" },
            new() { ProductId = 5, ProductName = "Dầu Ăn Neptune Light Chai 1L", Barcode = "893467312305", CategoryName = "Gia vị & Đồ khô", Price = 58000, Unit = "Chai", Stock = 32, ImageUrl = "https://cdn.tgdd.vn/Products/Images/2282/81575/bhx/dau-an-neptune-light-chai-1-lit-202303021415053421.jpg" },
            new() { ProductId = 6, ProductName = "Gạo Thơm ST25 Đặc Sản Sóc Trăng 5kg", Barcode = "893467312306", CategoryName = "Gia vị & Đồ khô", Price = 185000, Unit = "Túi", Stock = 20, ImageUrl = "https://cdn.tgdd.vn/Products/Images/2513/218967/bhx/gao-st25-ong-cua-tui-5kg-202301051516223451.jpg" },
            new() { ProductId = 7, ProductName = "Nước Mắm Nam Ngư Đệ Nhị 900ml", Barcode = "893467312307", CategoryName = "Gia vị & Đồ khô", Price = 32000, Unit = "Chai", Stock = 40, ImageUrl = "https://cdn.tgdd.vn/Products/Images/2286/77890/bhx/nuoc-mam-nam-ngu-de-nhi-chai-900ml-202301051015112234.jpg" },
            new() { ProductId = 8, ProductName = "Nước Giặt OMO Matic Cửa Trên Túi 3.6kg", Barcode = "893467312308", CategoryName = "Hóa phẩm & Giặt xả", Price = 175000, Unit = "Túi", Stock = 15, ImageUrl = "https://cdn.tgdd.vn/Products/Images/2445/142999/bhx/nuoc-giat-omo-matic-chuyen-gia-cua-tren-tui-36kg-202301051015112234.jpg" }
        };
    }

    private List<OrderItem> GetMockOrders()
    {
        return new List<OrderItem>
        {
            new() { OrderId = 1001, CustomerName = "Nguyễn Văn Hùng", EmployeeName = "Thu Ngân 01", TotalAmount = 245000, FinalAmount = 245000, PaymentMethod = "VietQR", OrderDate = DateTime.Now.AddHours(-2), Status = "Hoàn thành" },
            new() { OrderId = 1002, CustomerName = "Trần Thị Mai", EmployeeName = "Thu Ngân 01", TotalAmount = 98000, FinalAmount = 98000, PaymentMethod = "Tiền mặt", OrderDate = DateTime.Now.AddHours(-1), Status = "Hoàn thành" },
            new() { OrderId = 1003, CustomerName = "Lê Hoàng Long", EmployeeName = "Thu Ngân 02", TotalAmount = 450000, DiscountAmount = 50000, FinalAmount = 400000, PaymentMethod = "Thẻ POS", OrderDate = DateTime.Now.AddMinutes(-30), Status = "Hoàn thành" },
            new() { OrderId = 1004, CustomerName = "Khách Lẻ", EmployeeName = "Thu Ngân 01", TotalAmount = 62500, FinalAmount = 62500, PaymentMethod = "VietQR", OrderDate = DateTime.Now.AddMinutes(-10), Status = "Hoàn thành" }
        };
    }
}
