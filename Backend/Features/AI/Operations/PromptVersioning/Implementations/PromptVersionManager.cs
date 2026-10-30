using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using SmartSupermarket.Backend.Features.AI.PromptVersioning.Interfaces;
using SmartSupermarket.Backend.Features.AI.PromptVersioning.Models;

namespace SmartSupermarket.Backend.Features.AI.PromptVersioning.Implementations;

public class PromptVersionManager : IPromptVersionManager
{
    private string _activeVersion = "v4.0.0-optimized";
    private readonly ConcurrentDictionary<string, List<PromptVersionDto>> _versionStore = new(StringComparer.OrdinalIgnoreCase);

    public string ActiveVersion => _activeVersion;

    public PromptVersionManager()
    {
        InitializePromptVersions();
    }

    public PromptVersionDto GetActivePrompt(PromptType type)
    {
        var key = type.ToString();
        if (_versionStore.TryGetValue(key, out var list) && list.Count > 0)
        {
            var active = list.FirstOrDefault(v => v.Version.Equals(_activeVersion, StringComparison.OrdinalIgnoreCase) && v.IsActive);
            if (active != null) return active;

            // Fallback to latest
            return list.OrderByDescending(v => v.CreatedAt).First();
        }

        // Return empty fallback
        return new PromptVersionDto
        {
            Version = _activeVersion,
            Type = type,
            Content = string.Empty,
            IsActive = true
        };
    }

    public PromptVersionDto? GetPromptByVersion(PromptType type, string version)
    {
        var key = type.ToString();
        if (_versionStore.TryGetValue(key, out var list))
        {
            return list.FirstOrDefault(v => v.Version.Equals(version, StringComparison.OrdinalIgnoreCase));
        }
        return null;
    }

    public List<PromptVersionDto> GetVersionHistory(PromptType type)
    {
        var key = type.ToString();
        return _versionStore.TryGetValue(key, out var list)
            ? list.OrderByDescending(v => v.CreatedAt).ToList()
            : new List<PromptVersionDto>();
    }

    public void SetActiveVersion(string version)
    {
        if (string.IsNullOrWhiteSpace(version)) return;

        _activeVersion = version;
        foreach (var kvp in _versionStore)
        {
            foreach (var item in kvp.Value)
            {
                item.IsActive = item.Version.Equals(version, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    public PromptRollbackResultDto RollbackToVersion(string targetVersion)
    {
        var prev = _activeVersion;
        // Verify if version exists
        bool exists = _versionStore.Values.Any(list => list.Any(v => v.Version.Equals(targetVersion, StringComparison.OrdinalIgnoreCase)));
        if (!exists)
        {
            return new PromptRollbackResultDto
            {
                Success = false,
                PreviousVersion = prev,
                RestoredVersion = prev,
                Message = $"Phiên bản '{targetVersion}' không tồn tại trong kho lưu trữ prompt."
            };
        }

        SetActiveVersion(targetVersion);
        return new PromptRollbackResultDto
        {
            Success = true,
            PreviousVersion = prev,
            RestoredVersion = targetVersion,
            Message = $"Đã khôi phục thành công prompt về phiên bản {targetVersion}."
        };
    }

    public void RegisterVersion(PromptVersionDto versionDto)
    {
        if (versionDto == null) return;
        var key = versionDto.Type.ToString();
        var list = _versionStore.GetOrAdd(key, _ => new List<PromptVersionDto>());
        list.RemoveAll(v => v.Version.Equals(versionDto.Version, StringComparison.OrdinalIgnoreCase));
        list.Add(versionDto);
    }

    private void InitializePromptVersions()
    {
        // 1. System Identity (v3 vs v4-optimized)
        RegisterVersion(new PromptVersionDto
        {
            Version = "v3.0.0-rag",
            Type = PromptType.SystemIdentity,
            Content = "Bạn là Smart SuperMarket AI - Trợ lý Đi chợ & Đầu bếp thông minh. Xưng là 'mình' và gọi người dùng là 'bạn'. Tuyệt đối không bịa đặt dữ liệu.",
            Changelog = "Phiên bản tích hợp RAG kiểm chứng kiến thức Cookpad và siêu thị.",
            IsActive = false,
            EstimatedTokens = 55
        });

        RegisterVersion(new PromptVersionDto
        {
            Version = "v4.0.0-optimized",
            Type = PromptType.SystemIdentity,
            Content = "Bạn là Trợ lý Mua sắm & Đầu bếp Smart SuperMarket. Xưng 'mình', gọi 'bạn'. Trả lời súc tích, căn lề trái, ưu tiên kết luận trước, dùng dữ kiện thật 100%.",
            Changelog = "Tối ưu hóa độ dài 35%, tăng tốc độ phản hồi và tập trung vào Result First.",
            IsActive = true,
            EstimatedTokens = 38
        });

        // 2. Shopping Assistant Prompt
        RegisterVersion(new PromptVersionDto
        {
            Version = "v3.0.0-rag",
            Type = PromptType.ShoppingAssistant,
            Content = "Gợi ý mâm cơm theo số người và ngân sách. Xếp hạng: Khuyến nghị chính, Phương án thay thế, Tham khảo.",
            Changelog = "Quy tắc mâm cơm Phase 2.",
            IsActive = false,
            EstimatedTokens = 42
        });

        RegisterVersion(new PromptVersionDto
        {
            Version = "v4.0.0-optimized",
            Type = PromptType.ShoppingAssistant,
            Content = "Gợi ý mâm cơm chuẩn ngân sách & khẩu phần. Đưa ra 3 Option xếp hạng, tính đúng giá theo kho, không in lại bảng nguyên liệu dài trong văn bản.",
            Changelog = "Loại bỏ văn bản dư thừa, đồng bộ trực tiếp với thẻ mâm cơm giao diện.",
            IsActive = true,
            EstimatedTokens = 32
        });

        // 3. Retail Concierge Prompt
        RegisterVersion(new PromptVersionDto
        {
            Version = "v4.0.0-optimized",
            Type = PromptType.RetailConcierge,
            Content = "Tư vấn sản phẩm theo 4 Tier (Tiết kiệm, Best Value, Bền bỉ, Cao cấp). Luôn nêu rõ chỉ số giá trị (đ/kg, đ/g) và phân tích ưu/nhược điểm rõ ràng.",
            Changelog = "Chuẩn hóa định dạng giải thích phân khúc bán lẻ.",
            IsActive = true,
            EstimatedTokens = 45
        });

        // 4. Knowledge Grounding Prompt
        RegisterVersion(new PromptVersionDto
        {
            Version = "v4.0.0-optimized",
            Type = PromptType.KnowledgeGrounding,
            Content = "[KNOWLEDGE GROUNDING] Chỉ trả lời dựa trên tài liệu được cấp. Nếu câu hỏi ngoài phạm vi siêu thị hoặc không có dữ liệu, hãy lịch sự từ chối.",
            Changelog = "Chống hallucination tuyệt đối với zero-speculation.",
            IsActive = true,
            EstimatedTokens = 30
        });

        // 5. Fallback Prompt
        RegisterVersion(new PromptVersionDto
        {
            Version = "v4.0.0-optimized",
            Type = PromptType.Fallback,
            Content = "Dạ mình là Trợ lý Mua sắm & Đầu bếp của Smart SuperMarket. Mình chỉ có thể hỗ trợ các thông tin về nấu ăn, nguyên liệu, sản phẩm siêu thị và chính sách mua hàng thôi ạ!",
            Changelog = "Thông điệp an toàn khi người dùng hỏi ngoài phạm vi.",
            IsActive = true,
            EstimatedTokens = 35
        });

        // 6. Explanation Prompt
        RegisterVersion(new PromptVersionDto
        {
            Version = "v4.0.0-optimized",
            Type = PromptType.Explanation,
            Content = "Cấu trúc phản hồi chuẩn: 1. Đề xuất tốt nhất -> 2. So sánh các lựa chọn (Tier & Ưu/Nhược) -> 3. Lời khuyên tối ưu chi phí.",
            Changelog = "Cấu trúc XAI tối ưu hóa độ dài.",
            IsActive = true,
            EstimatedTokens = 35
        });
    }
}
