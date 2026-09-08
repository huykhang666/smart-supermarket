namespace SmartSupermarket.Backend.Common.Barcode;

public static class BarcodeGenerator
{
    public static string GenerateEAN13()
    {
        // TODO: Generate 13-digit Barcode string for new products
        return $"893{Random.Shared.NextInt64(100000000, 999999999)}";
    }
}
