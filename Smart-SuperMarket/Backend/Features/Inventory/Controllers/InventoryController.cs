using Microsoft.AspNetCore.Mvc;
using SmartSupermarket.Backend.Common.Results;

namespace SmartSupermarket.Backend.Features.Inventory.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    [HttpGet("{branchId}")]
    public async Task<IActionResult> GetInventoryByBranch(int branchId)
    {
        return Ok(ApiResult<string>.Success($"Inventory for Branch {branchId}"));
    }
}
