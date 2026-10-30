using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Domain.Enums;
using SmartSupermarket.Backend.Features.AI.DTOs.Responses;
using SmartSupermarket.Backend.Infrastructure.Persistence;

namespace SmartSupermarket.Backend.Features.AI.Tools;

public class RetailTool : IRetailTool
{
    private readonly AppDbContext _dbContext;

    public RetailTool(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<Product>> SearchRetailProductsAsync(
        string keyword,
        string? category = null,
        decimal? maxPrice = null,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Products
            .Include(p => p.Category)
            .Where(p => p.Status == ProductStatus.Active);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var lower = keyword.ToLower().Trim();
            query = query.Where(p => p.ProductName.ToLower().Contains(lower));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var lowerCat = category.ToLower().Trim();
            query = query.Where(p => p.Category != null && p.Category.CategoryName.ToLower().Contains(lowerCat));
        }

        if (maxPrice.HasValue && maxPrice.Value > 0)
        {
            query = query.Where(p => p.Price <= maxPrice.Value);
        }

        return await query.OrderBy(p => p.Price)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public Task<RetailComparisonTableDto> GenerateCategoryComparisonAsync(
        string category,
        CancellationToken cancellationToken = default)
    {
        var lower = category?.ToLower().Trim() ?? string.Empty;

        var table = new RetailComparisonTableDto
        {
            Title = $"Bảng so sánh phân khúc ngành hàng: {category}",
            Headers = new List<string> { "Tiêu chí", "Tier 1: Tiết kiệm", "Tier 2: Đáng tiền nhất (Best Value)", "Tier 3: Bền bỉ / Hiệu năng" }
        };

        if (lower.Contains("dầu gội") || lower.Contains("gội") || lower.Contains("chăm sóc cá nhân"))
        {
            table.Rows = new List<ComparisonRowDto>
            {
                new() { Criteria = "Mức giá", Tier1CheapValue = "50.000 - 90.000 đ (Chai nhỏ)", Tier2StandardValue = "150.000 - 180.000 đ (Chai lớn 630g)", Tier3PremiumValue = "220.000 - 350.000 đ (Thảo dược/Dược liệu)" },
                new() { Criteria = "Công dụng chính", Tier1CheapValue = "Làm sạch tóc cơ bản, thơm nhẹ", Tier2StandardValue = "Sạch gàu, mát lạnh sâu, nuôi dưỡng tóc", Tier3PremiumValue = "Phục hồi hư tổn nặng, ngăn rụng tóc chuyên sâu" },
                new() { Criteria = "Đơn giá trung bình", Tier1CheapValue = "~250 đ/ml", Tier2StandardValue = "~270 đ/g (Dung tích lớn lợi hơn 30%)", Tier3PremiumValue = "~450 đ/ml" },
                new() { Criteria = "Khuyên dùng cho", Tier1CheapValue = "Sinh viên, người cần giải pháp tiết kiệm", Tier2StandardValue = "Gia đình dùng hàng ngày, giá tốt trên dung tích", Tier3PremiumValue = "Da đầu nhạy cảm, tóc uốn nhuộm hư tổn" }
            };
        }
        else if (lower.Contains("nước rửa chén") || lower.Contains("nước giặt") || lower.Contains("hóa phẩm"))
        {
            table.Rows = new List<ComparisonRowDto>
            {
                new() { Criteria = "Quy cách đóng gói", Tier1CheapValue = "Chai nhỏ 750g - 800ml", Tier2StandardValue = "Can lớn 3.6kg - 3.8kg (Tiết kiệm)", Tier3PremiumValue = "Túi / Can đậm đặc sinh học hữu cơ" },
                new() { Criteria = "Mức giá", Tier1CheapValue = "25.000 - 35.000 đ", Tier2StandardValue = "85.000 - 105.000 đ", Tier3PremiumValue = "140.000 - 210.000 đ" },
                new() { Criteria = "Đơn giá trên 1 kg", Tier1CheapValue = "~38.600 đ/kg", Tier2StandardValue = "~29.100 đ/kg (Tiết kiệm ~25% tiền)", Tier3PremiumValue = "~55.000 đ/kg" },
                new() { Criteria = "Khuyên dùng cho", Tier1CheapValue = "Người ở trọ, ít nấu nướng", Tier2StandardValue = "Gia đình 3-5 người, dùng thường xuyên", Tier3PremiumValue = "Nhà có trẻ nhỏ, da tay dễ kích ứng" }
            };
        }
        else if (lower.Contains("nồi") || lower.Contains("chảo") || lower.Contains("nhà bếp") || lower.Contains("gia dụng"))
        {
            table.Rows = new List<ComparisonRowDto>
            {
                new() { Criteria = "Mức giá", Tier1CheapValue = "120.000 - 170.000 đ", Tier2StandardValue = "165.000 - 250.000 đ", Tier3PremiumValue = "350.000 - 650.000 đ" },
                new() { Criteria = "Chất liệu & Lòng nồi", Tier1CheapValue = "Nhôm thường, chống dính mỏng", Tier2StandardValue = "Đáy từ vân đá, hợp kim nhôm đúc", Tier3PremiumValue = "Inox 304 nguyên khối hoặc tráng gốm cao cấp" },
                new() { Criteria = "Độ bền ước tính", Tier1CheapValue = "6 - 12 tháng", Tier2StandardValue = "2 - 3 năm (Nồi/Chảo đáy từ)", Tier3PremiumValue = "5+ năm bền bỉ" },
                new() { Criteria = "Khuyên dùng cho", Tier1CheapValue = "Nhu cầu ngắn hạn, ngân sách hẹp", Tier2StandardValue = "Gia đình muốn bền, giá hợp lý, dùng bếp từ", Tier3PremiumValue = "Đầu bếp gia đình, yêu cầu an toàn tuyệt đối" }
            };
        }
        else if (lower.Contains("giấy") || lower.Contains("khăn giấy"))
        {
            table.Rows = new List<ComparisonRowDto>
            {
                new() { Criteria = "Quy cách", Tier1CheapValue = "Lốc 6-10 cuộn 2 lớp tái chế nhẹ", Tier2StandardValue = "Lốc 10 cuộn 3 lớp bột giấy nguyên sinh", Tier3PremiumValue = "Lốc cao cấp 4 lớp không lõi dập hoa" },
                new() { Criteria = "Mức giá", Tier1CheapValue = "45.000 - 60.000 đ", Tier2StandardValue = "75.000 - 95.000 đ", Tier3PremiumValue = "110.000 - 145.000 đ" },
                new() { Criteria = "Đơn giá trên 1 cuộn", Tier1CheapValue = "~5.000 đ/cuộn", Tier2StandardValue = "~7.800 đ/cuộn (Mịn dai, không bụi)", Tier3PremiumValue = "~12.000 đ/cuộn" },
                new() { Criteria = "Khuyên dùng cho", Tier1CheapValue = "Sử dụng bình dân, nhà hàng quán ăn", Tier2StandardValue = "Gia đình tiêu chuẩn, êm dịu cho da", Tier3PremiumValue = "Chăm sóc làn da nhạy cảm cao cấp" }
            };
        }
        else if (lower.Contains("em bé") || lower.Contains("bé") || lower.Contains("trẻ em"))
        {
            table.Rows = new List<ComparisonRowDto>
            {
                new() { Criteria = "Đặc điểm an toàn", Tier1CheapValue = "Tiêu chuẩn cơ bản, không mùi", Tier2StandardValue = "Không cồn, sợi mềm kháng khuẩn, pH 5.5", Tier3PremiumValue = "Chiết xuất hữu cơ organic, chứng nhận da liễu" },
                new() { Criteria = "Mức giá", Tier1CheapValue = "25.000 - 35.000 đ", Tier2StandardValue = "32.000 - 65.000 đ", Tier3PremiumValue = "90.000 - 180.000 đ" },
                new() { Criteria = "Bảo vệ làn da bé", Tier1CheapValue = "Thích hợp bé trên 1 tuổi", Tier2StandardValue = "An toàn lành tính cho bé sơ sinh", Tier3PremiumValue = "Chuyên biệt cho da chàm, cực nhạy cảm" },
                new() { Criteria = "Lời khuyên", Tier1CheapValue = "Dùng lau chân tay, đồ chơi", Tier2StandardValue = "Khuyên chọn cho sinh hoạt hàng ngày (Best Value)", Tier3PremiumValue = "Dành cho bé có tiền sử dị ứng thời tiết/mỹ phẩm" }
            };
        }
        else
        {
            table.Rows = new List<ComparisonRowDto>
            {
                new() { Criteria = "Mức giá", Tier1CheapValue = "Thấp nhất danh mục", Tier2StandardValue = "Mức giá phổ thông hợp lý", Tier3PremiumValue = "Phân khúc cao cấp" },
                new() { Criteria = "Chất lượng / Dung tích", Tier1CheapValue = "Vừa đủ dùng, gói/chai nhỏ", Tier2StandardValue = "Dung tích lớn, p/p tối ưu nhất", Tier3PremiumValue = "Thành phần cao cấp, công năng vượt trội" },
                new() { Criteria = "Khuyên dùng", Tier1CheapValue = "Cần tiết kiệm tối đa", Tier2StandardValue = "Lựa chọn thông minh cho gia đình", Tier3PremiumValue = "Yêu cầu chất lượng & độ bền tối đa" }
            };
        }

        return Task.FromResult(table);
    }
}
