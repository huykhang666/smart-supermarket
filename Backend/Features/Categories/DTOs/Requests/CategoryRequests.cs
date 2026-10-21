namespace SmartSupermarket.Backend.Features.Categories.DTOs.Requests;

public class CreateCategoryRequest
{
    public string CategoryName { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public int? ParentId { get; set; }
    public int OrderIndex { get; set; } = 0;
    public string? ImageUrl { get; set; }
    public byte Status { get; set; } = 1;
}

public class UpdateCategoryRequest
{
    public string CategoryName { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public int? ParentId { get; set; }
    public int OrderIndex { get; set; }
    public string? ImageUrl { get; set; }
    public byte Status { get; set; } = 1;
}

public class MoveCategoryRequest
{
    public int SourceCategoryId { get; set; }
    public int TargetCategoryId { get; set; }
}

public class MoveParentRequest
{
    public int? ParentId { get; set; }
}

public class ReorderCategoryRequest
{
    public int OrderIndex { get; set; }
}
