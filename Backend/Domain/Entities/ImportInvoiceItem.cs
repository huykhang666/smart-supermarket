namespace SmartSupermarket.Backend.Domain.Entities;

public class ImportInvoiceItem
{
    public int ImportInvoiceItemId { get; set; }
    public int ImportInvoiceId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal CostPrice { get; set; }
    public DateTime ExpiryDate { get; set; }

    public ImportInvoice ImportInvoice { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
