namespace SmartSupermarket.Backend.Domain.Entities;

public class ImportInvoice
{
    public int ImportInvoiceId { get; set; }
    public string InvoiceCode { get; set; } = string.Empty;
    public int SupplierId { get; set; }
    public DateTime ImportDate { get; set; } = DateTime.UtcNow;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "Completed";
    public string CreatedBy { get; set; } = "Admin";

    public Supplier Supplier { get; set; } = null!;
    public ICollection<ImportInvoiceItem> Items { get; set; } = new List<ImportInvoiceItem>();
}
