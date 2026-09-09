using Moq;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Domain.Enums;
using SmartSupermarket.Backend.Features.Products.Repositories;
using SmartSupermarket.Backend.Features.Products.Services;
using Xunit;

namespace SmartSupermarket.Backend.Tests;

public class BarcodeServiceTests
{
    private readonly Mock<IProductRepository> _mockProductRepository;
    private readonly BarcodeService _barcodeService;

    public BarcodeServiceTests()
    {
        _mockProductRepository = new Mock<IProductRepository>();
        _barcodeService = new BarcodeService(_mockProductRepository.Object);
    }

    [Fact]
    public void GenerateEan13_ShouldReturnValid13DigitStringWithCorrectCheckDigit()
    {
        // Act
        string barcode = _barcodeService.GenerateEan13("200", 12345);

        // Assert
        Assert.NotNull(barcode);
        Assert.Equal(13, barcode.Length);
        Assert.StartsWith("200", barcode);
        Assert.True(_barcodeService.IsValidEan13(barcode));
    }

    [Theory]
    [InlineData("8935001800019", true)]  // Valid EAN-13
    [InlineData("8934673123456", false)] // Invalid check digit
    [InlineData("1234567890128", true)]  // Valid EAN-13
    [InlineData("12345", false)]         // Invalid length
    [InlineData("893500180001A", false)] // Contains letter
    public void IsValidEan13_TestCases_ShouldMatchExpectedResult(string barcode, bool expectedResult)
    {
        // Act
        bool result = _barcodeService.IsValidEan13(barcode);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Theory]
    [InlineData("8935001800012", true)]
    [InlineData("CODE128-PROD-01", true)]
    [InlineData("PROD_12345", true)]
    [InlineData("<script>alert(1)</script>", false)]
    [InlineData("SELECT * FROM Product", false)]
    [InlineData("SHORT", false)] // Length < 8
    public void IsValidBarcodeFormat_TestCases_ShouldMatchExpectedResult(string barcode, bool expectedResult)
    {
        // Act
        bool result = _barcodeService.IsValidBarcodeFormat(barcode);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public void GenerateQrCode_ShouldReturnFormattedPayload()
    {
        // Act
        string qr = _barcodeService.GenerateQrCode("8935001800012", "Coca-Cola 330ml", 10000);

        // Assert
        Assert.Contains("BARCODE:8935001800012", qr);
        Assert.Contains("NAME:Coca-Cola 330ml", qr);
        Assert.Contains("PRICE:10000VND", qr);
    }

    [Fact]
    public async Task ValidateBarcodeForCreateAsync_DuplicateBarcode_ShouldThrowInvalidOperationException()
    {
        // Arrange
        string barcode = "8935001800012";
        _mockProductRepository
            .Setup(r => r.ExistsBarcodeAsync(barcode, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _barcodeService.ValidateBarcodeForCreateAsync(barcode));
    }

    [Fact]
    public async Task LookupByBarcodeAsync_ExistingProduct_ShouldReturnProductBarcodeDto()
    {
        // Arrange
        string barcode = "8935001800012";
        var product = new Product
        {
            ProductId = 1,
            ProductName = "Coca-Cola 330ml",
            Barcode = barcode,
            Price = 10000,
            Unit = "lon",
            Status = ProductStatus.Active,
            Category = new Category { CategoryId = 1, CategoryName = "Nước giải khát" }
        };

        _mockProductRepository
            .Setup(r => r.GetByBarcodeAsync(barcode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        var result = await _barcodeService.LookupByBarcodeAsync(barcode);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.ProductId);
        Assert.Equal("Coca-Cola 330ml", result.ProductName);
        Assert.Equal(10000, result.Price);
        Assert.Equal("Nước giải khát", result.CategoryName);
    }
}
