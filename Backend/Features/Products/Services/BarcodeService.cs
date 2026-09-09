using System.Text;
using System.Text.RegularExpressions;
using SmartSupermarket.Backend.Domain.Enums;
using SmartSupermarket.Backend.Features.Products.DTOs;
using SmartSupermarket.Backend.Features.Products.Repositories;

namespace SmartSupermarket.Backend.Features.Products.Services;

public class BarcodeService : IBarcodeService
{
    private readonly IProductRepository _productRepository;

    public BarcodeService(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public string GenerateEan13(string? customPrefix = "200", int? sequence = null)
    {
        string prefix = string.IsNullOrWhiteSpace(customPrefix) ? "200" : customPrefix.Trim();
        if (prefix.Length > 3)
        {
            prefix = prefix[..3];
        }
        else
        {
            prefix = prefix.PadLeft(3, '0');
        }

        int seq = sequence ?? Random.Shared.Next(1, 999999999);
        string seqStr = (seq % 1000000000).ToString("D9");

        string first12 = prefix + seqStr;
        char checkDigit = CalculateEan13CheckDigit(first12);

        return first12 + checkDigit;
    }

    public async Task<string> GenerateEan13Async(string? customPrefix = "200", CancellationToken cancellationToken = default)
    {
        for (int attempt = 0; attempt < 50; attempt++)
        {
            string candidate = GenerateEan13(customPrefix);
            if (!await IsDuplicateBarcodeAsync(candidate, null, cancellationToken))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("Không thể tự động sinh mã vạch EAN-13 duy nhất sau 50 lần thử.");
    }

    public string GenerateQrCode(string barcode, string productName, decimal price)
    {
        if (string.IsNullOrWhiteSpace(barcode))
        {
            throw new ArgumentException("Mã vạch không được để trống khi tạo QR Code.");
        }

        // Standard QR Code payload format for POS / SuperMarket product
        var sb = new StringBuilder();
        sb.Append($"BARCODE:{barcode.Trim()};");
        sb.Append($"NAME:{productName.Trim()};");
        sb.Append($"PRICE:{price:F0}VND");

        return sb.ToString();
    }

    public bool IsValidEan13(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode) || barcode.Length != 13 || !barcode.All(char.IsDigit))
        {
            return false;
        }

        string first12 = barcode[..12];
        char expectedCheckDigit = CalculateEan13CheckDigit(first12);

        return barcode[12] == expectedCheckDigit;
    }

    public bool IsValidBarcodeFormat(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode))
        {
            return false;
        }

        string trimmed = barcode.Trim();
        if (trimmed.Length < 8 || trimmed.Length > 50)
        {
            return false;
        }

        // Dangerous characters check (<, >, ', ", %, ;, --)
        if (Regex.IsMatch(trimmed, @"[<>'"";%]|--"))
        {
            return false;
        }

        // Allowed characters: letters, digits, hyphen, underscore
        return Regex.IsMatch(trimmed, @"^[a-zA-Z0-9_\-]+$");
    }

    public async Task<bool> IsDuplicateBarcodeAsync(string barcode, int? excludeProductId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(barcode))
        {
            return false;
        }

        return await _productRepository.ExistsBarcodeAsync(barcode.Trim(), excludeProductId, cancellationToken);
    }

    public async Task ValidateBarcodeForCreateAsync(string barcode, CancellationToken cancellationToken = default)
    {
        if (!IsValidBarcodeFormat(barcode))
        {
            throw new ArgumentException($"Mã vạch '{barcode}' không đúng định dạng (độ dài 8-50 ký tự, không chứa ký tự đặc biệt nguy hiểm).");
        }

        if (await IsDuplicateBarcodeAsync(barcode, null, cancellationToken))
        {
            throw new InvalidOperationException($"Mã vạch '{barcode}' đã tồn tại trên hệ thống (BR-PROD-01).");
        }
    }

    public async Task ValidateBarcodeForUpdateAsync(string barcode, int productId, CancellationToken cancellationToken = default)
    {
        if (!IsValidBarcodeFormat(barcode))
        {
            throw new ArgumentException($"Mã vạch '{barcode}' không đúng định dạng.");
        }

        if (await IsDuplicateBarcodeAsync(barcode, productId, cancellationToken))
        {
            throw new InvalidOperationException($"Mã vạch '{barcode}' đã được sử dụng bởi sản phẩm khác (BR-PROD-01).");
        }
    }

    public async Task<ProductBarcodeDto?> LookupByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(barcode) || !IsValidBarcodeFormat(barcode))
        {
            return null;
        }

        var product = await _productRepository.GetByBarcodeAsync(barcode.Trim(), cancellationToken);
        if (product == null)
        {
            return null;
        }

        return new ProductBarcodeDto
        {
            ProductId = product.ProductId,
            ProductName = product.ProductName,
            Barcode = product.Barcode,
            Price = product.Price,
            Unit = product.Unit,
            Status = product.Status,
            CategoryName = product.Category?.CategoryName ?? string.Empty
        };
    }

    public static char CalculateEan13CheckDigit(string first12Digits)
    {
        if (first12Digits.Length != 12 || !first12Digits.All(char.IsDigit))
        {
            throw new ArgumentException("12 chữ số đầu tiên của EAN-13 phải chứa toàn số.");
        }

        int sum = 0;
        for (int i = 0; i < 12; i++)
        {
            int digit = first12Digits[i] - '0';
            sum += (i % 2 == 0) ? digit : digit * 3;
        }
        int checkDigit = (10 - (sum % 10)) % 10;
        return (char)('0' + checkDigit);
    }
}
