namespace SmartSupermarket.Backend.Features.Suppliers.DTOs.Requests;

public class CreateSupplierRequest
{
    public string? SupplierCode { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? TaxCode { get; set; }
    public string? LogoUrl { get; set; }
    public byte Status { get; set; } = 1;
}

public class UpdateSupplierRequest
{
    public string SupplierName { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? TaxCode { get; set; }
    public string? LogoUrl { get; set; }
    public byte Status { get; set; } = 1;
}

public class LinkProductSupplierRequest
{
    public int ProductId { get; set; }
    public int SupplierId { get; set; }
    public decimal PurchasePrice { get; set; }
    public string? SupplierProductCode { get; set; }
    public int LeadTime { get; set; } = 1;
    public int MinimumOrderQuantity { get; set; } = 1;
    public decimal Rating { get; set; } = 5.00m;
    public bool IsDefault { get; set; } = false;
}

public class UpdateLinkRequest
{
    public decimal PurchasePrice { get; set; }
    public string? SupplierProductCode { get; set; }
    public int LeadTime { get; set; } = 1;
    public int MinimumOrderQuantity { get; set; } = 1;
    public decimal Rating { get; set; } = 5.00m;
    public bool IsDefault { get; set; } = false;
}
