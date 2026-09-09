using Microsoft.AspNetCore.Hosting;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Domain.Enums;
using SmartSupermarket.Backend.Features.Categories.Repositories;
using SmartSupermarket.Backend.Features.Products.DTOs;
using SmartSupermarket.Backend.Features.Products.Repositories;
using SmartSupermarket.Backend.Features.Suppliers.Repositories;

namespace SmartSupermarket.Backend.Features.Products.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly IWebHostEnvironment _environment;

    public ProductService(
        IProductRepository productRepository,
        ICategoryRepository categoryRepository,
        ISupplierRepository supplierRepository,
        IWebHostEnvironment environment)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _supplierRepository = supplierRepository;
        _environment = environment;
    }

    public async Task<IEnumerable<ProductDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var (items, _) = await _productRepository.GetPagedAsync(
            search: null, categoryId: null, supplierId: null, status: null, minPrice: null, maxPrice: null, page: 1, pageSize: 10000, cancellationToken);
        
        return items.Select(MapToProductDto);
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        ValidateProductRequest(request.ProductName, request.Unit, request.Price, request.CostPrice);

        // BR-PROD-05: Category validation
        var category = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);
        if (category == null)
        {
            throw new KeyNotFoundException($"Danh mục với ID {request.CategoryId} không tồn tại trên hệ thống.");
        }

        // BR-PROD-05: Supplier validation
        if (request.SupplierId.HasValue)
        {
            var supplier = await _supplierRepository.GetByIdAsync(request.SupplierId.Value, cancellationToken);
            if (supplier == null)
            {
                throw new KeyNotFoundException($"Nhà cung cấp với ID {request.SupplierId.Value} không tồn tại trên hệ thống.");
            }
        }

        // BR-PROD-01: Barcode Uniqueness
        string barcode = request.Barcode?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(barcode))
        {
            barcode = await GenerateBarcodeAsync(null, cancellationToken);
        }
        else
        {
            if (await _productRepository.ExistsBarcodeAsync(barcode, null, cancellationToken))
            {
                throw new InvalidOperationException($"Mã vạch '{barcode}' đã tồn tại trên hệ thống (BR-PROD-01).");
            }
        }

        var product = new Product
        {
            ProductName = request.ProductName.Trim(),
            Barcode = barcode,
            CategoryId = request.CategoryId,
            SupplierId = request.SupplierId,
            Price = request.Price,
            CostPrice = request.CostPrice,
            ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? "/images/products/no-image.png" : request.ImageUrl.Trim(),
            Unit = request.Unit.Trim().ToLower(),
            Status = ProductStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        await _productRepository.AddAsync(product, cancellationToken);
        await _productRepository.SaveChangesAsync(cancellationToken);

        var createdProduct = await _productRepository.GetByIdAsync(product.ProductId, cancellationToken);
        return MapToProductDto(createdProduct ?? product);
    }

    public async Task<ProductDto> UpdateProductAsync(int id, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(id, cancellationToken);
        if (product == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy sản phẩm với ID {id}.");
        }

        ValidateProductRequest(request.ProductName, request.Unit, request.Price, request.CostPrice);

        // BR-PROD-05: Category validation
        var category = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);
        if (category == null)
        {
            throw new KeyNotFoundException($"Danh mục với ID {request.CategoryId} không tồn tại.");
        }

        // BR-PROD-05: Supplier validation
        if (request.SupplierId.HasValue)
        {
            var supplier = await _supplierRepository.GetByIdAsync(request.SupplierId.Value, cancellationToken);
            if (supplier == null)
            {
                throw new KeyNotFoundException($"Nhà cung cấp với ID {request.SupplierId.Value} không tồn tại.");
            }
        }

        // BR-PROD-01: Barcode Uniqueness check for update
        string newBarcode = request.Barcode.Trim();
        if (await _productRepository.ExistsBarcodeAsync(newBarcode, id, cancellationToken))
        {
            throw new InvalidOperationException($"Mã vạch '{newBarcode}' đã được sử dụng bởi sản phẩm khác (BR-PROD-01).");
        }

        product.ProductName = request.ProductName.Trim();
        product.Barcode = newBarcode;
        product.CategoryId = request.CategoryId;
        product.SupplierId = request.SupplierId;
        product.Price = request.Price;
        product.CostPrice = request.CostPrice;
        if (!string.IsNullOrWhiteSpace(request.ImageUrl))
        {
            product.ImageUrl = request.ImageUrl.Trim();
        }
        product.Unit = request.Unit.Trim().ToLower();
        product.UpdatedAt = DateTime.UtcNow;

        _productRepository.Update(product);
        await _productRepository.SaveChangesAsync(cancellationToken);

        var updatedProduct = await _productRepository.GetByIdAsync(id, cancellationToken);
        return MapToProductDto(updatedProduct ?? product);
    }

    public async Task<bool> DeleteProductAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(id, cancellationToken);
        if (product == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy sản phẩm với ID {id}.");
        }

        // BR-PROD-03: Soft Delete (Switch Status to Inactive)
        product.Status = ProductStatus.Inactive;
        product.UpdatedAt = DateTime.UtcNow;

        _productRepository.Update(product);
        await _productRepository.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<ProductPagedResult> SearchProductsAsync(
        string? search,
        int? categoryId,
        int? supplierId,
        byte? status,
        decimal? minPrice,
        decimal? maxPrice,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var (items, totalCount) = await _productRepository.GetPagedAsync(
            search, categoryId, supplierId, status, minPrice, maxPrice, page, pageSize, cancellationToken);

        return new ProductPagedResult
        {
            Items = items.Select(MapToProductDto),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ProductDto?> GetProductByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(id, cancellationToken);
        return product == null ? null : MapToProductDto(product);
    }

    public async Task<ProductDto?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(barcode))
        {
            return null;
        }

        var product = await _productRepository.GetByBarcodeAsync(barcode.Trim(), cancellationToken);
        return product == null ? null : MapToProductDto(product);
    }

    public async Task<ProductBarcodeDto?> GetBarcodeInfoAsync(string barcode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(barcode))
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

    public async Task<string> GenerateBarcodeAsync(int? productId = null, CancellationToken cancellationToken = default)
    {
        // EAN-13 GS1 Internal Barcode Generation Rule (Barcode.md)
        // Prefix: 200 (Internal Supermarket EAN-13)
        const string prefix = "200";

        for (int attempt = 0; attempt < 50; attempt++)
        {
            int pId = productId ?? Random.Shared.Next(1, 99999);
            string mid5 = (pId % 100000).ToString("D5");
            string extra4 = Random.Shared.Next(0, 9999).ToString("D4");
            string first12 = prefix + mid5 + extra4;

            string candidateBarcode = CalculateEan13Checksum(first12);

            if (!await _productRepository.ExistsBarcodeAsync(candidateBarcode, null, cancellationToken))
            {
                return candidateBarcode;
            }
        }

        throw new InvalidOperationException("Không thể tự động sinh mã vạch nội bộ duy nhất sau 50 lần thử.");
    }

    public Task<string> GenerateInternalProductCodeAsync(CancellationToken cancellationToken = default)
    {
        string code = $"PROD-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";
        return Task.FromResult(code);
    }

    public async Task<string> UploadProductImageAsync(int productId, Stream imageStream, string fileName, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(productId, cancellationToken);
        if (product == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy sản phẩm với ID {productId}.");
        }

        if (imageStream == null || imageStream.Length == 0)
        {
            throw new ArgumentException("File hình ảnh không hợp lệ hoặc rỗng.");
        }

        // BR-PROD-06: File extension & size validation (Max 5MB)
        if (imageStream.Length > 5 * 1024 * 1024)
        {
            throw new ArgumentException("Dung lượng file ảnh vượt quá giới hạn 5 MB (BR-PROD-06).");
        }

        string ext = Path.GetExtension(fileName).ToLowerInvariant();
        string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        if (!allowedExtensions.Contains(ext))
        {
            throw new ArgumentException("Chỉ chấp nhận các định dạng ảnh: .jpg, .jpeg, .png, .webp (BR-PROD-06).");
        }

        string webRoot = _environment.WebRootPath;
        if (string.IsNullOrEmpty(webRoot))
        {
            webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        }

        string uploadFolder = Path.Combine(webRoot, "images", "products");
        Directory.CreateDirectory(uploadFolder);

        string uniqueFileName = $"prod_{productId}_{Guid.NewGuid():N}{ext}";
        string filePath = Path.Combine(uploadFolder, uniqueFileName);

        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await imageStream.CopyToAsync(fileStream, cancellationToken);
        }

        string relativeUrl = $"/images/products/{uniqueFileName}";
        product.ImageUrl = relativeUrl;
        product.UpdatedAt = DateTime.UtcNow;

        _productRepository.Update(product);
        await _productRepository.SaveChangesAsync(cancellationToken);

        return relativeUrl;
    }

    public async Task<ProductDto> UpdateProductPriceAsync(int productId, UpdatePriceRequest request, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(productId, cancellationToken);
        if (product == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy sản phẩm với ID {productId}.");
        }

        if (request.Price < 0)
        {
            throw new ArgumentException("Giá bán niêm yết không được là số âm (BR-PROD-02).");
        }

        if (request.CostPrice.HasValue && request.CostPrice.Value < 0)
        {
            throw new ArgumentException("Giá vốn nhập kho không được là số âm (BR-PROD-02).");
        }

        product.Price = request.Price;
        if (request.CostPrice.HasValue)
        {
            product.CostPrice = request.CostPrice.Value;
        }
        product.UpdatedAt = DateTime.UtcNow;

        _productRepository.Update(product);
        await _productRepository.SaveChangesAsync(cancellationToken);

        var updatedProduct = await _productRepository.GetByIdAsync(productId, cancellationToken);
        return MapToProductDto(updatedProduct ?? product);
    }

    public async Task<ProductPriceHistoryDto?> GetProductPriceHistoryAsync(int productId, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(productId, cancellationToken);
        if (product == null)
        {
            return null;
        }

        var dto = MapToProductDto(product);

        return new ProductPriceHistoryDto
        {
            ProductId = product.ProductId,
            ProductName = product.ProductName,
            CurrentPrice = product.Price,
            CurrentCostPrice = product.CostPrice,
            GrossProfit = dto.GrossProfit,
            ProfitMarginPercentage = dto.ProfitMarginPercentage,
            IsNegativeMarginWarning = dto.IsNegativeMarginWarning,
            LastUpdatedAt = product.UpdatedAt ?? product.CreatedAt
        };
    }

    private static void ValidateProductRequest(string productName, string unit, decimal price, decimal? costPrice)
    {
        if (string.IsNullOrWhiteSpace(productName))
        {
            throw new ArgumentException("Tên sản phẩm không được để trống.");
        }

        if (productName.Trim().Length > 150)
        {
            throw new ArgumentException("Tên sản phẩm không được vượt quá 150 ký tự.");
        }

        if (string.IsNullOrWhiteSpace(unit))
        {
            throw new ArgumentException("Đơn vị tính không được để trống (BR-PROD-04).");
        }

        if (unit.Trim().Length > 20)
        {
            throw new ArgumentException("Đơn vị tính không được vượt quá 20 ký tự (BR-PROD-04).");
        }

        if (price < 0)
        {
            throw new ArgumentException("Giá bán không được là số âm (BR-PROD-02).");
        }

        if (costPrice.HasValue && costPrice.Value < 0)
        {
            throw new ArgumentException("Giá vốn không được là số âm (BR-PROD-02).");
        }
    }

    private static string CalculateEan13Checksum(string first12Digits)
    {
        int sum = 0;
        for (int i = 0; i < 12; i++)
        {
            int digit = first12Digits[i] - '0';
            sum += (i % 2 == 0) ? digit : digit * 3;
        }
        int checkDigit = (10 - (sum % 10)) % 10;
        return first12Digits + checkDigit;
    }

    private static ProductDto MapToProductDto(Product product)
    {
        return new ProductDto
        {
            ProductId = product.ProductId,
            ProductName = product.ProductName,
            Barcode = product.Barcode,
            CategoryId = product.CategoryId,
            CategoryName = product.Category?.CategoryName ?? string.Empty,
            SupplierId = product.SupplierId,
            SupplierName = product.Supplier?.SupplierName,
            Price = product.Price,
            CostPrice = product.CostPrice,
            ImageUrl = string.IsNullOrWhiteSpace(product.ImageUrl) ? "/images/products/no-image.png" : product.ImageUrl,
            Unit = product.Unit,
            Status = product.Status,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        };
    }
}
