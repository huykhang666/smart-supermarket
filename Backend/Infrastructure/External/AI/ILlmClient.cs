using System.Threading;
using System.Threading.Tasks;

namespace SmartSupermarket.Backend.Infrastructure.External.AI;

/// <summary>
/// Giao diện chuẩn trừu tượng hóa cho các nhà cung cấp mô hình ngôn ngữ lớn (LLM Provider).
/// Cho phép hoán đổi linh hoạt giữa Groq, Gemini hoặc OpenAI mà không làm thay đổi tầng nghiệp vụ.
/// </summary>
public interface ILlmClient
{
    /// <summary>
    /// Tên nhà cung cấp hiện tại (Groq, Gemini, OpenAI)
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Gửi prompt và nhận phản hồi văn bản thuần hoặc JSON có cấu trúc.
    /// </summary>
    /// <param name="prompt">Nội dung câu lệnh / bối cảnh dữ liệu</param>
    /// <param name="systemInstruction">Chỉ dẫn vai trò hệ thống (System Prompt)</param>
    /// <param name="requireJson">Yêu cầu AI ép kiểu trả về JSON chuẩn (JSON Mode)</param>
    /// <param name="cancellationToken">Hủy tác vụ nếu timeout</param>
    Task<string> GenerateAsync(
        string prompt,
        string? systemInstruction = null,
        bool requireJson = false,
        CancellationToken cancellationToken = default);
}
