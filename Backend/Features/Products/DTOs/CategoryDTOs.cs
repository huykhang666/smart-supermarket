namespace SmartSupermarket.Backend.Features.Products.DTOs;

public class CategoryDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ProductCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CategoryDropdownDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
}

public class CategoryTreeDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ProductCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateCategoryRequest
{
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateCategoryRequest
{
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class MoveCategoryRequest
{
    public int SourceCategoryId { get; set; }
    public int TargetCategoryId { get; set; }
}

public class CategoryPagedResult
{
    public IEnumerable<CategoryDto> Items { get; set; } = new List<CategoryDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
