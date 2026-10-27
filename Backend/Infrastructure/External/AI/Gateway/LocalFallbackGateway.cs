using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SmartSupermarket.Backend.Infrastructure.External.AI.Gateway;

public class LocalFallbackGateway
{
    public Task<string> GenerateAsync(
        string prompt,
        string? systemInstruction = null,
        bool requireJson = false,
        CancellationToken cancellationToken = default)
    {
        var lowerPrompt = prompt?.ToLowerInvariant() ?? string.Empty;

        if (requireJson)
        {
            var fallbackObject = new
            {
                source = "LocalDeterministicRuleEngine",
                status = "Success",
                recommendation = "Cơm Trứng Chiên & Canh Rau Muống Luộc",
                estimatedBudget = 30800,
                options = new[]
                {
                    new
                    {
                        optionNumber = 1,
                        title = "Cơm Trứng Chiên & Canh Rau Muống Luộc",
                        price = 30800,
                        items = new[]
                        {
                            new { name = "Trứng gà Ba Huân (3 quả)", price = 11400, category = 9 },
                            new { name = "Rau muống nước VietGAP (500g)", price = 14000, category = 4 },
                            new { name = "Gia vị & chanh ớt", price = 5400, category = 5 }
                        }
                    },
                    new
                    {
                        optionNumber = 2,
                        title = "Mì Omachi Xúc Xích & Cải Ngọt Siêu Tốc",
                        price = 44300,
                        items = new[]
                        {
                            new { name = "Mì Omachi ngũ quả", price = 8500, category = 10 },
                            new { name = "Xúc xích Vissan", price = 6000, category = 9 },
                            new { name = "Cải ngọt VietGAP (500g)", price = 14000, category = 4 }
                        }
                    },
                    new
                    {
                        optionNumber = 3,
                        title = "Cá Hộp Ba Cô Gái & Dưa Leo Baby",
                        price = 28900,
                        items = new[]
                        {
                            new { name = "Cá nục sốt cà chua Ba Cô Gái", price = 18500, category = 10 },
                            new { name = "Dưa leo baby Đà Lạt", price = 7500, category = 4 }
                        }
                    }
                }
            };

            return Task.FromResult(JsonSerializer.Serialize(fallbackObject));
        }

        if (lowerPrompt.Contains("chào") || lowerPrompt.Contains("hello") || lowerPrompt.Contains("hi"))
        {
            return Task.FromResult(
                "Chào bạn! Mình là Đầu Bếp & Trợ Lý Đi Chợ Thông Minh của Smart SuperMarket 🛒.\n\n" +
                "Mình có thể gợi ý các mâm cơm gia đình hàng ngày chuẩn vị (theo cẩm nang Cookpad), tư vấn mẹo chế biến và cân đối ngân sách từ 30k đến 500k.\n\n" +
                "Hôm nay bạn muốn nấu món gì hay cần mình gợi ý mâm cơm ngon cho gia đình ạ?");
        }

        return Task.FromResult(
            "Dạ, theo yêu cầu của bạn, mình đã cân đối và thiết kế 3 mâm cơm chuẩn vị theo cẩm nang Cookpad:\n\n" +
            "🍲 Option 1: Cơm Trứng Chiên & Canh Rau Muống Luộc (~30,800 đ)\n" +
            "✨ Option 2: Mì Omachi Xúc Xích & Cải Ngọt Siêu Tốc (~44,300 đ)\n" +
            "🌿 Option 3: Cá Hộp Ba Cô Gái & Dưa Leo Baby (~28,900 đ)\n\n" +
            "👉 Bạn có thể xem chi tiết nguyên liệu, giá tiền từng món ở các thẻ mâm cơm bên dưới, sau đó bấm 'Chọn mâm này' để nạp nhanh vào giỏ hàng nhé!");
    }
}
