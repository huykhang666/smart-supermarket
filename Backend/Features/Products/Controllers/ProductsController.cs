using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartSupermarket.Backend.Common.Results;
using SmartSupermarket.Backend.Features.Products.DTOs;
using SmartSupermarket.Backend.Features.Products.Services;
using SmartSupermarket.Backend.Infrastructure.Persistence;
using Swashbuckle.AspNetCore.Annotations;

namespace SmartSupermarket.Backend.Features.Products.Controllers;

[ApiController]
[Route("api/[controller]")]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly IBarcodeService _barcodeService;
    private readonly AppDbContext _dbContext;

    public ProductsController(IProductService productService, IBarcodeService barcodeService, AppDbContext dbContext)
    {
        _productService = productService;
        _barcodeService = barcodeService;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Lấy danh sách 100+ sản phẩm siêu thị mẫu chi tiết (Tên, mã vạch, danh mục, giá gốc, giá bán, giá ưu đãi, tồn kho, vị trí kệ)
    /// </summary>
    [HttpGet("sample")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResult<List<ProductSeedItem>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSampleCatalog(CancellationToken cancellationToken = default)
    {
        var seedPath = DbInitializer.ResolveSeedFilePath();
        if (System.IO.File.Exists(seedPath))
        {
            var json = await System.IO.File.ReadAllTextAsync(seedPath, cancellationToken);
            var items = System.Text.Json.JsonSerializer.Deserialize<List<ProductSeedItem>>(
                json, 
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );
            return Ok(ApiResult<List<ProductSeedItem>>.Success(items ?? new List<ProductSeedItem>(), "Lấy dữ liệu 100 sản phẩm mẫu thành công."));
        }
        return NotFound(ApiResult<string>.Failure("Không tìm thấy tệp products_seed.json."));
    }

    /// <summary>
    /// Lấy danh sách sản phẩm phân trang (Hỗ trợ tìm kiếm, lọc theo danh mục, nhà cung cấp, khoảng giá, trạng thái)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResult<ProductPagedResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] string? search,
        [FromQuery] int? categoryId,
        [FromQuery] int? supplierId,
        [FromQuery] byte? status,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _productService.SearchProductsAsync(
            search, categoryId, supplierId, status, minPrice, maxPrice, page, pageSize, cancellationToken);

        return Ok(ApiResult<ProductPagedResult>.Success(result, "Lấy danh sách sản phẩm thành công."));
    }

    /// <summary>
    /// Lấy chi tiết thông tin 1 sản phẩm theo ProductId
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResult<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult<ProductDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken cancellationToken = default)
    {
        var product = await _productService.GetProductByIdAsync(id, cancellationToken);
        if (product == null)
        {
            return NotFound(ApiResult<ProductDto?>.Failure($"Không tìm thấy sản phẩm có ID = {id}."));
        }

        return Ok(ApiResult<ProductDto>.Success(product, "Lấy thông tin sản phẩm thành công."));
    }

    /// <summary>
    /// Tra cứu sản phẩm tức thì theo Mã vạch (Barcode) tại quầy POS WinForms
    /// </summary>
    [HttpGet("barcode/{barcode}")]
    [ProducesResponseType(typeof(ApiResult<ProductBarcodeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult<ProductBarcodeDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByBarcode([FromRoute] string barcode, CancellationToken cancellationToken = default)
    {
        var productBarcode = await _barcodeService.LookupByBarcodeAsync(barcode, cancellationToken);
        if (productBarcode == null)
        {
            return NotFound(ApiResult<ProductBarcodeDto?>.Failure($"Mã vạch '{barcode}' không tồn tại trên hệ thống."));
        }

        return Ok(ApiResult<ProductBarcodeDto>.Success(productBarcode, "Tra cứu mã vạch thành công."));
    }

    /// <summary>
    /// Tạo mới Sản phẩm vào hệ thống (Yêu cầu Role Admin hoặc Manager)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(ApiResult<ProductDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResult<ProductDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var createdProduct = await _productService.CreateProductAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = createdProduct.ProductId }, 
                ApiResult<ProductDto>.Success(createdProduct, "Tạo mới sản phẩm thành công."));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResult<ProductDto>.Failure(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResult<ProductDto>.Failure(ex.Message));
        }
        catch (KeyNotFoundException ex)
        {
            return BadRequest(ApiResult<ProductDto>.Failure(ex.Message));
        }
    }

    /// <summary>
    /// Cập nhật thông tin Sản phẩm (Yêu cầu Role Admin hoặc Manager)
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(ApiResult<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult<ProductDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResult<ProductDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var updatedProduct = await _productService.UpdateProductAsync(id, request, cancellationToken);
            return Ok(ApiResult<ProductDto>.Success(updatedProduct, "Cập nhật sản phẩm thành công."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResult<ProductDto>.Failure(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResult<ProductDto>.Failure(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResult<ProductDto>.Failure(ex.Message));
        }
    }

    /// <summary>
    /// Chuyển trạng thái sản phẩm sang Ngừng kinh doanh (Soft Delete - Yêu cầu Role Admin hoặc Manager)
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(ApiResult<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken cancellationToken = default)
    {
        try
        {
            await _productService.DeleteProductAsync(id, cancellationToken);
            return Ok(ApiResult<bool>.Success(true, "Đã chuyển trạng thái sản phẩm sang Ngừng kinh doanh thành công."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResult<bool>.Failure(ex.Message));
        }
    }

    /// <summary>
    /// Tải lên Hình ảnh cho Sản phẩm trực tiếp từ máy (Lưu vào máy chủ server wwwroot/images/products)
    /// </summary>
    [HttpPost("upload-image")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResult<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadImage([FromForm] int? productId, IFormFile file, CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(ApiResult<object>.Failure("Vui lòng chọn file hình ảnh hợp lệ."));
        }

        try
        {
            using var stream = file.OpenReadStream();
            string imageUrl = await _productService.UploadProductImageAsync(productId, stream, file.FileName, cancellationToken);
            return Ok(ApiResult<object>.Success(new { imageUrl }, "Tải lên hình ảnh thành công."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResult<object>.Failure(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResult<object>.Failure(ex.Message));
        }
    }

    /// <summary>
    /// Tự động sinh mã vạch nội bộ GS1 EAN-13 (Tiền tố 200)
    /// </summary>
    [HttpPost("generate-barcode")]
    [ProducesResponseType(typeof(ApiResult<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GenerateBarcode([FromQuery] int? productId, CancellationToken cancellationToken = default)
    {
        string barcode = await _barcodeService.GenerateEan13Async("200", cancellationToken);
        return Ok(ApiResult<string>.Success(barcode, "Sinh mã vạch EAN-13 thành công."));
    }

    /// <summary>
    /// Cập nhật giá bán niêm yết và giá vốn sản phẩm (Yêu cầu Role Admin hoặc Manager)
    /// </summary>
    [HttpPut("{id:int}/price")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(ApiResult<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult<ProductDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResult<ProductDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePrice([FromRoute] int id, [FromBody] UpdatePriceRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var updatedProduct = await _productService.UpdateProductPriceAsync(id, request, cancellationToken);
            return Ok(ApiResult<ProductDto>.Success(updatedProduct, "Cập nhật giá sản phẩm thành công."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResult<ProductDto>.Failure(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResult<ProductDto>.Failure(ex.Message));
        }
    }

    /// <summary>
    /// Xem chi tiết giá hiện tại và biên lợi nhuận kinh doanh của sản phẩm
    /// </summary>
    [HttpGet("{id:int}/price-history")]
    [ProducesResponseType(typeof(ApiResult<ProductPriceHistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult<ProductPriceHistoryDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPriceHistory([FromRoute] int id, CancellationToken cancellationToken = default)
    {
        var priceHistory = await _productService.GetProductPriceHistoryAsync(id, cancellationToken);
        if (priceHistory == null)
        {
            return NotFound(ApiResult<ProductPriceHistoryDto?>.Failure($"Không tìm thấy sản phẩm có ID = {id}."));
        }

        return Ok(ApiResult<ProductPriceHistoryDto>.Success(priceHistory, "Lấy thông tin giá sản phẩm thành công."));
    }

    /// <summary>
    /// Lấy danh sách quy cách đóng gói quy đổi của sản phẩm (Lon, Lốc 6, Thùng 24,...)
    /// </summary>
    [HttpGet("{id:int}/units")]
    [ProducesResponseType(typeof(ApiResult<List<ProductUnitConversionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProductUnits([FromRoute] int id, CancellationToken cancellationToken = default)
    {
        var product = await _dbContext.Products.FindAsync(new object[] { id }, cancellationToken);
        if (product == null)
        {
            return NotFound(ApiResult<List<ProductUnitConversionDto>>.Failure($"Không tìm thấy sản phẩm ID {id}."));
        }

        var units = await _dbContext.ProductUnitConversions
            .Where(u => u.ProductId == id)
            .OrderBy(u => u.ConversionRate)
            .Select(u => new ProductUnitConversionDto
            {
                Id = u.Id,
                ProductId = u.ProductId,
                UnitName = u.UnitName,
                ConversionRate = u.ConversionRate,
                Price = u.Price,
                Barcode = u.Barcode,
                IsDefault = u.IsDefault
            })
            .ToListAsync(cancellationToken);

        // Luôn kèm đơn vị cơ sở mặc định nếu chưa có cấu hình
        if (!units.Any())
        {
            units.Add(new ProductUnitConversionDto
            {
                Id = 0,
                ProductId = id,
                UnitName = string.IsNullOrEmpty(product.Unit) ? "Cái/Lon" : product.Unit,
                ConversionRate = 1,
                Price = product.Price,
                Barcode = product.Barcode,
                IsDefault = true
            });
        }

        return Ok(ApiResult<List<ProductUnitConversionDto>>.Success(units, "Lấy danh sách quy cách đơn vị thành công."));
    }

    /// <summary>
    /// Thêm quy cách đóng gói quy đổi cho sản phẩm (VD: Lốc 6, Thùng 24)
    /// </summary>
    [HttpPost("{id:int}/units")]
    [ProducesResponseType(typeof(ApiResult<ProductUnitConversionDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddProductUnit([FromRoute] int id, [FromBody] CreateProductUnitRequest request, CancellationToken cancellationToken = default)
    {
        var product = await _dbContext.Products.FindAsync(new object[] { id }, cancellationToken);
        if (product == null)
        {
            return NotFound(ApiResult<ProductUnitConversionDto>.Failure($"Không tìm thấy sản phẩm ID {id}."));
        }

        if (string.IsNullOrWhiteSpace(request.UnitName))
        {
            return BadRequest(ApiResult<ProductUnitConversionDto>.Failure("Tên quy cách đóng gói không được để trống."));
        }

        if (request.ConversionRate <= 0)
        {
            return BadRequest(ApiResult<ProductUnitConversionDto>.Failure("Hệ số quy đổi phải lớn hơn 0."));
        }

        decimal price = request.Price ?? (product.Price * request.ConversionRate);

        var unit = new Domain.Entities.ProductUnitConversion
        {
            ProductId = id,
            UnitName = request.UnitName.Trim(),
            ConversionRate = request.ConversionRate,
            Price = price,
            Barcode = string.IsNullOrWhiteSpace(request.Barcode) ? null : request.Barcode.Trim(),
            IsDefault = request.IsDefault
        };

        _dbContext.ProductUnitConversions.Add(unit);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var dto = new ProductUnitConversionDto
        {
            Id = unit.Id,
            ProductId = unit.ProductId,
            UnitName = unit.UnitName,
            ConversionRate = unit.ConversionRate,
            Price = unit.Price,
            Barcode = unit.Barcode,
            IsDefault = unit.IsDefault
        };

        return CreatedAtAction(nameof(GetProductUnits), new { id }, ApiResult<ProductUnitConversionDto>.Success(dto, "Thêm quy cách đóng gói thành công."));
    }

    /// <summary>
    /// Xóa quy cách đóng gói quy đổi
    /// </summary>
    [HttpDelete("units/{unitId:int}")]
    [ProducesResponseType(typeof(ApiResult<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteProductUnit([FromRoute] int unitId, CancellationToken cancellationToken = default)
    {
        var unit = await _dbContext.ProductUnitConversions.FindAsync(new object[] { unitId }, cancellationToken);
        if (unit == null)
        {
            return NotFound(ApiResult<bool>.Failure($"Không tìm thấy quy cách ID {unitId}."));
        }

        _dbContext.ProductUnitConversions.Remove(unit);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ApiResult<bool>.Success(true, "Xóa quy cách thành công."));
    }
}
