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

    public async Task<Customer?> GetActiveByPhoneAsync(string phone, int? excludeCustomerId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Customers
            .Include(c => c.User)
            .Where(c => (c.User.PhoneNumber == phone || c.User.Username == phone) && c.Status == 1);

        if (excludeCustomerId.HasValue)
        {
            query = query.Where(c => c.CustomerId != excludeCustomerId.Value);
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(IEnumerable<Customer> Items, int TotalCount)> GetPagedAsync(
        string? keyword, byte? status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Customers
            .Include(c => c.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            string trimmed = keyword.Trim();
            query = query.Where(c =>
                c.User.FullName.Contains(trimmed) ||
                (c.User.PhoneNumber != null && c.User.PhoneNumber.Contains(trimmed)) ||
                c.User.Email.Contains(trimmed) ||
                c.User.Username.Contains(trimmed));
        }

        if (status.HasValue)
        {
            query = query.Where(c => c.Status == status.Value);
        }

        int totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(c => c.CustomerId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<List<PointHistory>> GetPointHistoryAsync(int customerId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.PointHistories
            .Where(p => p.CustomerId == customerId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddPointHistoryAsync(PointHistory pointHistory, CancellationToken cancellationToken = default)
    {
        await _dbContext.PointHistories.AddAsync(pointHistory, cancellationToken);
    }

    public async Task<(IEnumerable<Order> Items, int TotalCount)> GetOrdersAsync(
        int customerId, int? branchId, byte? status, DateTime? startDate, DateTime? endDate, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Orders
            .Where(o => o.CustomerId == customerId)
            .AsQueryable();

        if (branchId.HasValue)
        {
            query = query.Where(o => o.BranchId == branchId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(o => (byte)o.Status == status.Value);
        }

        if (startDate.HasValue)
        {
            query = query.Where(o => o.OrderDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(o => o.OrderDate <= endDate.Value);
        }

        int totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(o => o.OrderDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<List<Voucher>> GetVouchersAsync(int customerId, string? statusFilter, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Vouchers
            .Where(v => v.CustomerId == customerId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            string filter = statusFilter.Trim().ToLowerInvariant();
            DateTime now = DateTime.UtcNow;

            if (filter == "available")
            {
                query = query.Where(v => !v.IsUsed && v.ExpiryDate >= now.Date);
            }
            else if (filter == "used")
            {
                query = query.Where(v => v.IsUsed);
            }
            else if (filter == "expired")
            {
                query = query.Where(v => !v.IsUsed && v.ExpiryDate < now.Date);
            }
        }

        return await query
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddVoucherAsync(Voucher voucher, CancellationToken cancellationToken = default)
    {
        await _dbContext.Vouchers.AddAsync(voucher, cancellationToken);
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
