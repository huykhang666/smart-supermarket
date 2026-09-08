using Microsoft.AspNetCore.Mvc;
using SmartSupermarket.Backend.Common.Results;

namespace SmartSupermarket.Backend.Features.Customers.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    [HttpGet("{customerId}")]
    public async Task<IActionResult> GetCustomerProfile(int customerId)
    {
        return Ok(ApiResult<string>.Success($"Customer Profile for {customerId}"));
    }
}
