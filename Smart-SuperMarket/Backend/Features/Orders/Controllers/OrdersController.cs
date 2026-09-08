using Microsoft.AspNetCore.Mvc;
using SmartSupermarket.Backend.Common.Results;

namespace SmartSupermarket.Backend.Features.Orders.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateOrder()
    {
        return Ok(ApiResult<string>.Success("Order Created"));
    }
}
