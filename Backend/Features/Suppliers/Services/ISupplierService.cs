using SmartSupermarket.Backend.Features.Suppliers.DTOs;

namespace SmartSupermarket.Backend.Features.Suppliers.Services;

public interface ISupplierService
{
    Task<SupplierDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<SupplierDto?> GetByCodeAsync(string supplierCode, CancellationToken cancellationToken = default);
    Task<IEnumerable<SupplierDto>> GetAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<SupplierPagedResult> GetPagedAsync(string? search, byte? status, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    Task<IEnumerable<SupplierDto>> SearchAsync(string search, bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<IEnumerable<SupplierDropdownDto>> GetDropdownAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<ProductSupplierDto>> GetSuppliedProductsBySupplierIdAsync(int supplierId, CancellationToken cancellationToken = default);
    Task<SupplierStatisticsDto> GetSupplierStatisticsAsync(CancellationToken cancellationToken = default);
    Task<SupplierDto> CreateAsync(CreateSupplierRequest request, CancellationToken cancellationToken = default);
    Task<SupplierDto> UpdateAsync(int id, UpdateSupplierRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> SoftDeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<SupplierDto> RestoreAsync(int id, CancellationToken cancellationToken = default);
    Task<string> UploadLogoAsync(int id, string logoUrl, CancellationToken cancellationToken = default);
    Task<bool> LinkProductSupplierAsync(LinkProductSupplierRequest request, CancellationToken cancellationToken = default);
    Task<bool> UnlinkProductSupplierAsync(int productId, int supplierId, CancellationToken cancellationToken = default);
    Task<bool> UpdateProductSupplierLinkAsync(int productId, int supplierId, UpdateLinkRequest request, CancellationToken cancellationToken = default);
    Task<ImportSupplierResultDto> ImportFromJsonAsync(string jsonContent, CancellationToken cancellationToken = default);
    Task<ImportSupplierResultDto> ImportFromCsvAsync(Stream csvStream, CancellationToken cancellationToken = default);
}
