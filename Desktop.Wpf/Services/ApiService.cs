using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using SmartSupermarket.POS.Models;

namespace SmartSupermarket.POS.Services;

public class ApiService
{
    private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(8) };
    public string BaseUrl { get; set; } = "http://localhost:5137";

    public ApiService()
    {
        EnsureBackendRunning();
    }

    /// <summary>
    /// Đảm bảo Backend API trên cổng 5137 luôn chạy ngầm
    /// </summary>
    public static void EnsureBackendRunning()
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromMilliseconds(400) };
            var res = client.GetAsync("http://localhost:5137/swagger/index.html").GetAwaiter().GetResult();
            if (res.IsSuccessStatusCode) return;
        }
        catch
        {
            // Backend chưa chạy, tự động khởi động Backend ngầm
            try
            {
                var candidatePaths = new[]
                {
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\Backend\bin\Debug\net10.0\SmartSupermarket.Backend.exe"),
                    Path.Combine(Directory.GetCurrentDirectory(), @"Backend\bin\Debug\net10.0\SmartSupermarket.Backend.exe"),
                    @"C:\Users\MINH TRIEN\OneDrive\Desktop\smart-supermarket\Backend\bin\Debug\net10.0\SmartSupermarket.Backend.exe"
                };

                foreach (var path in candidatePaths)
                {
                    var fullPath = Path.GetFullPath(path);
                    if (File.Exists(fullPath))
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = fullPath,
                            WorkingDirectory = Path.GetDirectoryName(fullPath),
                            CreateNoWindow = true,
                            UseShellExecute = false
                        };
                        Process.Start(psi);
                        break;
                    }
                }
            }
            catch { }
        }
    }

    /// <summary>
    /// Lấy danh sách sản phẩm THỰC TẾ từ Database Backend qua REST API
    /// </summary>
    public async Task<List<ProductItem>> GetProductsAsync()
    {
        var list = new List<ProductItem>();

        // 1. Thử lấy từ endpoint phân trang CSDL
        try
        {
            var res = await _httpClient.GetAsync($"{BaseUrl}/api/products?page=1&pageSize=300");
            if (res.IsSuccessStatusCode)
            {
                var content = await res.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;
                if (root.TryGetProperty("data", out var data) && data.TryGetProperty("items", out var items))
                {
                    if (items.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var el in items.EnumerateArray())
                        {
                            list.Add(ParseProductItem(el));
                        }
                    }
                }
            }
        }
        catch { }

        // 2. Nếu đã lấy được danh sách từ API CSDL, trả về ngay
        if (list.Count > 0)
        {
            return list;
        }

        // 3. Fallback sang endpoint lấy toàn bộ catalog mẫu thực tế 200 sản phẩm từ CSDL
        try
        {
            var res = await _httpClient.GetAsync($"{BaseUrl}/api/products/sample");
            if (res.IsSuccessStatusCode)
            {
                var content = await res.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;
                if (root.TryGetProperty("data", out var items) && items.ValueKind == JsonValueKind.Array)
                {
                    foreach (var el in items.EnumerateArray())
                    {
                        list.Add(ParseProductItem(el));
                    }
                }
            }
        }
        catch { }

        // 4. Nếu kết nối mạng backend tạm thời gián đoạn, đọc trực tiếp file CSDL products_seed.json
        if (list.Count == 0)
        {
            list = LoadDirectFromSeedJson();
        }

        return list;
    }

    private ProductItem ParseProductItem(JsonElement el)
    {
        int id = el.TryGetProperty("productId", out var pid) ? pid.GetInt32()
               : el.TryGetProperty("id", out var idProp) ? idProp.GetInt32() : 0;

        string name = el.TryGetProperty("productName", out var pn) ? pn.GetString() ?? "" : "";
        string barcode = el.TryGetProperty("barcode", out var bc) ? bc.GetString() ?? "" : "";
        string category = el.TryGetProperty("categoryName", out var cn) ? cn.GetString() ?? "Khác" : "Khác";
        decimal price = el.TryGetProperty("price", out var pr) ? pr.GetDecimal() : 0;
        decimal? costPrice = el.TryGetProperty("costPrice", out var cp) && cp.ValueKind != JsonValueKind.Null ? cp.GetDecimal() : null;
        string image = el.TryGetProperty("imageUrl", out var iu) ? iu.GetString() ?? "" : "";
        string unit = el.TryGetProperty("unit", out var un) ? un.GetString() ?? "Cái" : "Cái";
        int stock = el.TryGetProperty("stock", out var st) && st.ValueKind != JsonValueKind.Null ? st.GetInt32() : 50;
        string status = el.TryGetProperty("statusName", out var sn) ? sn.GetString() ?? "Đang bán" : "Đang bán";
        bool isNearExpiry = el.TryGetProperty("isNearExpiry", out var ine) && ine.GetBoolean();

        return new ProductItem
        {
            ProductId = id,
            ProductName = name,
            Barcode = barcode,
            CategoryName = category,
            Price = price,
            CostPrice = costPrice,
            ImageUrl = image,
            Unit = unit,
            Stock = stock,
            Status = status,
            IsNearExpiry = isNearExpiry
        };
    }

    private List<ProductItem> LoadDirectFromSeedJson()
    {
        var list = new List<ProductItem>();
        try
        {
            var candidatePaths = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\Backend\Data\products_seed.json"),
                Path.Combine(Directory.GetCurrentDirectory(), @"Backend\Data\products_seed.json"),
                @"C:\Users\MINH TRIEN\OneDrive\Desktop\smart-supermarket\Backend\Data\products_seed.json"
            };

            foreach (var path in candidatePaths)
            {
                var fullPath = Path.GetFullPath(path);
                if (File.Exists(fullPath))
                {
                    var json = File.ReadAllText(fullPath);
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var el in doc.RootElement.EnumerateArray())
                        {
                            list.Add(ParseProductItem(el));
                        }
                    }
                    break;
                }
            }
        }
        catch { }

        return list;
    }

    /// <summary>
    /// Lấy danh sách đơn hàng THỰC TẾ từ Database Backend qua REST API
    /// </summary>
    public async Task<List<OrderItem>> GetOrdersAsync()
    {
        var list = new List<OrderItem>();
        try
        {
            var res = await _httpClient.GetAsync($"{BaseUrl}/api/v1/orders?page=1&pageSize=100");
            if (res.IsSuccessStatusCode)
            {
                var content = await res.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;
                if (root.TryGetProperty("data", out var data) && data.TryGetProperty("items", out var items))
                {
                    if (items.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var el in items.EnumerateArray())
                        {
                            int id = el.GetProperty("orderId").GetInt32();
                            string cust = el.TryGetProperty("customerName", out var cn) && cn.ValueKind != JsonValueKind.Null ? cn.GetString() ?? "Khách Lẻ" : "Khách Lẻ";
                            string emp = el.TryGetProperty("employeeName", out var en) && en.ValueKind != JsonValueKind.Null ? en.GetString() ?? "Thu Ngân" : "Thu Ngân";
                            decimal total = el.TryGetProperty("totalAmount", out var ta) ? ta.GetDecimal() : 0;
                            decimal discount = el.TryGetProperty("discountAmount", out var da) ? da.GetDecimal() : 0;
                            decimal final_ = el.TryGetProperty("finalAmount", out var fa) ? fa.GetDecimal() : total;
                            int payInt = el.TryGetProperty("paymentMethod", out var pm) ? pm.GetInt32() : 1;
                            string payStr = payInt == 2 ? "VietQR" : payInt == 3 ? "Thẻ POS" : "Tiền mặt";
                            string dateStr = el.TryGetProperty("orderDate", out var od) ? od.GetString() ?? "" : "";
                            DateTime oDate = DateTime.TryParse(dateStr, out var dt) ? dt.ToLocalTime() : DateTime.Now;

                            var orderItem = new OrderItem
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
                            };

                            // Map real line items of this order
                            if (el.TryGetProperty("orderDetails", out var details) && details.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var d in details.EnumerateArray())
                                {
                                    orderItem.Items.Add(new CartItem
                                    {
                                        ProductId = d.TryGetProperty("productId", out var pId) ? pId.GetInt32() : 0,
                                        ProductName = d.TryGetProperty("productName", out var pName) ? pName.GetString() ?? "" : "Sản phẩm",
                                        Barcode = d.TryGetProperty("barcode", out var bCode) ? bCode.GetString() ?? "" : "",
                                        UnitPrice = d.TryGetProperty("unitPrice", out var uPrice) ? uPrice.GetDecimal() : 0,
                                        Quantity = d.TryGetProperty("quantity", out var qty) ? qty.GetInt32() : 1
                                    });
                                }
                            }

                            list.Add(orderItem);
                        }
                    }
                }
            }
        }
        catch { }

        return list;
    }

    /// <summary>
    /// Gửi tạo đơn hàng THỰC TẾ vào Database Backend
    /// </summary>
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
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            var res = await _httpClient.PostAsync($"{BaseUrl}/api/v1/orders", content);
            return res.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Tra cứu khách hàng thành viên THỰC TẾ qua số điện thoại từ Database
    /// </summary>
    public async Task<CustomerItem?> LookupCustomerAsync(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return null;

        try
        {
            var res = await _httpClient.GetAsync($"{BaseUrl}/api/customers/lookup?phone={Uri.EscapeDataString(phone.Trim())}");
            if (res.IsSuccessStatusCode)
            {
                var content = await res.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;
                if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object)
                {
                    return new CustomerItem
                    {
                        CustomerId = data.TryGetProperty("customerId", out var cid) ? cid.GetInt32() : 0,
                        FullName = data.TryGetProperty("fullName", out var fn) ? fn.GetString() ?? "" : "",
                        PhoneNumber = data.TryGetProperty("phoneNumber", out var pn) ? pn.GetString() ?? phone : phone,
                        LoyaltyPoints = data.TryGetProperty("points", out var pt) ? pt.GetInt32() : 0,
                        Tier = data.TryGetProperty("membershipLevel", out var ml) ? ml.GetString() ?? "Thành viên" : "Thành viên"
                    };
                }
            }
        }
        catch { }

        return null;
    }

    /// <summary>
    /// Lấy báo cáo thống kê doanh thu thực tế từ Backend CSDL
    /// </summary>
    public async Task<(string TodayRevenue, string TodayOrders, string TotalCustomers, string WarningCount, string AiAnalysis)?> GetDashboardSummaryAsync()
    {
        try
        {
            var res = await _httpClient.GetAsync($"{BaseUrl}/api/dashboard/summary");
            if (res.IsSuccessStatusCode)
            {
                var content = await res.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;
                if (root.TryGetProperty("data", out var data))
                {
                    string rev = data.TryGetProperty("todayRevenue", out var r) ? r.GetString() ?? "0 đ" : "0 đ";
                    string ord = data.TryGetProperty("todayOrders", out var o) ? o.GetString() ?? "0 đơn" : "0 đơn";
                    string cus = data.TryGetProperty("totalCustomers", out var c) ? c.GetString() ?? "0" : "0";
                    string warn = data.TryGetProperty("warningCount", out var w) ? w.GetString() ?? "0" : "0";
                    string ai = data.TryGetProperty("aiAnalysis", out var a) ? a.GetString() ?? "" : "";
                    return (rev, ord, cus, warn, ai);
                }
            }
        }
        catch { }

        return null;
    }
}
