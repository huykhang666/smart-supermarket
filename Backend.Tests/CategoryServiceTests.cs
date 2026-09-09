using Moq;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Features.Categories.DTOs;
using SmartSupermarket.Backend.Features.Categories.Repositories;
using SmartSupermarket.Backend.Features.Categories.Services;
using Xunit;

namespace SmartSupermarket.Backend.Tests;

public class CategoryServiceTests
{
    private readonly Mock<ICategoryRepository> _mockCategoryRepository;
    private readonly CategoryService _categoryService;

    public CategoryServiceTests()
    {
        _mockCategoryRepository = new Mock<ICategoryRepository>();
        _categoryService = new CategoryService(_mockCategoryRepository.Object);
    }

    #region 1. CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidRequest_ShouldCreateCategorySuccessfully()
    {
        // Arrange
        var request = new CreateCategoryRequest
        {
            CategoryName = "Rau củ quả tươi",
            Description = "Các loại rau củ sạch đóng gói"
        };

        _mockCategoryRepository
            .Setup(r => r.ExistsNameAsync("Rau củ quả tươi", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockCategoryRepository
            .Setup(r => r.ExistsSlugAsync("rau-cu-qua-tuoi", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _categoryService.CreateAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Rau củ quả tươi", result.CategoryName);
        Assert.Equal("rau-cu-qua-tuoi", result.Slug);
        Assert.Equal("Các loại rau củ sạch đóng gói", result.Description);
        _mockCategoryRepository.Verify(r => r.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockCategoryRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_DuplicateCategoryName_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var request = new CreateCategoryRequest { CategoryName = "Nước giải khát" };
        _mockCategoryRepository
            .Setup(r => r.ExistsNameAsync("Nước giải khát", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _categoryService.CreateAsync(request));
        Assert.Contains("BR-CAT-01", ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_EmptyCategoryName_ShouldThrowArgumentException(string invalidName)
    {
        // Arrange
        var request = new CreateCategoryRequest { CategoryName = invalidName };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _categoryService.CreateAsync(request));
        Assert.Contains("BR-CAT-01", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_NameExceeds100Chars_ShouldThrowArgumentException()
    {
        // Arrange
        var request = new CreateCategoryRequest { CategoryName = new string('A', 101) };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _categoryService.CreateAsync(request));
        Assert.Contains("BR-CAT-01", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_DescriptionExceeds255Chars_ShouldThrowArgumentException()
    {
        // Arrange
        var request = new CreateCategoryRequest
        {
            CategoryName = "Danh mục mới",
            Description = new string('A', 256)
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _categoryService.CreateAsync(request));
        Assert.Contains("BR-CAT-02", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_WithNonExistentParent_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var request = new CreateCategoryRequest
        {
            CategoryName = "Sữa đặc",
            ParentId = 99
        };

        _mockCategoryRepository
            .Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _categoryService.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_ConflictingSlug_ShouldAppendUniqueSuffix()
    {
        // Arrange
        var request = new CreateCategoryRequest { CategoryName = "Bánh kẹo" };

        _mockCategoryRepository
            .Setup(r => r.ExistsNameAsync("Bánh kẹo", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockCategoryRepository
            .Setup(r => r.ExistsSlugAsync("banh-keo", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _categoryService.CreateAsync(request);

        // Assert
        Assert.StartsWith("banh-keo-", result.Slug);
    }

    #endregion

    #region 2. UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ValidRequest_ShouldUpdateCategory()
    {
        // Arrange
        int id = 1;
        var category = new Category { CategoryId = id, CategoryName = "Tên cũ", Slug = "ten-cu" };
        var request = new UpdateCategoryRequest { CategoryName = "Tên mới", Description = "Mô tả mới", Status = 1 };

        _mockCategoryRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);
        _mockCategoryRepository
            .Setup(r => r.ExistsNameAsync("Tên mới", id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockCategoryRepository
            .Setup(r => r.ExistsSlugAsync("ten-moi", id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _categoryService.UpdateAsync(id, request);

        // Assert
        Assert.Equal("Tên mới", result.CategoryName);
        Assert.Equal("ten-moi", result.Slug);
        _mockCategoryRepository.Verify(r => r.Update(category), Times.Once);
        _mockCategoryRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_CategoryNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        _mockCategoryRepository
            .Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _categoryService.UpdateAsync(99, new UpdateCategoryRequest { CategoryName = "Test" }));
    }

    [Fact]
    public async Task UpdateAsync_DuplicateName_ShouldThrowInvalidOperationException()
    {
        // Arrange
        int id = 1;
        var category = new Category { CategoryId = id, CategoryName = "Tên cũ" };
        _mockCategoryRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);
        _mockCategoryRepository
            .Setup(r => r.ExistsNameAsync("Tên đã có", id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _categoryService.UpdateAsync(id, new UpdateCategoryRequest { CategoryName = "Tên đã có" }));
        Assert.Contains("BR-CAT-01", ex.Message);
    }

    [Fact]
    public async Task UpdateAsync_ParentIsSelf_ShouldThrowInvalidOperationException()
    {
        // Arrange
        int id = 1;
        var category = new Category { CategoryId = id, CategoryName = "Danh mục A" };
        _mockCategoryRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        var request = new UpdateCategoryRequest { CategoryName = "Danh mục A", ParentId = 1 };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _categoryService.UpdateAsync(id, request));
        Assert.Contains("BR-CAT-05", ex.Message);
    }

    [Fact]
    public async Task UpdateAsync_ParentIsChildCategory_ShouldThrowInvalidOperationException()
    {
        // Arrange
        int parentId = 1;
        int childId = 2;

        var parentCat = new Category { CategoryId = parentId, CategoryName = "Cha" };
        var childCat = new Category { CategoryId = childId, CategoryName = "Con", ParentId = parentId };

        _mockCategoryRepository
            .Setup(r => r.GetByIdAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(parentCat);
        _mockCategoryRepository
            .Setup(r => r.GetByIdAsync(childId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(childCat);
        _mockCategoryRepository
            .Setup(r => r.GetDescendantCategoryIdsAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<int> { childId });

        var request = new UpdateCategoryRequest { CategoryName = "Cha", ParentId = childId };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _categoryService.UpdateAsync(parentId, request));
        Assert.Contains("BR-CAT-05", ex.Message);
    }

    #endregion

    #region 3. SoftDeleteAsync & RestoreAsync Tests

    [Fact]
    public async Task SoftDeleteAsync_CategoryWithProducts_ShouldThrowInvalidOperationException()
    {
        // Arrange
        int catId = 1;
        var category = new Category { CategoryId = catId, CategoryName = "Nước giải khát" };
        var products = new List<Product> { new Product { ProductId = 10, ProductName = "Coca lon" } };

        _mockCategoryRepository
            .Setup(r => r.GetByIdAsync(catId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);
        _mockCategoryRepository
            .Setup(r => r.GetProductsByCategoryIdAsync(catId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _categoryService.SoftDeleteAsync(catId));
        Assert.Contains("BR-CAT-03", ex.Message);
    }

    [Fact]
    public async Task SoftDeleteAsync_CategoryWithoutProducts_ShouldSetStatusToInactive()
    {
        // Arrange
        int catId = 1;
        var category = new Category { CategoryId = catId, CategoryName = "Rau rác", Status = 1 };

        _mockCategoryRepository
            .Setup(r => r.GetByIdAsync(catId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);
        _mockCategoryRepository
            .Setup(r => r.GetProductsByCategoryIdAsync(catId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());

        // Act
        var result = await _categoryService.SoftDeleteAsync(catId);

        // Assert
        Assert.True(result);
        Assert.Equal((byte)2, category.Status); // Inactive
        _mockCategoryRepository.Verify(r => r.Update(category), Times.Once);
        _mockCategoryRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RestoreAsync_ValidCategory_ShouldSetStatusToActive()
    {
        // Arrange
        int catId = 1;
        var category = new Category { CategoryId = catId, CategoryName = "Đã xóa", Status = 2 };

        _mockCategoryRepository
            .Setup(r => r.GetByIdAsync(catId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        // Act
        var result = await _categoryService.RestoreAsync(catId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal((byte)1, category.Status);
        Assert.Equal("Active", result.StatusName);
    }

    [Fact]
    public async Task RestoreAsync_NotFound_ShouldThrowKeyNotFoundException()
    {
        _mockCategoryRepository
            .Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _categoryService.RestoreAsync(99));
    }

    #endregion

    #region 4. MoveParentAsync Tests

    [Fact]
    public async Task MoveParentAsync_ValidNewParent_ShouldUpdateParentId()
    {
        // Arrange
        int catId = 2;
        int newParentId = 1;

        var category = new Category { CategoryId = catId, CategoryName = "Sữa chua" };
        var parentCategory = new Category { CategoryId = newParentId, CategoryName = "Sữa" };

        _mockCategoryRepository
            .Setup(r => r.GetByIdAsync(catId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);
        _mockCategoryRepository
            .Setup(r => r.GetByIdAsync(newParentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(parentCategory);
        _mockCategoryRepository
            .Setup(r => r.GetDescendantCategoryIdsAsync(catId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<int>());

        // Act
        var result = await _categoryService.MoveParentAsync(catId, newParentId);

        // Assert
        Assert.Equal(newParentId, category.ParentId);
        _mockCategoryRepository.Verify(r => r.Update(category), Times.Once);
    }

    [Fact]
    public async Task MoveParentAsync_SelfAsParent_ShouldThrowInvalidOperationException()
    {
        // Arrange
        int catId = 1;
        var category = new Category { CategoryId = catId, CategoryName = "Danh mục" };
        _mockCategoryRepository
            .Setup(r => r.GetByIdAsync(catId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _categoryService.MoveParentAsync(catId, catId));
    }

    #endregion

    #region 5. MoveCategoryProductsAsync Tests

    [Fact]
    public async Task MoveCategoryProductsAsync_SameSourceAndTargetId_ShouldThrowArgumentException()
    {
        // Arrange
        var request = new MoveCategoryRequest
        {
            SourceCategoryId = 1,
            TargetCategoryId = 1
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _categoryService.MoveCategoryProductsAsync(request));
        Assert.Contains("BR-CAT-04", ex.Message);
    }

    [Fact]
    public async Task MoveCategoryProductsAsync_ValidRequest_ShouldCallRepositoryMoveAndSave()
    {
        // Arrange
        var request = new MoveCategoryRequest { SourceCategoryId = 1, TargetCategoryId = 2 };

        var sourceCat = new Category { CategoryId = 1, CategoryName = "Danh mục cũ" };
        var targetCat = new Category { CategoryId = 2, CategoryName = "Danh mục mới" };

        _mockCategoryRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sourceCat);
        _mockCategoryRepository
            .Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetCat);

        // Act
        var result = await _categoryService.MoveCategoryProductsAsync(request);

        // Assert
        Assert.True(result);
        _mockCategoryRepository.Verify(r => r.MoveProductCategoryRecordsAsync(1, 2, It.IsAny<CancellationToken>()), Times.Once);
        _mockCategoryRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MoveCategoryProductsAsync_SourceNotFound_ShouldThrowKeyNotFoundException()
    {
        var request = new MoveCategoryRequest { SourceCategoryId = 99, TargetCategoryId = 2 };
        _mockCategoryRepository
            .Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _categoryService.MoveCategoryProductsAsync(request));
    }

    #endregion

    #region 6. Reorder & Image Upload Tests

    [Fact]
    public async Task ReorderAsync_ValidRequest_ShouldUpdateOrderIndex()
    {
        // Arrange
        int id = 1;
        var category = new Category { CategoryId = id, OrderIndex = 0 };

        _mockCategoryRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        // Act
        var result = await _categoryService.ReorderAsync(id, 5);

        // Assert
        Assert.Equal(5, category.OrderIndex);
        _mockCategoryRepository.Verify(r => r.Update(category), Times.Once);
    }

    [Fact]
    public async Task UploadImageAsync_ValidRequest_ShouldUpdateImageUrl()
    {
        // Arrange
        int id = 1;
        var category = new Category { CategoryId = id, ImageUrl = null };

        _mockCategoryRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        // Act
        var url = await _categoryService.UploadImageAsync(id, "/images/cat1.png");

        // Assert
        Assert.Equal("/images/cat1.png", url);
        Assert.Equal("/images/cat1.png", category.ImageUrl);
    }

    #endregion

    #region 7. Query Tests (GetById, GetBySlug, GetPaged, GetTree, Dropdown, Search)

    [Fact]
    public async Task GetByIdAsync_ValidId_ShouldReturnCategoryDto()
    {
        // Arrange
        var category = new Category { CategoryId = 1, CategoryName = "Thực phẩm" };
        _mockCategoryRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        // Act
        var result = await _categoryService.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.CategoryId);
        Assert.Equal("Thực phẩm", result.CategoryName);
    }

    [Fact]
    public async Task GetBySlugAsync_ValidSlug_ShouldReturnCategoryDto()
    {
        // Arrange
        var category = new Category { CategoryId = 1, CategoryName = "Thực phẩm", Slug = "thuc-pham" };
        _mockCategoryRepository
            .Setup(r => r.GetBySlugAsync("thuc-pham", It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        // Act
        var result = await _categoryService.GetBySlugAsync("thuc-pham");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("thuc-pham", result.Slug);
    }

    [Fact]
    public async Task GetPagedAsync_ValidPagination_ShouldReturnCategoryPagedResult()
    {
        // Arrange
        var items = new List<Category>
        {
            new Category { CategoryId = 1, CategoryName = "Category 1" },
            new Category { CategoryId = 2, CategoryName = "Category 2" }
        };

        _mockCategoryRepository
            .Setup(r => r.GetPagedAsync(null, null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((items, 2));

        // Act
        var paged = await _categoryService.GetPagedAsync(null, null, 1, 10);

        // Assert
        Assert.NotNull(paged);
        Assert.Equal(2, paged.TotalCount);
        Assert.Equal(1, paged.TotalPages);
        Assert.Equal(2, paged.Items.Count());
    }

    [Fact]
    public async Task GetTreeAsync_ShouldReturnHierarchicalTree()
    {
        // Arrange
        var roots = new List<Category>
        {
            new Category
            {
                CategoryId = 1,
                CategoryName = "Bánh kẹo",
                SubCategories = new List<Category>
                {
                    new Category { CategoryId = 2, CategoryName = "Bánh quy", ParentId = 1 }
                }
            }
        };

        _mockCategoryRepository
            .Setup(r => r.GetTreeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(roots);

        // Act
        var tree = (await _categoryService.GetTreeAsync()).ToList();

        // Assert
        Assert.Single(tree);
        Assert.Equal("Bánh kẹo", tree[0].CategoryName);
        Assert.Single(tree[0].SubCategories);
        Assert.Equal("Bánh quy", tree[0].SubCategories[0].CategoryName);
    }

    [Fact]
    public async Task GetDropdownAsync_ShouldReturnCategoryDropdownDtoList()
    {
        // Arrange
        var list = new List<Category>
        {
            new Category { CategoryId = 1, CategoryName = "Ăn vặt", Slug = "an-vat" }
        };

        _mockCategoryRepository
            .Setup(r => r.GetDropdownAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(list);

        // Act
        var dropdown = (await _categoryService.GetDropdownAsync()).ToList();

        // Assert
        Assert.Single(dropdown);
        Assert.Equal("Ăn vặt", dropdown[0].CategoryName);
    }

    [Fact]
    public async Task GetProductsByCategoryIdAsync_NotFound_ShouldThrowKeyNotFoundException()
    {
        _mockCategoryRepository
            .Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _categoryService.GetProductsByCategoryIdAsync(99));
    }

    #endregion

    #region 8. Slug Generation Utility Tests

    [Theory]
    [InlineData("Nước giải khát", "nuoc-giai-khat")]
    [InlineData("Rau Củ & Quả Tươi 100%", "rau-cu-qua-tuoi-100")]
    [InlineData("  Sữa   Tươi   Đặc   ", "sua-tuoi-dac")]
    public void GenerateSlug_ShouldConvertVietnameseAccentsAndSpacesCorrectly(string input, string expectedSlug)
    {
        string slug = CategoryService.GenerateSlug(input);
        Assert.Equal(expectedSlug, slug);
    }

    #endregion
}
