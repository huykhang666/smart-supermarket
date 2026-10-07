using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Domain.Enums;
using SmartSupermarket.Backend.Infrastructure.Security;

namespace SmartSupermarket.Backend.Infrastructure.Persistence;

public class ProductSeedItem
{
    public int Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public decimal CostPrice { get; set; }
    public decimal Price { get; set; }
    public decimal DiscountPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public string Unit { get; set; } = string.Empty;
    public int Stock { get; set; }
    public string? StorageLocation { get; set; }
    public string? ImageUrl { get; set; }
    public string? Description { get; set; }
    public int Status { get; set; } = 1;
    public DateTime? ManufacturingDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
}

public static class DbInitializer
{
    public static string ResolveSeedFilePath()
    {
        var candidatePaths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Data", "products_seed.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "Backend", "Data", "products_seed.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "Data", "products_seed.json"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "products_seed.json"),
            @"D:\Laptrinhtrucquan\Backend\Data\products_seed.json"
        };

        foreach (var path in candidatePaths)
        {
            if (File.Exists(path))
                return path;
        }

        return candidatePaths[0];
    }

    public static async Task SeedDataAsync(AppDbContext dbContext, PasswordHasher passwordHasher)
    {
        // 1. Ensure DDL Tables & EF Migration History sync first
        await EnsureTablesCreatedAsync(dbContext);

        try
        {
            await dbContext.Database.MigrateAsync();
        }
        catch { }

        // 2. Keep system default user accounts for login (Admin, Manager, Staff)
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

        // 3. Seed 10 Categories lookup catalog
        var defaultCategories = new[]
        {
            new Category { CategoryName = "Nước giải khát & Đồ uống", Slug = "nuoc-giai-khat-do-uong", OrderIndex = 1, Status = 1, CreatedAt = DateTime.UtcNow },
            new Category { CategoryName = "Sữa & Sản phẩm từ sữa", Slug = "sua-san-pham-tu-sua", OrderIndex = 2, Status = 1, CreatedAt = DateTime.UtcNow },
            new Category { CategoryName = "Bánh kẹo & Ăn vặt", Slug = "banh-keo-an-vat", OrderIndex = 3, Status = 1, CreatedAt = DateTime.UtcNow },
            new Category { CategoryName = "Rau củ quả tươi", Slug = "rau-cu-qua-tuoi", OrderIndex = 4, Status = 1, CreatedAt = DateTime.UtcNow },
            new Category { CategoryName = "Gia vị & Đồ khô", Slug = "gia-vi-do-kho", OrderIndex = 5, Status = 1, CreatedAt = DateTime.UtcNow },
            new Category { CategoryName = "Đồ dùng gia đình & Nhà bếp", Slug = "do-dung-gia-dinh-nha-bep", OrderIndex = 6, Status = 1, CreatedAt = DateTime.UtcNow },
            new Category { CategoryName = "Hóa phẩm & Giặt xả", Slug = "hoa-pham-giat-xa", OrderIndex = 7, Status = 1, CreatedAt = DateTime.UtcNow },
            new Category { CategoryName = "Chăm sóc cá nhân", Slug = "cham-soc-ca-nhan", OrderIndex = 8, Status = 1, CreatedAt = DateTime.UtcNow },
            new Category { CategoryName = "Thịt, Thủy hải sản & Trứng", Slug = "thit-thuy-hai-san-trung", OrderIndex = 9, Status = 1, CreatedAt = DateTime.UtcNow },
            new Category { CategoryName = "Đồ ăn liền & Đóng hộp", Slug = "do-an-lien-dong-hop", OrderIndex = 10, Status = 1, CreatedAt = DateTime.UtcNow }
        };

        foreach (var cat in defaultCategories)
        {
            if (!await dbContext.Categories.AnyAsync(c => c.CategoryName == cat.CategoryName || c.Slug == cat.Slug))
            {
                await dbContext.Categories.AddAsync(cat);
            }
        }
        await dbContext.SaveChangesAsync();

        // 4. Seed 9 Suppliers lookup catalog
        var defaultSuppliers = new[]
        {
            new Supplier { SupplierCode = "SUP001", SupplierName = "Công ty TNHH NGK Coca-Cola Việt Nam", ContactPerson = "Nguyễn Văn A", PhoneNumber = "0901234567", Status = 1, CreatedAt = DateTime.UtcNow },
            new Supplier { SupplierCode = "SUP002", SupplierName = "Công ty CP Sữa Việt Nam (Vinamilk)", ContactPerson = "Trần Thị B", PhoneNumber = "0902345678", Status = 1, CreatedAt = DateTime.UtcNow },
            new Supplier { SupplierCode = "SUP003", SupplierName = "Công ty Cổ phần Mondelez Kinh Đô", ContactPerson = "Lê Văn C", PhoneNumber = "0903456789", Status = 1, CreatedAt = DateTime.UtcNow },
            new Supplier { SupplierCode = "SUP004", SupplierName = "Nhà cung cấp Nông sản Sạch Đà Lạt", ContactPerson = "Phạm Thị D", PhoneNumber = "0904567890", Status = 1, CreatedAt = DateTime.UtcNow },
            new Supplier { SupplierCode = "SUP005", SupplierName = "Tập đoàn Masan Consumer", ContactPerson = "Hoàng Văn E", PhoneNumber = "0905678901", Status = 1, CreatedAt = DateTime.UtcNow },
            new Supplier { SupplierCode = "SUP006", SupplierName = "Công ty TNHH Unilever Việt Nam", ContactPerson = "Vũ Thị F", PhoneNumber = "0906789012", Status = 1, CreatedAt = DateTime.UtcNow },
            new Supplier { SupplierCode = "SUP007", SupplierName = "Công ty Cổ phần Acecook Việt Nam", ContactPerson = "Đặng Văn G", PhoneNumber = "0907890123", Status = 1, CreatedAt = DateTime.UtcNow },
            new Supplier { SupplierCode = "SUP008", SupplierName = "Công ty Cổ phần Chăn nuôi C.P. Việt Nam", ContactPerson = "Nguyễn Văn Hùng", PhoneNumber = "0908888999", Status = 1, CreatedAt = DateTime.UtcNow },
            new Supplier { SupplierCode = "SUP009", SupplierName = "Công ty Cổ phần Ba Huân", ContactPerson = "Phạm Thị Loan", PhoneNumber = "0909999111", Status = 1, CreatedAt = DateTime.UtcNow }
        };

        foreach (var sup in defaultSuppliers)
        {
            if (!await dbContext.Suppliers.AnyAsync(s => s.SupplierCode == sup.SupplierCode || s.SupplierName == sup.SupplierName))
            {
                await dbContext.Suppliers.AddAsync(sup);
            }
        }
        await dbContext.SaveChangesAsync();

        // 5. Seed 200 Products and initial Inventory from products_seed.json
        if (!await dbContext.Products.AnyAsync())
        {
            var seedPath = ResolveSeedFilePath();
            if (File.Exists(seedPath))
            {
                var jsonContent = await File.ReadAllTextAsync(seedPath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var seedProducts = JsonSerializer.Deserialize<List<ProductSeedItem>>(jsonContent, options);

                if (seedProducts != null && seedProducts.Count > 0)
                {
                    var catLookup = await dbContext.Categories.ToDictionaryAsync(c => c.CategoryName, c => c.CategoryId);
                    var supLookup = await dbContext.Suppliers.ToDictionaryAsync(s => s.SupplierName, s => s.SupplierId);
                    var allCategories = await dbContext.Categories.ToListAsync();
                    var allSuppliers = await dbContext.Suppliers.ToListAsync();

                    int defaultCatId = allCategories.FirstOrDefault()?.CategoryId ?? 1;
                    int defaultSupId = allSuppliers.FirstOrDefault()?.SupplierId ?? 1;

                    foreach (var item in seedProducts)
                    {
                        int catId = defaultCatId;
                        if (!string.IsNullOrEmpty(item.CategoryName) && catLookup.TryGetValue(item.CategoryName, out var foundCatId))
                        {
                            catId = foundCatId;
                        }
                        else if (allCategories.Any(c => c.CategoryId == item.CategoryId))
                        {
                            catId = item.CategoryId;
                        }

                        int? supId = defaultSupId;
                        if (!string.IsNullOrEmpty(item.SupplierName) && supLookup.TryGetValue(item.SupplierName, out var foundSupId))
                        {
                            supId = foundSupId;
                        }
                        else if (allSuppliers.Any(s => s.SupplierId == item.SupplierId))
                        {
                            supId = item.SupplierId;
                        }

                        var product = new Product
                        {
                            ProductName = item.ProductName,
                            Barcode = item.Barcode,
                            CategoryId = catId,
                            SupplierId = supId,
                            Price = item.Price,
                            CostPrice = item.CostPrice > 0 ? item.CostPrice : null,
                            ImageUrl = item.ImageUrl,
                            Unit = string.IsNullOrWhiteSpace(item.Unit) ? "cái" : item.Unit,
                            Status = item.Status == 1 ? ProductStatus.Active : ProductStatus.Inactive,
                            ManufacturingDate = item.ManufacturingDate,
                            ExpiryDate = item.ExpiryDate,
                            CreatedAt = DateTime.UtcNow
                        };

                        await dbContext.Products.AddAsync(product);
                    }

                    await dbContext.SaveChangesAsync();

                    // Seed Inventories and Batches
                    var insertedProducts = await dbContext.Products.ToListAsync();
                    var seedDict = seedProducts
                        .GroupBy(s => s.Barcode)
                        .ToDictionary(g => g.Key, g => g.First());

                    foreach (var p in insertedProducts)
                    {
                        seedDict.TryGetValue(p.Barcode, out var sItem);
                        int stockQty = sItem?.Stock ?? 100;
                        string loc = sItem?.StorageLocation ?? "Kệ A1";
                        var mfg = sItem?.ManufacturingDate ?? DateTime.UtcNow.AddMonths(-1);
                        var exp = sItem?.ExpiryDate ?? DateTime.UtcNow.AddMonths(12);

                        var inv = new Inventory
                        {
                            ProductId = p.ProductId,
                            BranchId = 1,
                            QuantityOnHand = stockQty,
                            MinStockLevel = 10,
                            LastUpdated = DateTime.UtcNow
                        };
                        await dbContext.Inventories.AddAsync(inv);

                        var batch = new InventoryBatch
                        {
                            ProductId = p.ProductId,
                            BatchCode = $"LOT-{p.ProductId:D4}",
                            Quantity = stockQty,
                            ManufacturingDate = mfg,
                            ExpiryDate = exp,
                            StorageLocation = loc,
                            Status = "Good"
                        };
                        await dbContext.InventoryBatches.AddAsync(batch);
                    }

                    await dbContext.SaveChangesAsync();
                }
            }
        }
        else
        {
            // Backfill ManufacturingDate & ExpiryDate if null
            var seedPath = ResolveSeedFilePath();
            if (File.Exists(seedPath))
            {
                var jsonContent = await File.ReadAllTextAsync(seedPath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var seedProducts = JsonSerializer.Deserialize<List<ProductSeedItem>>(jsonContent, options);
                if (seedProducts != null && seedProducts.Count > 0)
                {
                    var seedDict = seedProducts.ToDictionary(s => s.Barcode, s => s);
                    var productsToUpdate = await dbContext.Products
                        .Where(p => p.ManufacturingDate == null || p.ExpiryDate == null)
                        .ToListAsync();

                    if (productsToUpdate.Any())
                    {
                        foreach (var p in productsToUpdate)
                        {
                            if (seedDict.TryGetValue(p.Barcode, out var sItem))
                            {
                                p.ManufacturingDate = sItem.ManufacturingDate;
                                p.ExpiryDate = sItem.ExpiryDate;
                            }
                        }
                        await dbContext.SaveChangesAsync();
                    }
                }
            }
        }

        // 6. Seed Promotions
        if (!await dbContext.Promotions.AnyAsync())
        {
            var defaultPromotions = new[]
            {
                new Promotion
                {
                    PromotionCode = "GIAM10K",
                    PromotionName = "Giảm 10.000đ cho đơn hàng từ 100.000đ",
                    Description = "Áp dụng cho mọi khách hàng thanh toán tại quầy POS",
                    DiscountType = "FixedAmount",
                    DiscountValue = 10000,
                    MinimumOrderAmount = 100000,
                    StartDate = DateTime.UtcNow.AddDays(-30),
                    EndDate = DateTime.UtcNow.AddDays(365),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Promotion
                {
                    PromotionCode = "GIAM10PERCENT",
                    PromotionName = "Giảm 10% tối đa 50.000đ cho đơn hàng từ 200.000đ",
                    Description = "Khuyến mãi chào hè siêu thị thông minh Smart SuperMarket",
                    DiscountType = "Percentage",
                    DiscountValue = 10,
                    MinimumOrderAmount = 200000,
                    MaximumDiscountAmount = 50000,
                    StartDate = DateTime.UtcNow.AddDays(-30),
                    EndDate = DateTime.UtcNow.AddDays(365),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Promotion
                {
                    PromotionCode = "VIPMEMBER",
                    PromotionName = "Ưu đãi khách hàng VIP giảm 15%",
                    Description = "Dành riêng cho khách hàng thân thiết có thẻ thành viên",
                    DiscountType = "Percentage",
                    DiscountValue = 15,
                    MinimumOrderAmount = 150000,
                    StartDate = DateTime.UtcNow.AddDays(-30),
                    EndDate = DateTime.UtcNow.AddDays(365),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            };

            await dbContext.Promotions.AddRangeAsync(defaultPromotions);
            await dbContext.SaveChangesAsync();
        }

        // 7. Seed DiscountRules (Hàng cận hạn sử dụng)
        if (!await dbContext.DiscountRules.AnyAsync())
        {
            var defaultRules = new[]
            {
                new DiscountRule { DaysBeforeExpiry = 30, DiscountPercent = 10, IsActive = true, Description = "Hàng còn 30 ngày hết hạn giảm 10%" },
                new DiscountRule { DaysBeforeExpiry = 15, DiscountPercent = 25, IsActive = true, Description = "Hàng còn 15 ngày hết hạn giảm 25%" },
                new DiscountRule { DaysBeforeExpiry = 7,  DiscountPercent = 50, IsActive = true, Description = "Hàng còn 7 ngày hết hạn giảm 50% thanh lý" }
            };

            await dbContext.DiscountRules.AddRangeAsync(defaultRules);
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
                DO $$
                BEGIN
                    CREATE TABLE IF NOT EXISTS ""__EFMigrationsHistory"" (
                        ""MigrationId"" VARCHAR(150) NOT NULL PRIMARY KEY,
                        ""ProductVersion"" VARCHAR(32) NOT NULL
                    );

                    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND LOWER(table_name) = 'users') THEN
                        INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"") VALUES
                            ('20260908161211_InitialCreate_UserCustomer', '9.0.0'),
                            ('20260908163346_AddRefreshTokenToUser', '9.0.0')
                        ON CONFLICT DO NOTHING;
                    END IF;

                    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND LOWER(table_name) = 'products') THEN
                        INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"") VALUES
                            ('20260909095510_InitialCreate_ProductCategorySupplier', '9.0.0'),
                            ('20260909113251_AddCategoryModuleDatabase', '9.0.0'),
                            ('20260909120108_UpdateCategoryModuleSchema', '9.0.0'),
                            ('20260909134300_AddSupplierAndProductSupplierModuleSchema', '9.0.0')
                        ON CONFLICT DO NOTHING;
                    END IF;

                    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND LOWER(table_name) IN ('discountrule', 'discountrules', 'inventories')) THEN
                        INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"") VALUES
                            ('20260911134808_AddInventoryAndImportModule', '9.0.0')
                        ON CONFLICT DO NOTHING;
                    END IF;
                END $$;

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
                    ""MembershipTier"" INT NOT NULL DEFAULT 1,
                    ""Gender"" SMALLINT NULL,
                    ""Address"" VARCHAR(255) NULL,
                    ""Status"" SMALLINT NOT NULL DEFAULT 1,
                    ""CreatedAt"" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ""UpdatedAt"" TIMESTAMPTZ NULL
                );

                ALTER TABLE ""Customers"" ADD COLUMN IF NOT EXISTS ""Gender"" SMALLINT NULL;
                ALTER TABLE ""Customers"" ADD COLUMN IF NOT EXISTS ""Address"" VARCHAR(255) NULL;
                ALTER TABLE ""Customers"" ADD COLUMN IF NOT EXISTS ""Status"" SMALLINT NOT NULL DEFAULT 1;
                ALTER TABLE ""Customers"" ADD COLUMN IF NOT EXISTS ""CreatedAt"" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP;
                ALTER TABLE ""Customers"" ADD COLUMN IF NOT EXISTS ""UpdatedAt"" TIMESTAMPTZ NULL;

                CREATE TABLE IF NOT EXISTS ""PointHistories"" (
                    ""PointHistoryId"" SERIAL PRIMARY KEY,
                    ""CustomerId"" INT NOT NULL,
                    ""OrderId"" INT NULL,
                    ""PointChange"" INT NOT NULL,
                    ""Type"" SMALLINT NOT NULL DEFAULT 1,
                    ""CreatedAt"" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
                );

                CREATE TABLE IF NOT EXISTS ""Vouchers"" (
                    ""VoucherId"" SERIAL PRIMARY KEY,
                    ""CustomerId"" INT NULL,
                    ""Code"" VARCHAR(50) NOT NULL,
                    ""DiscountAmount"" DECIMAL(18,2) NOT NULL DEFAULT 0,
                    ""MinimumOrderAmount"" DECIMAL(18,2) NOT NULL DEFAULT 0,
                    ""ExpiryDate"" TIMESTAMPTZ NOT NULL,
                    ""IsUsed"" BOOLEAN NOT NULL DEFAULT FALSE,
                    ""UsedAt"" TIMESTAMPTZ NULL,
                    ""OrderId"" INT NULL,
                    ""CreatedAt"" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
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

                ALTER TABLE ""Products"" ADD COLUMN IF NOT EXISTS ""ManufacturingDate"" TIMESTAMPTZ NULL;
                ALTER TABLE ""Products"" ADD COLUMN IF NOT EXISTS ""ExpiryDate"" TIMESTAMPTZ NULL;

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

                ALTER TABLE ""Orders"" ADD COLUMN IF NOT EXISTS ""PaymentMethod"" INT NOT NULL DEFAULT 1;
                ALTER TABLE ""Orders"" ADD COLUMN IF NOT EXISTS ""EmployeeId"" INT NOT NULL DEFAULT 1;
                ALTER TABLE ""Orders"" ADD COLUMN IF NOT EXISTS ""CustomerId"" INT NULL;
                ALTER TABLE ""Orders"" ADD COLUMN IF NOT EXISTS ""BranchId"" INT NOT NULL DEFAULT 1;
                ALTER TABLE ""Orders"" ADD COLUMN IF NOT EXISTS ""DiscountAmount"" DECIMAL(18,2) NOT NULL DEFAULT 0;
                ALTER TABLE ""Orders"" ADD COLUMN IF NOT EXISTS ""VoucherId"" INT NULL;
                ALTER TABLE ""Orders"" ADD COLUMN IF NOT EXISTS ""FinalAmount"" DECIMAL(18,2) NOT NULL DEFAULT 0;

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

                CREATE TABLE IF NOT EXISTS ""DiscountRule"" (
                    ""DiscountRuleId"" SERIAL PRIMARY KEY,
                    ""DaysBeforeExpiry"" INT NOT NULL,
                    ""DiscountPercent"" DECIMAL(18,2) NOT NULL DEFAULT 0,
                    ""IsActive"" BOOLEAN NOT NULL DEFAULT TRUE,
                    ""Description"" TEXT NULL
                );

                CREATE TABLE IF NOT EXISTS ""DiscountRules"" (
                    ""DiscountRuleId"" SERIAL PRIMARY KEY,
                    ""DaysBeforeExpiry"" INT NOT NULL,
                    ""DiscountPercent"" DECIMAL(18,2) NOT NULL DEFAULT 0,
                    ""IsActive"" BOOLEAN NOT NULL DEFAULT TRUE,
                    ""Description"" TEXT NULL
                );

                CREATE TABLE IF NOT EXISTS ""Inventories"" (
                    ""InventoryId"" SERIAL PRIMARY KEY,
                    ""ProductId"" INT NOT NULL,
                    ""BranchId"" INT NOT NULL DEFAULT 1,
                    ""QuantityOnHand"" INT NOT NULL DEFAULT 0,
                    ""MinStockLevel"" INT NOT NULL DEFAULT 10,
                    ""LastUpdated"" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
                );

                CREATE TABLE IF NOT EXISTS ""StockHistories"" (
                    ""StockHistoryId"" SERIAL PRIMARY KEY,
                    ""ProductId"" INT NOT NULL,
                    ""BranchId"" INT NOT NULL DEFAULT 1,
                    ""ChangeType"" INT NOT NULL,
                    ""QuantityChange"" INT NOT NULL,
                    ""QuantityBefore"" INT NOT NULL,
                    ""QuantityAfter"" INT NOT NULL,
                    ""ExpiryDate"" DATE NULL,
                    ""ReferenceId"" INT NULL,
                    ""Note"" TEXT NULL,
                    ""CreatedAt"" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ""CreatedByUserId"" INT NOT NULL DEFAULT 1
                );

                CREATE TABLE IF NOT EXISTS ""ImportReceipts"" (
                    ""ImportReceiptId"" SERIAL PRIMARY KEY,
                    ""ReceiptCode"" VARCHAR(100) NOT NULL,
                    ""SupplierId"" INT NOT NULL,
                    ""BranchId"" INT NOT NULL DEFAULT 1,
                    ""ImportedByUserId"" INT NOT NULL DEFAULT 1,
                    ""ImportDate"" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ""ConfirmedAt"" TIMESTAMPTZ NULL,
                    ""ConfirmedByUserId"" INT NULL,
                    ""TotalAmount"" DECIMAL(18,2) NOT NULL DEFAULT 0,
                    ""Status"" INT NOT NULL DEFAULT 1,
                    ""Note"" TEXT NULL
                );

                CREATE TABLE IF NOT EXISTS ""ImportDetails"" (
                    ""ImportDetailId"" SERIAL PRIMARY KEY,
                    ""ImportReceiptId"" INT NOT NULL,
                    ""ProductId"" INT NOT NULL,
                    ""Quantity"" INT NOT NULL DEFAULT 0,
                    ""CostPrice"" DECIMAL(18,2) NOT NULL DEFAULT 0,
                    ""ExpiryDate"" DATE NULL,
                    ""SubTotal"" DECIMAL(18,2) NOT NULL DEFAULT 0
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
