using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartSupermarket.Backend.Features.AI.DTOs.Common;

namespace SmartSupermarket.Backend.Features.AI.Services.Knowledge.Interfaces;

public interface IKnowledgeRetrievalService
{
    Task<KnowledgeQueryResultDto> RetrieveAsync(string query, int maxResults = 3, CancellationToken cancellationToken = default);
    Task<KnowledgeChunkDto?> GetExactMatchFaqAsync(string question, CancellationToken cancellationToken = default);
    Task<string> GroundPromptWithKnowledgeAsync(string query, CancellationToken cancellationToken = default);
}
