namespace SmartSupermarket.Backend.Domain.Enums;

public enum StockChangeType : byte
{
    Import = 1,
    Sale = 2,
    Adjustment = 3,
    Expired = 4
}
