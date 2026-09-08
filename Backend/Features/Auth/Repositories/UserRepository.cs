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
