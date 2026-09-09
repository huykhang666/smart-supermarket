using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartSupermarket.Backend.Common.Results;
using SmartSupermarket.Backend.Features.Products.DTOs;
using SmartSupermarket.Backend.Features.Products.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace SmartSupermarket.Backend.Features.Products.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly IBarcodeService _barcodeService;

    public ProductsController(IProductService productService, IBarcodeService barcodeService)
    {
        _productService = productService;
        _barcodeService = barcodeService;
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
    /// Tải lên Hình ảnh cho Sản phẩm (Yêu cầu Role Admin hoặc Manager)
    /// </summary>
    [HttpPost("upload-image")]
    [Authorize(Roles = "Admin,Manager")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResult<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadImage([FromForm] int productId, IFormFile file, CancellationToken cancellationToken = default)
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
}
