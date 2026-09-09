namespace SmartSupermarket.Backend.Features.Suppliers.DTOs;

public class SupplierDto
{
    public int SupplierId { get; set; }
    public string SupplierCode { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? TaxCode { get; set; }
    public string? LogoUrl { get; set; }
    public byte Status { get; set; } = 1;
    public string StatusName => Status switch
    {
        1 => "Đang hợp tác",
        2 => "Tạm ngừng hợp tác",
        _ => "Khác"
    };
    public int ProductCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class SupplierDropdownDto
{
    public int SupplierId { get; set; }
    public string SupplierCode { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
}

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

public class ProductSupplierDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public decimal PurchasePrice { get; set; }
    public string? SupplierProductCode { get; set; }
    public int LeadTime { get; set; }
    public int MinimumOrderQuantity { get; set; }
    public decimal Rating { get; set; }
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
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

public class SupplierStatisticsDto
{
    public int TotalSuppliers { get; set; }
    public int ActiveSuppliers { get; set; }
    public int InactiveSuppliers { get; set; }
    public int TotalSuppliedProducts { get; set; }
    public decimal AverageRating { get; set; }
}

public class SupplierPagedResult
{
    public IEnumerable<SupplierDto> Items { get; set; } = new List<SupplierDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public class ImportSupplierResultDto
{
    public int TotalRows { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<string> Errors { get; set; } = new();
}
