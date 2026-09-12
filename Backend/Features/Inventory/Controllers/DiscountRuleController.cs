using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSupermarket.Backend.Common.Results;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Features.Inventory.Services;

namespace SmartSupermarket.Backend.Features.Inventory.Controllers;

[ApiController]
[Route("api/discount-rules")]
public class DiscountRuleController : ControllerBase
{
    private readonly IDiscountRuleService _discountRuleService;

    public DiscountRuleController(IDiscountRuleService discountRuleService)
    {
        _discountRuleService = discountRuleService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<IActionResult> GetAllRules()
    {
        var rules = await _discountRuleService.GetAllAsync();
        return Ok(ApiResult<IEnumerable<DiscountRule>>.Success(rules));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateRule(int id, [FromBody] DiscountRule rule)
    {
        try
        {
            await _discountRuleService.UpdateAsync(id, rule.DaysBeforeExpiry, rule.DiscountPercent, rule.IsActive, rule.Description ?? "", 0);
            return Ok(ApiResult<bool>.Success(true, "Cập nhật thành công."));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResult<bool>.Failure(ex.Message));
        }
    }
}
