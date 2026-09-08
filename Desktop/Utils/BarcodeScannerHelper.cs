namespace SmartSupermarket.Desktop.Utils;

public static class BarcodeScannerHelper
{
    // Helper logic for handling barcode scanner inputs or camera feed
    public static bool IsValidBarcode(string code)
    {
        return !string.IsNullOrWhiteSpace(code) && code.Length >= 8;
    }
}
