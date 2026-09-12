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
        try
        {
            await dbContext.Database.MigrateAsync();
        }
        catch { }

        await EnsureTablesCreatedAsync(dbContext);

        // 2. Clear/Reset full database (0 business data records)
        await ResetFullDatabaseAsync(dbContext);

        // 3. Keep system default user accounts for login (Admin, Manager, Staff)
        if (!await dbContext.Users.AnyAsync(u => u.Username == "admin"))
        {
            await dbContext.Users.AddAsync(new User
            {
                Username = "admin",
                PasswordHash = passwordHasher.HashPassword("AdminPassword123!"),
                FullName = "Quản trị viên Hệ thống",
                Email = "admin@smartmarket.vn",
                PhoneNumber = "0900000000",
                Role = UserRole.Admin,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow
            });
        }

        if (!await dbContext.Users.AnyAsync(u => u.Username == "staff"))
        {
            await dbContext.Users.AddAsync(new User
            {
                Username = "staff",
                PasswordHash = passwordHasher.HashPassword("StaffPassword123!"),
                FullName = "Nhân viên Thu ngân (Staff POS)",
                Email = "staff@smartmarket.vn",
                PhoneNumber = "0901111222",
                Role = UserRole.Staff,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow
            });
        }

        if (!await dbContext.Users.AnyAsync(u => u.Username == "manager"))
        {
            await dbContext.Users.AddAsync(new User
            {
                Username = "manager",
                PasswordHash = passwordHasher.HashPassword("ManagerPassword123!"),
                FullName = "Quản lý Cửa hàng (Store Manager)",
                Email = "manager@smartmarket.vn",
                PhoneNumber = "0903333444",
                Role = UserRole.Manager,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow
            });
        }

        await dbContext.SaveChangesAsync();

        // 4. Seed default Categories lookup catalog
        if (!await dbContext.Categories.AnyAsync())
        {
            var defaultCategories = new[]
            {
                new Category { CategoryName = "Nước giải khát", Slug = "nuoc-giai-khat", OrderIndex = 1, Status = 1, CreatedAt = DateTime.UtcNow },
                new Category { CategoryName = "Sữa & Sản phẩm từ sữa", Slug = "sua-san-pham-tu-sua", OrderIndex = 2, Status = 1, CreatedAt = DateTime.UtcNow },
                new Category { CategoryName = "Bánh kẹo & Ăn vặt", Slug = "banh-keo-an-vat", OrderIndex = 3, Status = 1, CreatedAt = DateTime.UtcNow },
                new Category { CategoryName = "Rau củ quả tươi", Slug = "rau-cu-qua-tuoi", OrderIndex = 4, Status = 1, CreatedAt = DateTime.UtcNow },
                new Category { CategoryName = "Gia vị & Đồ khô", Slug = "gia-vi-do-kho", OrderIndex = 5, Status = 1, CreatedAt = DateTime.UtcNow },
                new Category { CategoryName = "Đồ dùng gia đình", Slug = "do-dung-gia-dinh", OrderIndex = 6, Status = 1, CreatedAt = DateTime.UtcNow }
            };
            await dbContext.Categories.AddRangeAsync(defaultCategories);
            await dbContext.SaveChangesAsync();
        }

        // 5. Seed default Suppliers lookup catalog
        if (!await dbContext.Suppliers.AnyAsync())
        {
            var defaultSuppliers = new[]
            {
                new Supplier { SupplierCode = "SUP001", SupplierName = "Công ty TNHH NGK Coca-Cola Việt Nam", ContactPerson = "Nguyễn Văn A", PhoneNumber = "0901234567", Status = 1, CreatedAt = DateTime.UtcNow },
                new Supplier { SupplierCode = "SUP002", SupplierName = "Công ty CP Sữa Việt Nam (Vinamilk)", ContactPerson = "Trần Thị B", PhoneNumber = "0902345678", Status = 1, CreatedAt = DateTime.UtcNow },
                new Supplier { SupplierCode = "SUP003", SupplierName = "Công ty Cổ phần Mondelez Kinh Đô", ContactPerson = "Lê Văn C", PhoneNumber = "0903456789", Status = 1, CreatedAt = DateTime.UtcNow },
                new Supplier { SupplierCode = "SUP004", SupplierName = "Nhà cung cấp Nông sản Sạch Đà Lạt", ContactPerson = "Phạm Thị D", PhoneNumber = "0904567890", Status = 1, CreatedAt = DateTime.UtcNow }
            };
            await dbContext.Suppliers.AddRangeAsync(defaultSuppliers);
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

        // 12. Seed sample Promotions / Vouchers
        if (!await dbContext.Promotions.AnyAsync())
        {
            var now = DateTime.UtcNow;
            var samplePromotions = new[]
            {
                new Promotion
                {
                    PromotionCode = "KATQ10",
                    PromotionName = "Giảm 10% Đơn Hàng Siêu Thị",
                    Description = "Áp dụng cho đơn hàng từ 200.000 VNĐ",
                    DiscountType = "Percentage",
                    DiscountValue = 10m,
                    MinimumOrderAmount = 200000m,
                    MaximumDiscountAmount = 100000m,
                    StartDate = now.AddDays(-5),
                    EndDate = now.AddDays(60),
                    IsActive = true,
                    CreatedAt = now
                },
                new Promotion
                {
                    PromotionCode = "FREESHIP",
                    PromotionName = "Miễn Phí Giao Hàng Bán Lẻ",
                    Description = "Giảm 30.000 VNĐ cho đơn hàng từ 500.000 VNĐ",
                    DiscountType = "FixedAmount",
                    DiscountValue = 30000m,
                    MinimumOrderAmount = 500000m,
                    MaximumDiscountAmount = null,
                    StartDate = now.AddDays(-2),
                    EndDate = now.AddDays(90),
                    IsActive = true,
                    CreatedAt = now
                },
                new Promotion
                {
                    PromotionCode = "CHAOHETUAN",
                    PromotionName = "Voucher Chào Hè Tuần",
                    Description = "Giảm 50.000 VNĐ cho đơn hàng từ 1.000.000 VNĐ",
                    DiscountType = "FixedAmount",
                    DiscountValue = 50000m,
                    MinimumOrderAmount = 1000000m,
                    MaximumDiscountAmount = null,
                    StartDate = now.AddDays(-1),
                    EndDate = now.AddDays(30),
                    IsActive = true,
                    CreatedAt = now
                }
            };
            await dbContext.Promotions.AddRangeAsync(samplePromotions);
            await dbContext.SaveChangesAsync();
        }
    }

    public static async Task ResetFullDatabaseAsync(AppDbContext dbContext)
    {
        try
        {
            await EnsureTablesCreatedAsync(dbContext);

            string safeResetSql = @"
                DO $$
                DECLARE
                    rec RECORD;
                    users_tbl TEXT;
                BEGIN
                    FOR rec IN 
                        SELECT table_name 
                        FROM information_schema.tables 
                        WHERE table_schema = 'public' 
                          AND table_type = 'BASE TABLE'
                          AND LOWER(table_name) NOT IN ('users', '__efmigrationshistory')
                    LOOP
                        EXECUTE 'TRUNCATE TABLE ""' || rec.table_name || '"" RESTART IDENTITY CASCADE;';
                    END LOOP;

                    SELECT table_name INTO users_tbl 
                    FROM information_schema.tables 
                    WHERE table_schema = 'public' AND LOWER(table_name) = 'users' 
                    LIMIT 1;

                    IF users_tbl IS NOT NULL THEN
                        EXECUTE 'DELETE FROM ""' || users_tbl || '"" WHERE ""Username"" NOT IN (''admin'', ''staff'', ''manager'');';
                    END IF;
                END $$;
            ";

            await dbContext.Database.ExecuteSqlRawAsync(safeResetSql);
        }
        catch { }
    }

    private static async Task EnsureTablesCreatedAsync(AppDbContext dbContext)
    {
        try
        {
            string sql = @"
                CREATE TABLE IF NOT EXISTS ""Users"" (
                    ""UserId"" SERIAL PRIMARY KEY,
                    ""Username"" VARCHAR(100) NOT NULL,
                    ""PasswordHash"" TEXT NOT NULL,
                    ""FullName"" VARCHAR(150) NOT NULL,
                    ""Email"" VARCHAR(150) NOT NULL,
                    ""PhoneNumber"" VARCHAR(50) NULL,
                    ""DateOfBirth"" TIMESTAMPTZ NULL,
                    ""Role"" INT NOT NULL DEFAULT 0,
                    ""Status"" INT NOT NULL DEFAULT 1,
                    ""BranchId"" INT NULL,
                    ""RefreshToken"" TEXT NULL,
                    ""RefreshTokenExpiryTime"" TIMESTAMPTZ NULL,
                    ""CreatedAt"" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ""UpdatedAt"" TIMESTAMPTZ NULL
                );

                CREATE TABLE IF NOT EXISTS ""Categories"" (
                    ""CategoryId"" SERIAL PRIMARY KEY,
                    ""CategoryName"" VARCHAR(150) NOT NULL,
                    ""Slug"" VARCHAR(150) NOT NULL,
                    ""Description"" TEXT NULL,
                    ""ParentId"" INT NULL,
                    ""OrderIndex"" INT NOT NULL DEFAULT 0,
                    ""ImageUrl"" TEXT NULL,
                    ""Status"" SMALLINT NOT NULL DEFAULT 1,
                    ""CreatedAt"" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ""UpdatedAt"" TIMESTAMPTZ NULL
                );

                CREATE TABLE IF NOT EXISTS ""Suppliers"" (
                    ""SupplierId"" SERIAL PRIMARY KEY,
                    ""SupplierCode"" VARCHAR(50) NOT NULL,
                    ""SupplierName"" VARCHAR(150) NOT NULL,
                    ""ContactPerson"" VARCHAR(100) NULL,
                    ""PhoneNumber"" VARCHAR(50) NULL,
                    ""Email"" VARCHAR(150) NULL,
                    ""Address"" TEXT NULL,
                    ""TaxCode"" VARCHAR(50) NULL,
                    ""LogoUrl"" TEXT NULL,
                    ""Status"" SMALLINT NOT NULL DEFAULT 1,
                    ""CreatedAt"" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ""UpdatedAt"" TIMESTAMPTZ NULL
                );

                CREATE TABLE IF NOT EXISTS ""Customers"" (
                    ""CustomerId"" SERIAL PRIMARY KEY,
                    ""UserId"" INT NOT NULL,
                    ""LoyaltyPoints"" INT NOT NULL DEFAULT 0,
                    ""MembershipTier"" INT NOT NULL DEFAULT 1
                );

                CREATE TABLE IF NOT EXISTS ""Products"" (
                    ""ProductId"" SERIAL PRIMARY KEY,
                    ""ProductName"" VARCHAR(150) NOT NULL,
                    ""Barcode"" VARCHAR(100) NOT NULL,
                    ""CategoryId"" INT NOT NULL,
                    ""SupplierId"" INT NULL,
                    ""Price"" DECIMAL(18,2) NOT NULL DEFAULT 0,
                    ""CostPrice"" DECIMAL(18,2) NULL,
                    ""ImageUrl"" TEXT NULL,
                    ""Unit"" VARCHAR(50) NOT NULL DEFAULT 'cái',
                    ""Status"" INT NOT NULL DEFAULT 1,
                    ""CreatedAt"" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ""UpdatedAt"" TIMESTAMPTZ NULL
                );

                CREATE TABLE IF NOT EXISTS ""Orders"" (
                    ""OrderId"" SERIAL PRIMARY KEY,
                    ""EmployeeId"" INT NOT NULL DEFAULT 1,
                    ""CustomerId"" INT NULL,
                    ""BranchId"" INT NOT NULL DEFAULT 1,
                    ""OrderDate"" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ""TotalAmount"" DECIMAL(18,2) NOT NULL DEFAULT 0,
                    ""DiscountAmount"" DECIMAL(18,2) NOT NULL DEFAULT 0,
                    ""VoucherId"" INT NULL,
                    ""FinalAmount"" DECIMAL(18,2) NOT NULL DEFAULT 0,
                    ""Status"" INT NOT NULL DEFAULT 1,
                    ""PaymentMethod"" INT NOT NULL DEFAULT 1
                );

                CREATE TABLE IF NOT EXISTS ""Promotions"" (
                    ""PromotionId"" SERIAL PRIMARY KEY,
                    ""PromotionCode"" VARCHAR(50) NOT NULL,
                    ""PromotionName"" VARCHAR(200) NOT NULL,
                    ""Description"" TEXT NULL,
                    ""DiscountType"" VARCHAR(50) NOT NULL DEFAULT 'Percentage',
                    ""DiscountValue"" DECIMAL(18,2) NOT NULL DEFAULT 0,
                    ""MinimumOrderAmount"" DECIMAL(18,2) NOT NULL DEFAULT 0,
                    ""MaximumDiscountAmount"" DECIMAL(18,2) NULL,
                    ""StartDate"" TIMESTAMPTZ NOT NULL,
                    ""EndDate"" TIMESTAMPTZ NOT NULL,
                    ""IsActive"" BOOLEAN NOT NULL DEFAULT TRUE,
                    ""CreatedAt"" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ""UpdatedAt"" TIMESTAMPTZ NULL
                );

                CREATE TABLE IF NOT EXISTS ""OrderDetails"" (
                    ""OrderDetailId"" SERIAL PRIMARY KEY,
                    ""OrderId"" INT NOT NULL,
                    ""ProductId"" INT NOT NULL,
                    ""Quantity"" INT NOT NULL DEFAULT 1,
                    ""UnitPrice"" DECIMAL(18,2) NOT NULL DEFAULT 0,
                    ""SubTotal"" DECIMAL(18,2) NOT NULL DEFAULT 0
                );

                CREATE TABLE IF NOT EXISTS ""Shifts"" (
                    ""ShiftId"" SERIAL PRIMARY KEY,
                    ""Code"" VARCHAR(50) NOT NULL,
                    ""Name"" VARCHAR(100) NOT NULL,
                    ""StartTime"" INTERVAL NOT NULL,
                    ""EndTime"" INTERVAL NOT NULL,
                    ""BreakMinute"" INT NOT NULL DEFAULT 30,
                    ""Status"" VARCHAR(50) NOT NULL DEFAULT 'Active'
                );

                CREATE TABLE IF NOT EXISTS ""Employees"" (
                    ""EmployeeId"" SERIAL PRIMARY KEY,
                    ""EmployeeCode"" VARCHAR(50) NOT NULL,
                    ""UserId"" INT NOT NULL,
                    ""DepartmentId"" INT NULL,
                    ""ShiftId"" INT NULL,
                    ""Salary"" DECIMAL(18,2) NOT NULL DEFAULT 0,
                    ""HireDate"" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ""Department"" VARCHAR(100) NOT NULL DEFAULT 'Bán Hàng',
                    ""Status"" VARCHAR(50) NOT NULL DEFAULT 'Active'
                );

                CREATE TABLE IF NOT EXISTS ""Attendances"" (
                    ""AttendanceId"" SERIAL PRIMARY KEY,
                    ""EmployeeId"" INT NOT NULL,
                    ""ShiftId"" INT NULL,
                    ""CheckIn"" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ""CheckOut"" TIMESTAMPTZ NULL,
                    ""LateMinute"" INT NOT NULL DEFAULT 0,
                    ""WorkingMinute"" INT NOT NULL DEFAULT 0,
                    ""Status"" VARCHAR(50) NOT NULL DEFAULT 'OnTime'
                );

                CREATE TABLE IF NOT EXISTS ""InventoryBatches"" (
                    ""BatchId"" SERIAL PRIMARY KEY,
                    ""ProductId"" INT NOT NULL,
                    ""BatchCode"" VARCHAR(100) NOT NULL,
                    ""Quantity"" INT NOT NULL DEFAULT 0,
                    ""ManufacturingDate"" TIMESTAMPTZ NOT NULL,
                    ""ExpiryDate"" TIMESTAMPTZ NOT NULL,
                    ""Status"" VARCHAR(50) NOT NULL DEFAULT 'Good',
                    ""StorageLocation"" VARCHAR(100) NOT NULL DEFAULT 'Kệ A1'
                );

                CREATE TABLE IF NOT EXISTS ""ImportInvoices"" (
                    ""ImportInvoiceId"" SERIAL PRIMARY KEY,
                    ""InvoiceCode"" VARCHAR(100) NOT NULL,
                    ""SupplierId"" INT NOT NULL,
                    ""ImportDate"" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ""TotalAmount"" DECIMAL(18,2) NOT NULL DEFAULT 0,
                    ""Status"" VARCHAR(50) NOT NULL DEFAULT 'Completed',
                    ""CreatedBy"" VARCHAR(100) NOT NULL DEFAULT 'Admin'
                );

                CREATE TABLE IF NOT EXISTS ""ImportInvoiceItems"" (
                    ""ImportInvoiceItemId"" SERIAL PRIMARY KEY,
                    ""ImportInvoiceId"" INT NOT NULL,
                    ""ProductId"" INT NOT NULL,
                    ""Quantity"" INT NOT NULL DEFAULT 0,
                    ""CostPrice"" DECIMAL(18,2) NOT NULL DEFAULT 0,
                    ""ExpiryDate"" TIMESTAMPTZ NOT NULL
                );

                CREATE TABLE IF NOT EXISTS ""AuditLogs"" (
                    ""AuditLogId"" SERIAL PRIMARY KEY,
                    ""Username"" VARCHAR(100) NOT NULL,
                    ""Action"" VARCHAR(100) NOT NULL,
                    ""EntityName"" VARCHAR(100) NOT NULL,
                    ""Details"" TEXT NULL,
                    ""Timestamp"" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
                );
            ";

            await dbContext.Database.ExecuteSqlRawAsync(sql);
        }
        catch { }
    }
}
