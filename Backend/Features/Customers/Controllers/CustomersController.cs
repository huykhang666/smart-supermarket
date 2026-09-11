using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartSupermarket.Backend.Infrastructure.Persistence;

namespace SmartSupermarket.Backend.Features.Customers.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly AppDbContext _context;

    public CustomersController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetCustomers()
    {
        var customers = await _context.Customers
            .Include(c => c.User)
            .Select(c => new
            {
                c.CustomerId,
                CustomerCode = "KH-" + c.CustomerId.ToString("D4"),
                FullName = c.User.FullName,
                Phone = c.User.PhoneNumber ?? c.User.Username,
                Points = c.LoyaltyPoints,
                Tier = c.MembershipTier == 3 ? "💎 Platinum VIP" : (c.MembershipTier == 2 ? "👑 VIP Gold" : (c.MembershipTier == 1 ? "🥈 Silver" : "🥉 Bronze")),
                VouchersCount = c.LoyaltyPoints > 1000 ? 3 : 1,
                TotalSpent = (c.LoyaltyPoints * 10000).ToString("N0")
            })
            .ToListAsync();

        return Ok(new { status = 200, data = customers });
    }
}
