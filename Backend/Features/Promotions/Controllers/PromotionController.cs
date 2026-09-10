using Microsoft.AspNetCore.Mvc;
using SmartSupermarket.Backend.Common.Results;
using SmartSupermarket.Backend.Features.Promotions.DTOs;
using SmartSupermarket.Backend.Features.Promotions.Services;

namespace SmartSupermarket.Backend.Features.Promotions.Controllers;

[ApiController]
[Route("api/v1/promotions")]
public class PromotionController : ControllerBase
{
    private readonly IPromotionService _promotionService;

    public PromotionController(IPromotionService promotionService)
    {
        _promotionService = promotionService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResult<PromotionPagedResult>>> GetPaged(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _promotionService.GetPagedAsync(search, isActive, page, pageSize, cancellationToken);
        return Ok(ApiResult<PromotionPagedResult>.Success(result, "Lấy danh sách khuyến mãi thành công"));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResult<PromotionDto>>> GetById(int id, CancellationToken cancellationToken = default)
    {
        var promotion = await _promotionService.GetByIdAsync(id, cancellationToken);
        if (promotion == null)
        {
            return NotFound(ApiResult<PromotionDto>.Failure($"Không tìm thấy khuyến mãi có ID = {id}"));
        }
        return Ok(ApiResult<PromotionDto>.Success(promotion, "Lấy thông tin khuyến mãi thành công"));
    }

    [HttpGet("code/{code}")]
    public async Task<ActionResult<ApiResult<PromotionDto>>> GetByCode(string code, CancellationToken cancellationToken = default)
    {
        var promotion = await _promotionService.GetByCodeAsync(code, cancellationToken);
        if (promotion == null)
        {
            return NotFound(ApiResult<PromotionDto>.Failure($"Không tìm thấy khuyến mãi có mã '{code}'"));
        }
        return Ok(ApiResult<PromotionDto>.Success(promotion, "Lấy thông tin khuyến mãi thành công"));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResult<PromotionDto>>> Create(
        [FromBody] CreatePromotionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var created = await _promotionService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.PromotionId }, ApiResult<PromotionDto>.Success(created, "Tạo khuyến mãi thành công"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResult<PromotionDto>.Failure(ex.Message));
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResult<PromotionDto>>> Update(
        int id,
        [FromBody] UpdatePromotionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var updated = await _promotionService.UpdateAsync(id, request, cancellationToken);
            return Ok(ApiResult<PromotionDto>.Success(updated, "Cập nhật khuyến mãi thành công"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResult<PromotionDto>.Failure(ex.Message));
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResult<bool>>> Delete(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var success = await _promotionService.DeleteAsync(id, cancellationToken);
            return Ok(ApiResult<bool>.Success(success, "Xóa khuyến mãi thành công"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResult<bool>.Failure(ex.Message));
        }
    }

    [HttpPost("apply")]
    public async Task<ActionResult<ApiResult<ApplyPromotionResponse>>> Apply(
        [FromBody] ApplyPromotionRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _promotionService.ApplyPromotionAsync(request, cancellationToken);
        return Ok(ApiResult<ApplyPromotionResponse>.Success(result, result.Message));
    }
}
