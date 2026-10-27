using System.Collections.Generic;

namespace SmartSupermarket.Backend.Features.AI.DTOs.Common;

public enum PlanStepType
{
    RecipeLookup,
    InventoryCheck,
    PromotionScan,
    BudgetRebalance,
    CartOperation,
    LlmSynthesis
}

public class ExecutionStepDto
{
    public int Order { get; set; }
    public PlanStepType StepType { get; set; }
    public string TargetAgent { get; set; } = string.Empty;
    public string TargetTool { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public bool IsExecuted { get; set; }
}

public class ExecutionPlanDto
{
    public string PlanId { get; set; } = string.Empty;
    public IntentType IntentType { get; set; }
    public List<ExecutionStepDto> Steps { get; set; } = new();
    public bool RequiresParallelExecution { get; set; }
}
