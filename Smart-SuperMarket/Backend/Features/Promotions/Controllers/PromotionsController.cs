using Microsoft.AspNetCore.Mvc;
using SmartSupermarket.Backend.Common.Results;

namespace SmartSupermarket.Backend.Features.Promotions.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PromotionsController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetActivePromotions()
    {
        return Ok(ApiResult<string>.Success("Active Promotions"));
    }
}
