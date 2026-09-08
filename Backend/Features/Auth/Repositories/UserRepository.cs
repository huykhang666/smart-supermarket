using Microsoft.EntityFrameworkCore;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Domain.Enums;
using SmartSupermarket.Backend.Infrastructure.Persistence;

namespace SmartSupermarket.Backend.Features.Auth.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _dbContext;

    public UserRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<User?> GetByIdAsync(int userId)
    {
        return await _dbContext.Users
            .Include(u => u.CustomerProfile)
            .FirstOrDefaultAsync(u => u.UserId == userId);
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _dbContext.Users
            .Include(u => u.CustomerProfile)
            .FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<User?> GetByPhoneAsync(string phone)
    {
        return await _dbContext.Users
            .FirstOrDefaultAsync(u => u.PhoneNumber == phone);
    }

    public async Task<User?> GetByUsernameOrPhoneOrEmailAsync(string identifier)
    {
        return await _dbContext.Users
            .Include(u => u.CustomerProfile)
            .FirstOrDefaultAsync(u => u.Username == identifier 
                                  || u.Email == identifier 
                                  || u.PhoneNumber == identifier);
    }

    public async Task<IEnumerable<User>> GetAllStaffAsync(int? branchId = null)
    {
        var query = _dbContext.Users.AsQueryable();

        query = query.Where(u => u.Role == UserRole.Staff || u.Role == UserRole.Manager);

        if (branchId.HasValue)
        {
            query = query.Where(u => u.BranchId == branchId.Value);
        }

        return await query.ToListAsync();
    }

    public async Task<(IEnumerable<User> Items, int TotalCount)> GetUsersPagedAsync(
        UserRole? role, int? branchId, string? search, int page, int pageSize)
    {
        var query = _dbContext.Users.Include(u => u.CustomerProfile).AsQueryable();

        if (role.HasValue)
        {
            query = query.Where(u => u.Role == role.Value);
        }

        if (branchId.HasValue)
        {
            query = query.Where(u => u.BranchId == branchId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.Trim().ToLower();
            query = query.Where(u => u.Username.ToLower().Contains(searchLower) ||
                                     u.FullName.ToLower().Contains(searchLower) ||
                                     u.Email.ToLower().Contains(searchLower) ||
                                     (u.PhoneNumber != null && u.PhoneNumber.Contains(searchLower)));
        }

        int totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task AddUserAsync(User user)
    {
        await _dbContext.Users.AddAsync(user);
    }

    public async Task AddCustomerProfileAsync(Customer customer)
    {
        await _dbContext.Customers.AddAsync(customer);
    }

    public void UpdateUser(User user)
    {
        _dbContext.Users.Update(user);
    }

    public async Task SaveChangesAsync()
    {
        await _dbContext.SaveChangesAsync();
    }
}
