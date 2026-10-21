using System;
using System.Collections.Generic;

namespace SmartSupermarket.Backend.Features.Categories.DTOs.Responses;

public class CategoryDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? ParentId { get; set; }
    public string? ParentName { get; set; }
    public int OrderIndex { get; set; }
    public string? ImageUrl { get; set; }
    public byte Status { get; set; } = 1;
    public string StatusName => Status switch
    {
        1 => "Active",
        2 => "Inactive",
        _ => "Unknown"
    };
    public int ProductCount { get; set; }
    public int SubCategoryCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CategoryDropdownDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public int? ParentId { get; set; }
    public int OrderIndex { get; set; }
}

public class CategoryTreeDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? ParentId { get; set; }
    public int OrderIndex { get; set; }
    public string? ImageUrl { get; set; }
    public byte Status { get; set; }
    public int ProductCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<CategoryTreeDto> SubCategories { get; set; } = new();
}

public class CategoryPagedResult
{
    public IEnumerable<CategoryDto> Items { get; set; } = new List<CategoryDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
