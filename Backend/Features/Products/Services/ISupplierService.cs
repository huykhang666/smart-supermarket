using SmartSupermarket.Backend.Features.Products.DTOs;

namespace SmartSupermarket.Backend.Features.Products.Services;

public interface ISupplierService
{
    Task<SupplierDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<SupplierDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<SupplierPagedResult> GetPagedAsync(string? search, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    Task<SupplierDto> CreateAsync(CreateSupplierRequest request, CancellationToken cancellationToken = default);
    Task<SupplierDto> UpdateAsync(int id, UpdateSupplierRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
