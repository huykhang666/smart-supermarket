using Microsoft.EntityFrameworkCore;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Domain.Enums;
using SmartSupermarket.Backend.Infrastructure.Security;

namespace SmartSupermarket.Backend.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task SeedDataAsync(AppDbContext dbContext, PasswordHasher passwordHasher)
    {
        // 1. Ensure Database is created & migrations applied
        await dbContext.Database.MigrateAsync();

        // 2. Check if Admin user already exists
        if (!await dbContext.Users.AnyAsync(u => u.Role == UserRole.Admin))
        {
            var adminUser = new User
            {
                Username = "admin",
                PasswordHash = passwordHasher.HashPassword("AdminPassword123!"),
                FullName = "Quản trị viên Hệ thống",
                Email = "admin@smartmarket.vn",
                PhoneNumber = "0900000000",
                Role = UserRole.Admin,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            await dbContext.Users.AddAsync(adminUser);
        }

        // 3. Check if Manager user exists
        if (!await dbContext.Users.AnyAsync(u => u.Username == "manager_01"))
        {
            var managerUser = new User
            {
                Username = "manager_01",
                PasswordHash = passwordHasher.HashPassword("ManagerPassword123!"),
                FullName = "Nguyễn Văn Quản Lý",
                Email = "manager01@smartmarket.vn",
                PhoneNumber = "0901112233",
                Role = UserRole.Manager,
                BranchId = 1,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            await dbContext.Users.AddAsync(managerUser);
        }

        // 4. Check if Staff user exists
        if (!await dbContext.Users.AnyAsync(u => u.Username == "staff_pos_01"))
        {
            var staffUser = new User
            {
                Username = "staff_pos_01",
                PasswordHash = passwordHasher.HashPassword("StaffPassword123!"),
                FullName = "Trần Thị Thu Ngân",
                Email = "staff01@smartmarket.vn",
                PhoneNumber = "0904445566",
                Role = UserRole.Staff,
                BranchId = 1,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            await dbContext.Users.AddAsync(staffUser);
        }

        // 5. Check if Customer user exists
        if (!await dbContext.Users.AnyAsync(u => u.Username == "0988776655"))
        {
            var customerUser = new User
            {
                Username = "0988776655",
                PasswordHash = passwordHasher.HashPassword("CustomerPassword123!"),
                FullName = "Lê Văn Khách Hàng",
                Email = "customer01@gmail.com",
                PhoneNumber = "0988776655",
                Role = UserRole.Customer,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            var customerProfile = new Customer
            {
                User = customerUser,
                LoyaltyPoints = 150,
                MembershipTier = 1 // Bronze
            };

            await dbContext.Users.AddAsync(customerUser);
            await dbContext.Customers.AddAsync(customerProfile);
        }

        await dbContext.SaveChangesAsync();
    }
}
