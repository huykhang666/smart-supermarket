using Microsoft.EntityFrameworkCore;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Infrastructure.Persistence;

namespace SmartSupermarket.Backend.Features.Customers.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly AppDbContext _dbContext;

    public CustomerRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Customer?> GetByIdAsync(int customerId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Customers
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.CustomerId == customerId, cancellationToken);
    }

    public async Task<Customer?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Customers
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
    }

    public async Task<Customer?> GetByPhoneAsync(string phone, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Customers
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.User.PhoneNumber == phone || c.User.Username == phone, cancellationToken);
    }

    public async Task<(IEnumerable<Customer> Items, int TotalCount)> GetPagedAsync(
        string? search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Customers
            .Include(c => c.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(c =>
                c.User.FullName.Contains(search) ||
                (c.User.PhoneNumber != null && c.User.PhoneNumber.Contains(search)) ||
                c.User.Username.Contains(search));
        }

        int totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(c => c.LoyaltyPoints)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task AddAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        await _dbContext.Customers.AddAsync(customer, cancellationToken);
    }

    public void Update(Customer customer)
    {
        _dbContext.Customers.Update(customer);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
