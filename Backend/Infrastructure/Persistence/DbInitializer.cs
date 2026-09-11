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

        // 6. Check if Categories exist
        if (!await dbContext.Categories.AnyAsync())
        {
            var beverageCat = new Category { CategoryName = "Nước giải khát", Slug = "nuoc-giai-khat", Description = "Các loại nước ngọt, nước suối, trà, cà phê đóng lon/chai", OrderIndex = 1, Status = 1, CreatedAt = DateTime.UtcNow };
            var dairyCat = new Category { CategoryName = "Sữa & Sản phẩm từ sữa", Slug = "sua-san-pham-tu-sua", Description = "Sữa tươi, sữa chua, phô mai, bơ", OrderIndex = 2, Status = 1, CreatedAt = DateTime.UtcNow };
            var snackCat = new Category { CategoryName = "Bánh kẹo & Ăn vặt", Slug = "banh-keo-an-vat", Description = "Các loại bánh quy, snack, kẹo", OrderIndex = 3, Status = 1, CreatedAt = DateTime.UtcNow };

            await dbContext.Categories.AddRangeAsync(beverageCat, dairyCat, snackCat);
            await dbContext.SaveChangesAsync();
        }

        // 7. Check if Suppliers exist
        if (!await dbContext.Suppliers.AnyAsync())
        {
            var cocaSupplier = new Supplier 
            { 
                SupplierCode = "SUP001",
                SupplierName = "Công ty TNHH Coca-Cola Việt Nam", 
                ContactPerson = "Nguyễn Văn A", 
                PhoneNumber = "02838111222", 
                Email = "contact@cocacola.vn", 
                Address = "Xa lộ Hà Nội, P. Linh Trung, TP. Thủ Đức, TP.HCM",
                TaxCode = "0301234567",
                LogoUrl = "/images/suppliers/coca-logo.png",
                Status = 1,
                CreatedAt = DateTime.UtcNow 
            };
            var vinamilkSupplier = new Supplier 
            { 
                SupplierCode = "SUP002",
                SupplierName = "Công ty Cổ phần Sữa Vinamilk", 
                ContactPerson = "Trần Thị B", 
                PhoneNumber = "02854155555", 
                Email = "vinamilk@vinamilk.com.vn", 
                Address = "Số 10 Tân Trào, P. Tân Phú, Quận 7, TP.HCM",
                TaxCode = "0307654321",
                LogoUrl = "/images/suppliers/vinamilk-logo.png",
                Status = 1,
                CreatedAt = DateTime.UtcNow 
            };

            await dbContext.Suppliers.AddRangeAsync(cocaSupplier, vinamilkSupplier);
            await dbContext.SaveChangesAsync();
        }

        // 8. Check if Products exist
        if (!await dbContext.Products.AnyAsync())
        {
            var beverageCategory = await dbContext.Categories.FirstOrDefaultAsync(c => c.CategoryName == "Nước giải khát");
            var dairyCategory = await dbContext.Categories.FirstOrDefaultAsync(c => c.CategoryName == "Sữa & Sản phẩm từ sữa");

            var cocaSupplier = await dbContext.Suppliers.FirstOrDefaultAsync(s => s.SupplierName.Contains("Coca-Cola"));
            var vinamilkSupplier = await dbContext.Suppliers.FirstOrDefaultAsync(s => s.SupplierName.Contains("Vinamilk"));

            if (beverageCategory != null)
            {
                var product1 = new Product
                {
                    ProductName = "Nước ngọt Coca-Cola Lon 330ml",
                    Barcode = "8935001800012",
                    CategoryId = beverageCategory.CategoryId,
                    SupplierId = cocaSupplier?.SupplierId,
                    Price = 10000.00m,
                    CostPrice = 7500.00m,
                    ImageUrl = "/images/products/coca_330ml.jpg",
                    Unit = "lon",
                    Status = ProductStatus.Active,
                    CreatedAt = DateTime.UtcNow
                };

                await dbContext.Products.AddAsync(product1);
            }

            if (dairyCategory != null)
            {
                var product2 = new Product
                {
                    ProductName = "Sữa tươi tiệt trùng Vinamilk Có đường 1L",
                    Barcode = "8934673123456",
                    CategoryId = dairyCategory.CategoryId,
                    SupplierId = vinamilkSupplier?.SupplierId,
                    Price = 36000.00m,
                    CostPrice = 29000.00m,
                    ImageUrl = "/images/products/vinamilk_1l.jpg",
                    Unit = "hộp",
                    Status = ProductStatus.Active,
                    CreatedAt = DateTime.UtcNow
                };

                await dbContext.Products.AddAsync(product2);
            }

            await dbContext.SaveChangesAsync();
        }

        // 9. Seed ProductCategory join entity
        if (!await dbContext.ProductCategories.AnyAsync())
        {
            var products = await dbContext.Products.ToListAsync();
            foreach (var prod in products)
            {
                dbContext.ProductCategories.Add(new ProductCategory
                {
                    ProductId = prod.ProductId,
                    CategoryId = prod.CategoryId
                });
            }
            await dbContext.SaveChangesAsync();
        }

        // 10. Seed ProductSupplier join entity (Many-to-Many)
        if (!await dbContext.ProductSuppliers.AnyAsync())
        {
            var products = await dbContext.Products.ToListAsync();
            var cocaSupplier = await dbContext.Suppliers.FirstOrDefaultAsync(s => s.SupplierName.Contains("Coca-Cola"));
            var vinamilkSupplier = await dbContext.Suppliers.FirstOrDefaultAsync(s => s.SupplierName.Contains("Vinamilk"));

            foreach (var prod in products)
            {
                if (prod.ProductName.Contains("Coca-Cola") && cocaSupplier != null)
                {
                    dbContext.ProductSuppliers.Add(new ProductSupplier
                    {
                        ProductId = prod.ProductId,
                        SupplierId = cocaSupplier.SupplierId,
                        PurchasePrice = 7500.00m,
                        SupplierProductCode = "KO-330",
                        LeadTime = 2,
                        MinimumOrderQuantity = 50,
                        Rating = 4.80m,
                        IsDefault = true,
                        CreatedAt = DateTime.UtcNow
                    });
                }
                else if (prod.ProductName.Contains("Vinamilk") && vinamilkSupplier != null)
                {
                    dbContext.ProductSuppliers.Add(new ProductSupplier
                    {
                        ProductId = prod.ProductId,
                        SupplierId = vinamilkSupplier.SupplierId,
                        PurchasePrice = 29000.00m,
                        SupplierProductCode = "VNM-1L",
                        LeadTime = 1,
                        MinimumOrderQuantity = 20,
                        Rating = 4.90m,
                        IsDefault = true,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
            await dbContext.SaveChangesAsync();
        }

        // 11. Seed DiscountRule for Inventory Module
        if (!await dbContext.DiscountRules.AnyAsync())
        {
            var rule1 = new DiscountRule
            {
                DaysBeforeExpiry = 7,
                DiscountPercent = 50.00m,
                Description = "Còn ≤ 7 ngày HSD — Giảm 50%",
                IsActive = true
            };

            var rule2 = new DiscountRule
            {
                DaysBeforeExpiry = 15,
                DiscountPercent = 20.00m,
                Description = "Còn ≤ 15 ngày HSD — Giảm 20%",
                IsActive = true
            };

            await dbContext.DiscountRules.AddRangeAsync(rule1, rule2);
            await dbContext.SaveChangesAsync();
        }
    }
}
