using System.Collections.Generic;
using SmartSupermarket.Backend.Features.AI.PromptVersioning.Models;

namespace SmartSupermarket.Backend.Features.AI.PromptVersioning.Interfaces;

public interface IPromptVersionManager
{
    string ActiveVersion { get; }
    PromptVersionDto GetActivePrompt(PromptType type);
    PromptVersionDto? GetPromptByVersion(PromptType type, string version);
    List<PromptVersionDto> GetVersionHistory(PromptType type);
    void SetActiveVersion(string version);
    PromptRollbackResultDto RollbackToVersion(string targetVersion);
    void RegisterVersion(PromptVersionDto versionDto);
}
