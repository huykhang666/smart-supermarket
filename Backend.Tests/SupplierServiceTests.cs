using System.Text;
using Moq;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Features.Suppliers.DTOs;
using SmartSupermarket.Backend.Features.Suppliers.Repositories;
using SmartSupermarket.Backend.Features.Suppliers.Services;
using Xunit;

namespace SmartSupermarket.Backend.Tests;

public class SupplierServiceTests
{
    private readonly Mock<ISupplierRepository> _mockSupplierRepository;
    private readonly Mock<IProductSupplierRepository> _mockProductSupplierRepository;
    private readonly SupplierService _supplierService;

    public SupplierServiceTests()
    {
        _mockSupplierRepository = new Mock<ISupplierRepository>();
        _mockProductSupplierRepository = new Mock<IProductSupplierRepository>();
        _supplierService = new SupplierService(_mockSupplierRepository.Object, _mockProductSupplierRepository.Object);
    }

    #region 1. GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsSupplierDto()
    {
        // Arrange
        var supplier = new Supplier
        {
            SupplierId = 1,
            SupplierCode = "SUP00001",
            SupplierName = "Công ty Vinamilk",
            ContactPerson = "Nguyễn Văn A",
            PhoneNumber = "0901234567",
            Email = "vinamilk@supplier.com",
            Status = 1
        };
        _mockSupplierRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(supplier);

        // Act
        var result = await _supplierService.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.SupplierId);
        Assert.Equal("SUP00001", result.SupplierCode);
        Assert.Equal("Công ty Vinamilk", result.SupplierName);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        _mockSupplierRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Supplier?)null);

        // Act
        var result = await _supplierService.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region 2. GetByCodeAsync Tests

    [Fact]
    public async Task GetByCodeAsync_ExistingCode_ReturnsSupplierDto()
    {
        // Arrange
        var supplier = new Supplier
        {
            SupplierId = 2,
            SupplierCode = "SUP00002",
            SupplierName = "Công ty TH True Milk",
            Status = 1
        };
        _mockSupplierRepository.Setup(r => r.GetByCodeAsync("SUP00002", It.IsAny<CancellationToken>()))
            .ReturnsAsync(supplier);

        // Act
        var result = await _supplierService.GetByCodeAsync("SUP00002");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.SupplierId);
        Assert.Equal("SUP00002", result.SupplierCode);
    }

    [Fact]
    public async Task GetByCodeAsync_NonExistingCode_ReturnsNull()
    {
        // Arrange
        _mockSupplierRepository.Setup(r => r.GetByCodeAsync("INVALID", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Supplier?)null);

        // Act
        var result = await _supplierService.GetByCodeAsync("INVALID");

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region 3. GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_Default_ReturnsActiveSuppliers()
    {
        // Arrange
        var suppliers = new List<Supplier>
        {
            new() { SupplierId = 1, SupplierName = "Nha Cung Cap A", Status = 1 },
            new() { SupplierId = 2, SupplierName = "Nha Cung Cap B", Status = 1 }
        };
        _mockSupplierRepository.Setup(r => r.GetAllAsync(false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(suppliers);

        // Act
        var result = await _supplierService.GetAllAsync(false);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetAllAsync_IncludeInactive_ReturnsAllSuppliers()
    {
        // Arrange
        var suppliers = new List<Supplier>
        {
            new() { SupplierId = 1, SupplierName = "Nha Cung Cap A", Status = 1 },
            new() { SupplierId = 2, SupplierName = "Nha Cung Cap B", Status = 2 }
        };
        _mockSupplierRepository.Setup(r => r.GetAllAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(suppliers);

        // Act
        var result = await _supplierService.GetAllAsync(true);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
    }

    #endregion

    #region 4. GetPagedAsync Tests

    [Fact]
    public async Task GetPagedAsync_ValidParameters_ReturnsPagedResult()
    {
        // Arrange
        var items = new List<Supplier>
        {
            new() { SupplierId = 1, SupplierName = "Nha Cung Cap A", Status = 1 }
        };
        _mockSupplierRepository.Setup(r => r.GetPagedAsync("A", 1, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((items, 1));

        // Act
        var result = await _supplierService.GetPagedAsync("A", 1, 1, 10);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
    }

    #endregion

    #region 5. SearchAsync Tests

    [Fact]
    public async Task SearchAsync_ValidSearch_ReturnsMatchingSuppliers()
    {
        // Arrange
        var suppliers = new List<Supplier>
        {
            new() { SupplierId = 1, SupplierName = "Vinamilk", Status = 1 }
        };
        _mockSupplierRepository.Setup(r => r.SearchAsync("Vina", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(suppliers);

        // Act
        var result = await _supplierService.SearchAsync("Vina");

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Vinamilk", result.First().SupplierName);
    }

    #endregion

    #region 6. GetDropdownAsync Tests

    [Fact]
    public async Task GetDropdownAsync_ReturnsDropdownList()
    {
        // Arrange
        var dropdowns = new List<Supplier>
        {
            new() { SupplierId = 1, SupplierCode = "SUP001", SupplierName = "NCC 1" }
        };
        _mockSupplierRepository.Setup(r => r.GetDropdownAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(dropdowns);

        // Act
        var result = await _supplierService.GetDropdownAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("SUP001", result.First().SupplierCode);
    }

    #endregion

    #region 7. GetSuppliedProductsBySupplierIdAsync Tests

    [Fact]
    public async Task GetSuppliedProductsBySupplierIdAsync_ExistingSupplier_ReturnsProductSuppliers()
    {
        // Arrange
        var supplier = new Supplier { SupplierId = 1, SupplierName = "NCC 1" };
        var links = new List<ProductSupplier>
        {
            new()
            {
                ProductId = 10,
                SupplierId = 1,
                PurchasePrice = 15000,
                LeadTime = 3,
                MinimumOrderQuantity = 10,
                Rating = 4.5m,
                Product = new Product { ProductId = 10, ProductName = "Sữa tươi Vinamilk 1L", Barcode = "8934567890123" }
            }
        };

        _mockSupplierRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(supplier);
        _mockProductSupplierRepository.Setup(r => r.GetSuppliedProductsBySupplierIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(links);

        // Act
        var result = await _supplierService.GetSuppliedProductsBySupplierIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Sữa tươi Vinamilk 1L", result.First().ProductName);
        Assert.Equal(15000, result.First().PurchasePrice);
    }

    [Fact]
    public async Task GetSuppliedProductsBySupplierIdAsync_NonExistingSupplier_ThrowsKeyNotFoundException()
    {
        // Arrange
        _mockSupplierRepository.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Supplier?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _supplierService.GetSuppliedProductsBySupplierIdAsync(99));
    }

    #endregion

    #region 8. GetSupplierStatisticsAsync Tests

    [Fact]
    public async Task GetSupplierStatisticsAsync_CalculatesCorrectStats()
    {
        // Arrange
        var links1 = new List<ProductSupplier> { new() { Rating = 4.0m }, new() { Rating = 5.0m } };
        var suppliers = new List<Supplier>
        {
            new() { SupplierId = 1, Status = 1, ProductSuppliers = links1 },
            new() { SupplierId = 2, Status = 2, ProductSuppliers = new List<ProductSupplier>() }
        };

        _mockSupplierRepository.Setup(r => r.GetAllAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(suppliers);

        // Act
        var result = await _supplierService.GetSupplierStatisticsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalSuppliers);
        Assert.Equal(1, result.ActiveSuppliers);
        Assert.Equal(1, result.InactiveSuppliers);
        Assert.Equal(2, result.TotalSuppliedProducts);
        Assert.Equal(4.50m, result.AverageRating);
    }

    #endregion

    #region 9. CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidRequest_CreatesSupplierSuccessfully()
    {
        // Arrange
        var request = new CreateSupplierRequest
        {
            SupplierCode = "SUP00100",
            SupplierName = "Công ty Nông nghiệp Sạch",
            ContactPerson = "Trần Văn B",
            PhoneNumber = "0912345678",
            Email = "cleanfarm@supplier.com",
            Address = "Hà Nội",
            TaxCode = "0101234567"
        };

        _mockSupplierRepository.Setup(r => r.ExistsNameAsync("Công ty Nông nghiệp Sạch", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockSupplierRepository.Setup(r => r.ExistsCodeAsync("SUP00100", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _supplierService.CreateAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("SUP00100", result.SupplierCode);
        Assert.Equal("Công ty Nông nghiệp Sạch", result.SupplierName);
        _mockSupplierRepository.Verify(r => r.AddAsync(It.IsAny<Supplier>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockSupplierRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_DuplicateName_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = new CreateSupplierRequest { SupplierName = "Vinamilk" };
        _mockSupplierRepository.Setup(r => r.ExistsNameAsync("Vinamilk", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _supplierService.CreateAsync(request));
        Assert.Contains("BR-SUPP-01", ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_EmptyName_ThrowsArgumentException(string invalidName)
    {
        // Arrange
        var request = new CreateSupplierRequest { SupplierName = invalidName };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _supplierService.CreateAsync(request));
        Assert.Contains("BR-SUPP-01", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_NameExceeds150Chars_ThrowsArgumentException()
    {
        // Arrange
        var request = new CreateSupplierRequest { SupplierName = new string('A', 151) };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _supplierService.CreateAsync(request));
        Assert.Contains("BR-SUPP-01", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_InvalidPhone_ThrowsArgumentException()
    {
        // Arrange
        var request = new CreateSupplierRequest
        {
            SupplierName = "Nhà cung cấp mới",
            PhoneNumber = "123456" // Invalid VN phone
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _supplierService.CreateAsync(request));
        Assert.Contains("BR-SUPP-02", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_InvalidEmail_ThrowsArgumentException()
    {
        // Arrange
        var request = new CreateSupplierRequest
        {
            SupplierName = "Nhà cung cấp mới",
            Email = "invalid-email-format"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _supplierService.CreateAsync(request));
        Assert.Contains("BR-SUPP-02", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_AutoGeneratesCode_WhenCodeIsEmpty()
    {
        // Arrange
        var request = new CreateSupplierRequest { SupplierName = "Nhà cung cấp tự phát sinh mã" };
        _mockSupplierRepository.Setup(r => r.ExistsNameAsync("Nhà cung cấp tự phát sinh mã", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _supplierService.CreateAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.StartsWith("SUP", result.SupplierCode);
    }

    #endregion

    #region 10. UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ValidRequest_UpdatesSupplierSuccessfully()
    {
        // Arrange
        var existingSupplier = new Supplier { SupplierId = 1, SupplierName = "Tên cũ", Status = 1 };
        var request = new UpdateSupplierRequest
        {
            SupplierName = "Tên mới cập nhật",
            PhoneNumber = "0987654321",
            Email = "newemail@supplier.com",
            Status = 1
        };

        _mockSupplierRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSupplier);
        _mockSupplierRepository.Setup(r => r.ExistsNameAsync("Tên mới cập nhật", 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _supplierService.UpdateAsync(1, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Tên mới cập nhật", result.SupplierName);
        _mockSupplierRepository.Verify(r => r.Update(existingSupplier), Times.Once);
        _mockSupplierRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingSupplier_ThrowsKeyNotFoundException()
    {
        // Arrange
        _mockSupplierRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Supplier?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _supplierService.UpdateAsync(999, new UpdateSupplierRequest { SupplierName = "Test" }));
    }

    [Fact]
    public async Task UpdateAsync_DuplicateNameForOtherSupplier_ThrowsInvalidOperationException()
    {
        // Arrange
        var existingSupplier = new Supplier { SupplierId = 1, SupplierName = "Tên cũ" };
        var request = new UpdateSupplierRequest { SupplierName = "Tên trùng" };

        _mockSupplierRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSupplier);
        _mockSupplierRepository.Setup(r => r.ExistsNameAsync("Tên trùng", 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _supplierService.UpdateAsync(1, request));
        Assert.Contains("BR-SUPP-01", ex.Message);
    }

    #endregion

    #region 11. SoftDeleteAsync & DeleteAsync Tests

    [Fact]
    public async Task SoftDeleteAsync_ExistingSupplier_SetsStatusToInactive()
    {
        // Arrange
        var existingSupplier = new Supplier { SupplierId = 1, SupplierName = "NCC Xóa", Status = 1 };
        _mockSupplierRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSupplier);

        // Act
        var result = await _supplierService.SoftDeleteAsync(1);

        // Assert
        Assert.True(result);
        Assert.Equal((byte)2, existingSupplier.Status);
        _mockSupplierRepository.Verify(r => r.Update(existingSupplier), Times.Once);
        _mockSupplierRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SoftDeleteAsync_NonExistingSupplier_ThrowsKeyNotFoundException()
    {
        // Arrange
        _mockSupplierRepository.Setup(r => r.GetByIdAsync(88, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Supplier?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _supplierService.SoftDeleteAsync(88));
    }

    #endregion

    #region 12. RestoreAsync Tests

    [Fact]
    public async Task RestoreAsync_ExistingSupplier_SetsStatusToActive()
    {
        // Arrange
        var inactiveSupplier = new Supplier { SupplierId = 1, SupplierName = "NCC Khôi Phục", Status = 2 };
        _mockSupplierRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inactiveSupplier);

        // Act
        var result = await _supplierService.RestoreAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal((byte)1, inactiveSupplier.Status);
        _mockSupplierRepository.Verify(r => r.Update(inactiveSupplier), Times.Once);
    }

    [Fact]
    public async Task RestoreAsync_NonExistingSupplier_ThrowsKeyNotFoundException()
    {
        // Arrange
        _mockSupplierRepository.Setup(r => r.GetByIdAsync(77, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Supplier?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _supplierService.RestoreAsync(77));
    }

    #endregion

    #region 13. UploadLogoAsync Tests

    [Fact]
    public async Task UploadLogoAsync_ExistingSupplier_UpdatesLogoUrl()
    {
        // Arrange
        var supplier = new Supplier { SupplierId = 1, LogoUrl = "/images/suppliers/old.png" };
        _mockSupplierRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(supplier);

        // Act
        var result = await _supplierService.UploadLogoAsync(1, "/images/suppliers/new.png");

        // Assert
        Assert.Equal("/images/suppliers/new.png", result);
        Assert.Equal("/images/suppliers/new.png", supplier.LogoUrl);
    }

    [Fact]
    public async Task UploadLogoAsync_NonExistingSupplier_ThrowsKeyNotFoundException()
    {
        // Arrange
        _mockSupplierRepository.Setup(r => r.GetByIdAsync(66, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Supplier?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _supplierService.UploadLogoAsync(66, "/images/suppliers/logo.png"));
    }

    #endregion

    #region 14. LinkProductSupplierAsync Tests

    [Fact]
    public async Task LinkProductSupplierAsync_NewLink_CreatesLinkSuccessfully()
    {
        // Arrange
        var request = new LinkProductSupplierRequest
        {
            ProductId = 10,
            SupplierId = 1,
            PurchasePrice = 20000,
            LeadTime = 2,
            MinimumOrderQuantity = 5,
            Rating = 4.8m,
            IsDefault = true
        };

        _mockProductSupplierRepository.Setup(r => r.GetLinkAsync(10, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductSupplier?)null);

        // Act
        var result = await _supplierService.LinkProductSupplierAsync(request);

        // Assert
        Assert.True(result);
        _mockProductSupplierRepository.Verify(r => r.ResetOtherDefaultsAsync(10, 1, It.IsAny<CancellationToken>()), Times.Once);
        _mockProductSupplierRepository.Verify(r => r.AddLinkAsync(It.IsAny<ProductSupplier>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockProductSupplierRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LinkProductSupplierAsync_ExistingLink_UpdatesLinkSuccessfully()
    {
        // Arrange
        var existing = new ProductSupplier { ProductId = 10, SupplierId = 1, PurchasePrice = 10000 };
        var request = new LinkProductSupplierRequest
        {
            ProductId = 10,
            SupplierId = 1,
            PurchasePrice = 25000,
            LeadTime = 4,
            MinimumOrderQuantity = 10,
            Rating = 4.0m,
            IsDefault = false
        };

        _mockProductSupplierRepository.Setup(r => r.GetLinkAsync(10, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        // Act
        var result = await _supplierService.LinkProductSupplierAsync(request);

        // Assert
        Assert.True(result);
        Assert.Equal(25000, existing.PurchasePrice);
        _mockProductSupplierRepository.Verify(r => r.UpdateLink(existing), Times.Once);
    }

    [Fact]
    public async Task LinkProductSupplierAsync_NegativePurchasePrice_ThrowsArgumentException()
    {
        // Arrange
        var request = new LinkProductSupplierRequest { ProductId = 1, SupplierId = 1, PurchasePrice = -100 };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _supplierService.LinkProductSupplierAsync(request));
        Assert.Contains("BR-SUPP-04", ex.Message);
    }

    [Fact]
    public async Task LinkProductSupplierAsync_NegativeLeadTime_ThrowsArgumentException()
    {
        // Arrange
        var request = new LinkProductSupplierRequest { ProductId = 1, SupplierId = 1, PurchasePrice = 10, LeadTime = -1 };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _supplierService.LinkProductSupplierAsync(request));
        Assert.Contains("BR-SUPP-04", ex.Message);
    }

    [Fact]
    public async Task LinkProductSupplierAsync_MoqLessThanOne_ThrowsArgumentException()
    {
        // Arrange
        var request = new LinkProductSupplierRequest { ProductId = 1, SupplierId = 1, PurchasePrice = 10, LeadTime = 1, MinimumOrderQuantity = 0 };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _supplierService.LinkProductSupplierAsync(request));
        Assert.Contains("BR-SUPP-04", ex.Message);
    }

    [Theory]
    [InlineData(0.99)]
    [InlineData(5.01)]
    public async Task LinkProductSupplierAsync_InvalidRating_ThrowsArgumentException(decimal invalidRating)
    {
        // Arrange
        var request = new LinkProductSupplierRequest
        {
            ProductId = 1,
            SupplierId = 1,
            PurchasePrice = 10,
            LeadTime = 1,
            MinimumOrderQuantity = 1,
            Rating = invalidRating
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _supplierService.LinkProductSupplierAsync(request));
        Assert.Contains("BR-SUPP-04", ex.Message);
    }

    #endregion

    #region 15. UnlinkProductSupplierAsync Tests

    [Fact]
    public async Task UnlinkProductSupplierAsync_ExistingLink_RemovesLinkSuccessfully()
    {
        // Arrange
        var link = new ProductSupplier { ProductId = 10, SupplierId = 1 };
        _mockProductSupplierRepository.Setup(r => r.GetLinkAsync(10, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(link);

        // Act
        var result = await _supplierService.UnlinkProductSupplierAsync(10, 1);

        // Assert
        Assert.True(result);
        _mockProductSupplierRepository.Verify(r => r.Unlink(link), Times.Once);
        _mockProductSupplierRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UnlinkProductSupplierAsync_NonExistingLink_ThrowsKeyNotFoundException()
    {
        // Arrange
        _mockProductSupplierRepository.Setup(r => r.GetLinkAsync(99, 99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductSupplier?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _supplierService.UnlinkProductSupplierAsync(99, 99));
    }

    #endregion

    #region 16. UpdateProductSupplierLinkAsync Tests

    [Fact]
    public async Task UpdateProductSupplierLinkAsync_ExistingLink_UpdatesSuccessfully()
    {
        // Arrange
        var link = new ProductSupplier { ProductId = 10, SupplierId = 1, PurchasePrice = 1000 };
        var request = new UpdateLinkRequest
        {
            PurchasePrice = 1500,
            LeadTime = 3,
            MinimumOrderQuantity = 2,
            Rating = 4.2m,
            IsDefault = true
        };

        _mockProductSupplierRepository.Setup(r => r.GetLinkAsync(10, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(link);

        // Act
        var result = await _supplierService.UpdateProductSupplierLinkAsync(10, 1, request);

        // Assert
        Assert.True(result);
        Assert.Equal(1500, link.PurchasePrice);
        Assert.True(link.IsDefault);
        _mockProductSupplierRepository.Verify(r => r.ResetOtherDefaultsAsync(10, 1, It.IsAny<CancellationToken>()), Times.Once);
        _mockProductSupplierRepository.Verify(r => r.UpdateLink(link), Times.Once);
    }

    [Fact]
    public async Task UpdateProductSupplierLinkAsync_NonExistingLink_ThrowsKeyNotFoundException()
    {
        // Arrange
        _mockProductSupplierRepository.Setup(r => r.GetLinkAsync(55, 55, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductSupplier?)null);

        var request = new UpdateLinkRequest { PurchasePrice = 1000, LeadTime = 1, MinimumOrderQuantity = 1, Rating = 4.0m };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _supplierService.UpdateProductSupplierLinkAsync(55, 55, request));
    }

    #endregion

    #region 17. ImportFromJsonAsync Tests

    [Fact]
    public async Task ImportFromJsonAsync_ValidJson_ReturnsSuccessCount()
    {
        // Arrange
        string json = @"[
            { ""supplierName"": ""Nhà cung cấp JSON 1"", ""supplierCode"": ""SUPJSON01"" },
            { ""supplierName"": ""Nhà cung cấp JSON 2"", ""supplierCode"": ""SUPJSON02"" }
        ]";

        _mockSupplierRepository.Setup(r => r.ExistsNameAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _supplierService.ImportFromJsonAsync(json);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalRows);
        Assert.Equal(2, result.SuccessCount);
        Assert.Equal(0, result.FailureCount);
    }

    [Fact]
    public async Task ImportFromJsonAsync_EmptyJson_ReturnsError()
    {
        // Act
        var result = await _supplierService.ImportFromJsonAsync("");

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Errors);
        Assert.Contains("rỗng", result.Errors.First());
    }

    #endregion

    #region 18. ImportFromCsvAsync Tests

    [Fact]
    public async Task ImportFromCsvAsync_ValidCsv_ReturnsSuccessCount()
    {
        // Arrange
        string csvContent = "SupplierName,SupplierCode,PhoneNumber,Email\nCông ty CSV 1,SUPCSV01,0901112223,csv1@supplier.com\nCông ty CSV 2,SUPCSV02,0902223334,csv2@supplier.com";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

        _mockSupplierRepository.Setup(r => r.ExistsNameAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _supplierService.ImportFromCsvAsync(stream);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalRows);
        Assert.Equal(2, result.SuccessCount);
        Assert.Equal(0, result.FailureCount);
    }

    [Fact]
    public async Task ImportFromCsvAsync_EmptyCsv_ReturnsError()
    {
        // Arrange
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(""));

        // Act
        var result = await _supplierService.ImportFromCsvAsync(stream);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Errors);
        Assert.Contains("rỗng", result.Errors.First());
    }

    #endregion
}
