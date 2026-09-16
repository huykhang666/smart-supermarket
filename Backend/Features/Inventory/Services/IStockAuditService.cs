using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SmartSupermarket.Backend.Features.Inventory.DTOs;

namespace SmartSupermarket.Backend.Features.Inventory.Services;

public interface IStockAuditService
{
    Task<StockAuditDTO> CreateAuditAsync(CreateStockAuditRequest request);
    Task<StockAuditDTO> SaveAuditDetailAsync(Guid auditId, SaveAuditDetailRequest request);
    Task<StockAuditDTO> CompleteAuditAsync(Guid auditId);
    Task<StockAuditDTO> GetAuditAsync(Guid auditId);
}
