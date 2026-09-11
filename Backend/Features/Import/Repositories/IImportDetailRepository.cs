using SmartSupermarket.Backend.Domain.Entities;

namespace SmartSupermarket.Backend.Features.Import.Repositories;

public interface IImportDetailRepository
{
    Task AddAsync(ImportDetail importDetail);
    Task<IEnumerable<ImportDetail>> GetByReceiptIdAsync(int importReceiptId);
    Task DeleteAsync(int importDetailId);
    Task SaveChangesAsync();
}
