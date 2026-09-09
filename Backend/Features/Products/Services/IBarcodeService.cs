using SmartSupermarket.Backend.Features.Products.DTOs;

namespace SmartSupermarket.Backend.Features.Products.Services;

public interface IBarcodeService
{
    // Barcode Generator
    string GenerateEan13(string? customPrefix = "200", int? sequence = null);
    Task<string> GenerateEan13Async(string? customPrefix = "200", CancellationToken cancellationToken = default);
    string GenerateQrCode(string barcode, string productName, decimal price);

    // Barcode Validation
    bool IsValidEan13(string barcode);
    bool IsValidBarcodeFormat(string barcode);

    // Duplicate Validation
    Task<bool> IsDuplicateBarcodeAsync(string barcode, int? excludeProductId = null, CancellationToken cancellationToken = default);
    Task ValidateBarcodeForCreateAsync(string barcode, CancellationToken cancellationToken = default);
    Task ValidateBarcodeForUpdateAsync(string barcode, int productId, CancellationToken cancellationToken = default);

    // Lookup By Barcode
    Task<ProductBarcodeDto?> LookupByBarcodeAsync(string barcode, CancellationToken cancellationToken = default);
}
