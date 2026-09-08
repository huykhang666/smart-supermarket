using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Domain.Enums;

namespace SmartSupermarket.Backend.Features.Auth.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int userId);
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByPhoneAsync(string phone);
    Task<User?> GetByUsernameOrPhoneOrEmailAsync(string identifier);
    Task<IEnumerable<User>> GetAllStaffAsync(int? branchId = null);
    Task<(IEnumerable<User> Items, int TotalCount)> GetUsersPagedAsync(UserRole? role, int? branchId, string? search, int page, int pageSize);
    Task AddUserAsync(User user);
    Task AddCustomerProfileAsync(Customer customer);
    void UpdateUser(User user);
    Task SaveChangesAsync();
}
