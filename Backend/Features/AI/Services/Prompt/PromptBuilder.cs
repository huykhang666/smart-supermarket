using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SmartSupermarket.Backend.Features.AI.Services.Prompt;

public class PromptBuilder : IPromptBuilder
{
    private readonly IHostEnvironment _environment;
    private readonly ILogger<PromptBuilder> _logger;
    private readonly ConcurrentDictionary<string, string> _promptCache = new();

    private const string BuiltInIdentity =
        "Bạn là Smart SuperMarket AI - Hệ thống Trợ lý Trí tuệ Nhân tạo phục vụ mua sắm và vận hành siêu thị thông minh.\n" +
        "Đại từ xưng hô bắt buộc: Luôn tự xưng là 'mình' và gọi người dùng là 'bạn'. Tuyệt đối không dùng 'em', 'tôi', 'chúng tôi', 'bot' hay 'AI'.\n" +
        "Tính cách: Thân thiện, tận tâm, chuẩn xác, am hiểu cẩm nang ẩm thực gia đình Việt Nam theo cẩm nang Cookpad.";

    private const string BuiltInRules =
        "1. Không đoán dữ liệu: Tuyệt đối không suy đoán tồn kho, hạn sử dụng hay giá cả.\n" +
        "2. Không tạo giá: Giá sản phẩm phải lấy từ công cụ dữ liệu, không tự ý làm tròn sai lệch.\n" +
        "3. Không tạo voucher: Không tự sinh ra mã giảm giá ngoài CSDL.\n" +
        "4. Không truy cập Database trực tiếp: Mọi dữ liệu phải qua Tool.\n" +
        "5. Cách ly danh mục: Nhóm 9 (thịt cá tươi), nhóm 4 (rau củ tươi), nhóm 10 (đồ hộp/ăn liền), nhóm 5 (gia vị).";

    private const string BuiltInStyle =
        "Định dạng đầu ra: Toàn bộ câu từ và danh sách mâm cơm phải được căn lề trái (text-align: left).\n" +
        "Không in lại danh sách chi tiết từng củ hành cọng rau trong text tin nhắn vì thẻ mâm cơm bên dưới đã hiển thị.\n" +
        "Luôn có lời kêu gọi hành động: nhắc người dùng bấm 'Chọn mâm này' để nạp vào giỏ hàng.";

    public PromptBuilder(IHostEnvironment environment, ILogger<PromptBuilder> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public async Task<string> BuildSystemPromptAsync(string role = "shopping", CancellationToken cancellationToken = default)
    {
        var identity = await GetLayerContentAsync("system/identity", cancellationToken);
        var rules = await GetLayerContentAsync("system/rules", cancellationToken);
        var style = await GetLayerContentAsync("system/style", cancellationToken);
        var rolePrompt = await GetLayerContentAsync($"shopping/{role}", cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine("[SYSTEM IDENTITY]");
        sb.AppendLine(identity);
        sb.AppendLine();
        sb.AppendLine("[STRICT CONSTRAINTS]");
        sb.AppendLine(rules);
        sb.AppendLine();
        sb.AppendLine("[UI & PRESENTATION STYLE]");
        sb.AppendLine(style);
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(rolePrompt))
        {
            sb.AppendLine("[ROLE INSTRUCTIONS]");
            sb.AppendLine(rolePrompt);
        }

        return sb.ToString();
    }

    public Task<string> BuildUserPromptWithContextAsync(
        string rawUserPrompt,
        string? historyContext = null,
        string? cartContext = null,
        CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(historyContext))
        {
            sb.AppendLine("[LỊCH SỬ ĐỐI THOẠI TRƯỚC ĐÓ]");
            sb.AppendLine(historyContext);
            sb.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(cartContext))
        {
            sb.AppendLine("[TRẠNG THÁI GIỎ HÀNG HIỆN TẠI]");
            sb.AppendLine(cartContext);
            sb.AppendLine();
        }

        sb.AppendLine("[YÊU CẦU HIỆN TẠI CỦA KHÁCH HÀNG]");
        sb.AppendLine(rawUserPrompt);

        return Task.FromResult(sb.ToString());
    }

    public async Task<string> GetLayerContentAsync(string layerKey, CancellationToken cancellationToken = default)
    {
        if (_promptCache.TryGetValue(layerKey, out var cached))
        {
            return cached;
        }

        var content = await TryLoadFromDiskAsync(layerKey, cancellationToken);
        if (string.IsNullOrWhiteSpace(content))
        {
            content = GetBuiltInFallback(layerKey);
        }

        _promptCache[layerKey] = content;
        return content;
    }

    private async Task<string?> TryLoadFromDiskAsync(string layerKey, CancellationToken cancellationToken)
    {
        try
        {
            var contentRoot = _environment.ContentRootPath;
            var solutionDir = Directory.GetParent(contentRoot)?.FullName ?? contentRoot;
            var candidatePaths = new[]
            {
                Path.Combine(solutionDir, "docs", "10_AI", "prompts", $"{layerKey}.md"),
                Path.Combine(contentRoot, "..", "docs", "10_AI", "prompts", $"{layerKey}.md"),
                Path.Combine(contentRoot, "Features", "AI", "Prompts", $"{layerKey}.md")
            };

            foreach (var path in candidatePaths)
            {
                if (File.Exists(path))
                {
                    return await File.ReadAllTextAsync(path, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "[PromptBuilder] Không đọc được prompt từ đĩa cho key: {Key}", layerKey);
        }

        return null;
    }

    private static string GetBuiltInFallback(string layerKey)
    {
        return layerKey.ToLowerInvariant() switch
        {
            "system/identity" => BuiltInIdentity,
            "system/rules" => BuiltInRules,
            "system/style" => BuiltInStyle,
            _ => string.Empty
        };
    }
}
