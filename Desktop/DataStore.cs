using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using SmartSupermarket.Backend.Features.Products.DTOs;

namespace Desktop;

public class SupplierItem
{
    public int SupplierId { get; set; }
    public string SupplierCode { get; set; } = "";
    public string SupplierName { get; set; } = "";
    public string ContactPerson { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public string Address { get; set; } = "";
}

public class PosOrderRecord
{
    public int OrderId { get; set; }
    public string OrderCode { get; set; } = "";
    public DateTime OrderDate { get; set; } = DateTime.Now;
    public string CustomerName { get; set; } = "Khách lẻ";
    public decimal SubTotal { get; set; }
    public decimal VatTotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public string PaymentMethod { get; set; } = "Tiền mặt";
    public List<PosOrderItem> Items { get; set; } = new();
}

public class PosOrderItem
{
    public int ProductId { get; set; }
    public string Barcode { get; set; } = "";
    public string ProductName { get; set; } = "";
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}

public static class DataStore
{
    private static readonly List<ProductDto> _products = new();
    private static readonly List<SupplierItem> _suppliers = new();
    private static readonly List<PosOrderRecord> _orders = new();

    private static readonly string ProductsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "local_products.json");
    private static readonly string SuppliersFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "local_suppliers.json");
    private static readonly string OrdersFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "local_orders.json");

    public static event EventHandler<ProductDto>? ProductAdded;
    public static event EventHandler<SupplierItem>? SupplierAdded;
    public static event EventHandler<PosOrderRecord>? OrderCompleted;

    public static IReadOnlyList<PosOrderRecord> Orders
    {
        get
        {
            lock (_orders)
            {
                return _orders.ToList().AsReadOnly();
            }
        }
    }

    public static decimal TodayRevenue
    {
        get
        {
            lock (_orders)
            {
                return _orders.Where(o => o.OrderDate.Date == DateTime.Today).Sum(o => o.GrandTotal);
            }
        }
    }

    public static int TodayOrdersCount
    {
        get
        {
            lock (_orders)
            {
                return _orders.Count(o => o.OrderDate.Date == DateTime.Today);
            }
        }
    }

    public static int TodayItemsSoldCount
    {
        get
        {
            lock (_orders)
            {
                return _orders.Where(o => o.OrderDate.Date == DateTime.Today).Sum(o => o.Items.Sum(i => i.Quantity));
            }
        }
    }

    public static IReadOnlyList<ProductDto> Products
    {
        get
        {
            lock (_products)
            {
                return _products.ToList().AsReadOnly();
            }
        }
    }

    public static IReadOnlyList<SupplierItem> Suppliers
    {
        get
        {
            lock (_suppliers)
            {
                return _suppliers.ToList().AsReadOnly();
            }
        }
    }

    static DataStore()
    {
        LoadDataFromDisk();
    }

    private static void LoadDataFromDisk()
    {
        try
        {
            if (File.Exists(ProductsFilePath))
            {
                string json = File.ReadAllText(ProductsFilePath);
                var items = JsonSerializer.Deserialize<List<ProductDto>>(json);
                if (items != null)
                {
                    lock (_products)
                    {
                        _products.Clear();
                        _products.AddRange(items);
                    }
                }
            }
        }
        catch { }

        try
        {
            if (File.Exists(SuppliersFilePath))
            {
                string json = File.ReadAllText(SuppliersFilePath);
                var items = JsonSerializer.Deserialize<List<SupplierItem>>(json);
                if (items != null)
                {
                    lock (_suppliers)
                    {
                        _suppliers.Clear();
                        _suppliers.AddRange(items);
                    }
                }
            }
        }
        catch { }

        try
        {
            if (File.Exists(OrdersFilePath))
            {
                string json = File.ReadAllText(OrdersFilePath);
                var items = JsonSerializer.Deserialize<List<PosOrderRecord>>(json);
                if (items != null)
                {
                    lock (_orders)
                    {
                        _orders.Clear();
                        _orders.AddRange(items);
                    }
                }
            }
        }
        catch { }
    }

    private static void SaveProductsToDisk()
    {
        try
        {
            List<ProductDto> copy;
            lock (_products)
            {
                copy = _products.ToList();
            }
            string json = JsonSerializer.Serialize(copy, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ProductsFilePath, json);
        }
        catch { }
    }

    private static void SaveSuppliersToDisk()
    {
        try
        {
            List<SupplierItem> copy;
            lock (_suppliers)
            {
                copy = _suppliers.ToList();
            }
            string json = JsonSerializer.Serialize(copy, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SuppliersFilePath, json);
        }
        catch { }
    }

    private static void SaveOrdersToDisk()
    {
        try
        {
            List<PosOrderRecord> copy;
            lock (_orders)
            {
                copy = _orders.ToList();
            }
            string json = JsonSerializer.Serialize(copy, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(OrdersFilePath, json);
        }
        catch { }
    }

    public static void AddOrder(PosOrderRecord order)
    {
        if (order == null) return;

        lock (_orders)
        {
            _orders.Insert(0, order);
        }

        SaveOrdersToDisk();
        OrderCompleted?.Invoke(null, order);
    }

    public static void AddProduct(ProductDto product)
    {
        if (product == null) return;
        
        lock (_products)
        {
            var existing = _products.FirstOrDefault(p => !string.IsNullOrEmpty(p.Barcode) && p.Barcode.Equals(product.Barcode, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                _products.Remove(existing);
            }
            _products.Insert(0, product);
        }

        SaveProductsToDisk();
        ProductAdded?.Invoke(null, product);
    }

    public static void AddSupplier(SupplierItem supplier)
    {
        if (supplier == null) return;

        lock (_suppliers)
        {
            var existing = _suppliers.FirstOrDefault(s => !string.IsNullOrEmpty(s.SupplierName) && s.SupplierName.Equals(supplier.SupplierName, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                _suppliers.Remove(existing);
            }
            _suppliers.Insert(0, supplier);
        }

        SaveSuppliersToDisk();
        SupplierAdded?.Invoke(null, supplier);
    }

    public static ProductDto? FindProductByBarcode(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode)) return null;
        lock (_products)
        {
            return _products.FirstOrDefault(p => !string.IsNullOrEmpty(p.Barcode) && p.Barcode.Equals(barcode.Trim(), StringComparison.OrdinalIgnoreCase));
        }
    }
}
