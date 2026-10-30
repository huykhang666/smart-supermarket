using Moq;
using Microsoft.AspNetCore.Hosting;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Domain.Enums;
using SmartSupermarket.Backend.Features.Categories.Repositories;
using SmartSupermarket.Backend.Features.Products.DTOs;
using SmartSupermarket.Backend.Features.Products.Repositories;
using SmartSupermarket.Backend.Features.Products.Services;
using SmartSupermarket.Backend.Features.Suppliers.Repositories;
using Xunit;

namespace SmartSupermarket.Backend.Tests;

public class ProductServiceTests
{
    private readonly Mock<IProductRepository> _mockProductRepository;
    private readonly Mock<ICategoryRepository> _mockCategoryRepository;
    private readonly Mock<ISupplierRepository> _mockSupplierRepository;
    private readonly Mock<IWebHostEnvironment> _mockEnvironment;
    private readonly ProductService _productService;

    public ProductServiceTests()
    {
        _mockProductRepository = new Mock<IProductRepository>();
        _mockCategoryRepository = new Mock<ICategoryRepository>();
        _mockSupplierRepository = new Mock<ISupplierRepository>();
        _mockEnvironment = new Mock<IWebHostEnvironment>();

        _productService = new ProductService(
            _mockProductRepository.Object,
            _mockCategoryRepository.Object,
            _mockSupplierRepository.Object,
            _mockEnvironment.Object
        );
    }

    #region CreateProductAsync Tests

    [Fact]
    public async Task CreateProductAsync_ValidRequest_ShouldCreateProductSuccessfully()
    {
        // Arrange
        var request = new CreateProductRequest
        {
            ProductName = "Sữa tươi Vinamilk 1L",
            Barcode = "8934567890123",
            CategoryId = 1,
            SupplierId = 1,
            Price = 35000,
            CostPrice = 28000,
            Unit = "hộp"
        };

        var category = new Category { CategoryId = 1, CategoryName = "Sữa & Chế phẩm" };
        var supplier = new Supplier { SupplierId = 1, SupplierName = "Vinamilk" };

        _mockCategoryRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);
        _mockSupplierRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(supplier);
        _mockProductRepository.Setup(r => r.ExistsBarcodeAsync("8934567890123", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _productService.CreateProductAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Sữa tươi Vinamilk 1L", result.ProductName);
        Assert.Equal("8934567890123", result.Barcode);
        Assert.Equal(35000, result.Price);
        Assert.Equal("hộp", result.Unit);
        _mockProductRepository.Verify(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockProductRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateProductAsync_DuplicateBarcode_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var request = new CreateProductRequest
        {
            ProductName = "Bánh Mì",
            Barcode = "8934567890123",
            CategoryId = 1,
            Price = 10000,
            Unit = "cái"
        };

        _mockCategoryRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Category { CategoryId = 1 });
        _mockProductRepository.Setup(r => r.ExistsBarcodeAsync("8934567890123", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _productService.CreateProductAsync(request));
        Assert.Contains("BR-PROD-01", ex.Message);
    }

    [Fact]
    public async Task CreateProductAsync_NonExistingCategory_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var request = new CreateProductRequest
        {
            ProductName = "Sản phẩm test",
            CategoryId = 999,
            Price = 10000,
            Unit = "gói"
        };

        _mockCategoryRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _productService.CreateProductAsync(request));
    }

    [Fact]
    public async Task CreateProductAsync_NegativePrice_ShouldThrowArgumentException()
    {
        // Arrange
        var request = new CreateProductRequest
        {
            ProductName = "Sản phẩm test",
            CategoryId = 1,
            Price = -5000,
            Unit = "gói"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _productService.CreateProductAsync(request));
        Assert.Contains("BR-PROD-02", ex.Message);
    }

    #endregion

    #region UpdateProductAsync Tests

    [Fact]
    public async Task UpdateProductAsync_ValidRequest_ShouldUpdateSuccessfully()
    {
        // Arrange
        var existingProduct = new Product
        {
            ProductId = 1,
            ProductName = "Tên cũ",
            Barcode = "8934567890123",
            CategoryId = 1,
            Price = 10000,
            Unit = "cái",
            Status = ProductStatus.Active
        };

        var request = new UpdateProductRequest
        {
            ProductName = "Tên mới",
            Barcode = "8934567890123",
            CategoryId = 1,
            Price = 15000,
            Unit = "cái"
        };

        _mockProductRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);
        _mockCategoryRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Category { CategoryId = 1 });
        _mockProductRepository.Setup(r => r.ExistsBarcodeAsync("8934567890123", 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _productService.UpdateProductAsync(1, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Tên mới", result.ProductName);
        Assert.Equal(15000, result.Price);
    }

    [Fact]
    public async Task UpdateProductAsync_NonExistingProduct_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        _mockProductRepository.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var request = new UpdateProductRequest { ProductName = "Test", CategoryId = 1, Price = 100, Unit = "cái", Barcode = "1234567890" };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _productService.UpdateProductAsync(99, request));
    }

    #endregion

    #region DeleteProductAsync Tests

    [Fact]
    public async Task DeleteProductAsync_ExistingProduct_ShouldSoftDelete()
    {
        // Arrange
        var existingProduct = new Product { ProductId = 1, Status = ProductStatus.Active };
        _mockProductRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        // Act
        var result = await _productService.DeleteProductAsync(1);

        // Assert
        Assert.True(result);
        Assert.Equal(ProductStatus.Inactive, existingProduct.Status);
        _mockProductRepository.Verify(r => r.Update(existingProduct), Times.Once);
        _mockProductRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteProductAsync_NonExistingProduct_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        _mockProductRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _productService.DeleteProductAsync(999));
    }

    #endregion

    #region Price & History Tests

    [Fact]
    public async Task UpdateProductPriceAsync_ValidPrices_ShouldUpdatePricesCorrectly()
    {
        // Arrange
        var existingProduct = new Product { ProductId = 1, Price = 10000, CostPrice = 7000 };
        var request = new UpdatePriceRequest { Price = 12000, CostPrice = 8000 };

        _mockProductRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        // Act
        var result = await _productService.UpdateProductPriceAsync(1, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(12000, result.Price);
        Assert.Equal(8000, result.CostPrice);
    }

    [Fact]
    public async Task GetProductPriceHistoryAsync_ExistingProduct_ShouldCalculateMarginAndWarning()
    {
        // Arrange
        var product = new Product
        {
            ProductId = 1,
            ProductName = "Sản phẩm Bán Lỗ",
            Price = 8000,
            CostPrice = 10000
        };

        _mockProductRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        var result = await _productService.GetProductPriceHistoryAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(-2000, result.GrossProfit);
        Assert.True(result.IsNegativeMarginWarning);
    }

    #endregion
}
