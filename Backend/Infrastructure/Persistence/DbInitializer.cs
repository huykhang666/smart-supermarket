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

        // 3. Keep only Admin system user for login
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
            await dbContext.SaveChangesAsync();
        }

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
                        EXECUTE 'DELETE FROM ""' || users_tbl || '"" WHERE ""Username"" != ''admin'';';
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
