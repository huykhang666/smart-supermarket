using System.Collections.Generic;

namespace SmartSupermarket.Backend.Features.AI.DTOs.Requests;

public class AiCartInjectRequestDto
{
    public string SessionId { get; set; } = string.Empty;
    public List<int>? ProductIds { get; set; }
}
