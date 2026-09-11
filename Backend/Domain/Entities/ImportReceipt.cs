using SmartSupermarket.Backend.Domain.Enums;

namespace SmartSupermarket.Backend.Domain.Entities;

public class ImportReceipt
{
    public int ImportReceiptId { get; set; }
    public string ReceiptCode { get; set; } = string.Empty;
    public int SupplierId { get; set; }
    public int BranchId { get; set; }
    public int ImportedByUserId { get; set; }
    public DateTime ImportDate { get; set; } = DateTime.UtcNow;
    public DateTime? ConfirmedAt { get; set; }
    public int? ConfirmedByUserId { get; set; }
    public decimal TotalAmount { get; set; }
    public ImportStatus Status { get; set; } = ImportStatus.Draft;
    public string? Note { get; set; }

    // Navigation properties
    public Supplier Supplier { get; set; } = null!;
    public User ImportedByUser { get; set; } = null!;
    public User? ConfirmedByUser { get; set; }
    public ICollection<ImportDetail> ImportDetails { get; set; } = new List<ImportDetail>();
}
