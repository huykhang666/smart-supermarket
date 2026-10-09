using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SmartSupermarket.Backend.Common.Results;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Features.Inventory.DTOs;
using SmartSupermarket.Backend.Features.Inventory.Services;

namespace SmartSupermarket.Backend.Features.Inventory.Controllers;

[ApiController]
[Route("api/v1/discount-rules")]
[Route("api/discount-rules")]
public class DiscountRuleController : ControllerBase
{
    private readonly IDiscountRuleService _discountRuleService;

    public DiscountRuleController(IDiscountRuleService discountRuleService)
    {
        _discountRuleService = discountRuleService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResult<IEnumerable<DiscountRule>>>> GetAllRules([FromQuery] bool includeInactive = false)
    {
        var rules = await _discountRuleService.GetAllAsync(includeInactive);
        return Ok(ApiResult<IEnumerable<DiscountRule>>.Success(rules, "Lấy danh sách quy tắc giảm giá cận date thành công"));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResult<DiscountRule>>> GetById(int id)
    {
        var rule = await _discountRuleService.GetByIdAsync(id);
        if (rule == null)
            return NotFound(ApiResult<DiscountRule>.Failure($"Không tìm thấy quy tắc giảm giá có ID = {id}"));

        return Ok(ApiResult<DiscountRule>.Success(rule));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResult<DiscountRule>>> CreateRule([FromBody] CreateDiscountRuleRequest request)
    {
        try
        {
            var created = await _discountRuleService.CreateAsync(
                request.DaysBeforeExpiry,
                request.DiscountPercent,
                request.IsActive,
                request.Description);

            return CreatedAtAction(nameof(GetById), new { id = created.DiscountRuleId }, ApiResult<DiscountRule>.Success(created, "Thêm quy tắc giảm giá thành công"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResult<DiscountRule>.Failure(ex.Message));
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResult<bool>>> UpdateRule(int id, [FromBody] UpdateDiscountRuleRequest request)
    {
        try
        {
            await _discountRuleService.UpdateAsync(
                id,
                request.DaysBeforeExpiry,
                request.DiscountPercent,
                request.IsActive,
                request.Description ?? "",
                0);

            return Ok(ApiResult<bool>.Success(true, "Cập nhật quy tắc giảm giá thành công"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResult<bool>.Failure(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResult<bool>.Failure(ex.Message));
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResult<bool>>> DeleteRule(int id)
    {
        var deleted = await _discountRuleService.DeleteAsync(id);
        if (!deleted)
            return NotFound(ApiResult<bool>.Failure($"Không tìm thấy quy tắc giảm giá có ID = {id}"));

        return Ok(ApiResult<bool>.Success(true, "Xóa quy tắc giảm giá thành công"));
    }

    [HttpGet("expiring-products")]
    public async Task<ActionResult<ApiResult<IEnumerable<ExpiringProductDto>>>> GetExpiringProducts()
    {
        var products = await _discountRuleService.GetExpiringProductsAsync();
        return Ok(ApiResult<IEnumerable<ExpiringProductDto>>.Success(products, "Lấy danh sách hàng cận date xả hàng thành công"));
    }

    [HttpGet("check-discount/{query}")]
    public async Task<ActionResult<ApiResult<CheckProductExpiryDiscountResponse>>> CheckProductDiscount(string query)
    {
        var result = await _discountRuleService.CheckProductExpiryDiscountAsync(query);
        if (result == null)
            return NotFound(ApiResult<CheckProductExpiryDiscountResponse>.Failure($"Không tìm thấy sản phẩm '{query}'"));

        return Ok(ApiResult<CheckProductExpiryDiscountResponse>.Success(result));
    }
}
