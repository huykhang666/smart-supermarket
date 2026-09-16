using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Features.Inventory.DTOs;
using SmartSupermarket.Backend.Infrastructure.Persistence;

namespace SmartSupermarket.Backend.Features.Inventory.Services;

public class StockAuditService : IStockAuditService
{
    private readonly AppDbContext _context;

    public StockAuditService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<StockAuditDTO> CreateAuditAsync(CreateStockAuditRequest request)
    {
        var audit = new StockAudit
        {
            CreatedBy = request.CreatedBy,
            Zone = request.Zone,
            Notes = request.Notes,
            Status = "Draft",
            AuditDate = DateTime.UtcNow
        };

        _context.StockAudits.Add(audit);
        await _context.SaveChangesAsync();

        return await GetAuditAsync(audit.Id);
    }

    public async Task<StockAuditDTO> SaveAuditDetailAsync(Guid auditId, SaveAuditDetailRequest request)
    {
        var audit = await _context.StockAudits
            .Include(a => a.Details)
            .FirstOrDefaultAsync(a => a.Id == auditId);
            
        if (audit == null) throw new Exception("Audit not found.");
        if (audit.Status == "Completed") throw new Exception("Cannot modify a completed audit.");

        var product = await _context.Products.FirstOrDefaultAsync(p => p.Barcode == request.Barcode);
        if (product == null) throw new Exception("Product not found with given barcode.");

        var systemQty = await _context.InventoryBatches
            .Where(b => b.ProductId == product.ProductId)
            .SumAsync(b => (int?)b.Quantity) ?? 0;

        var existingDetail = audit.Details.FirstOrDefault(d => d.Barcode == request.Barcode);
        
        if (existingDetail != null)
        {
            existingDetail.ActualQty = request.ActualQty;
            existingDetail.SystemQty = systemQty;
        }
        else
        {
            audit.Details.Add(new StockAuditDetail
            {
                StockAuditId = auditId,
                Barcode = request.Barcode,
                SystemQty = systemQty,
                ActualQty = request.ActualQty
            });
        }

        await _context.SaveChangesAsync();
        return await GetAuditAsync(auditId);
    }

    public async Task<StockAuditDTO> CompleteAuditAsync(Guid auditId)
    {
        var audit = await _context.StockAudits.FindAsync(auditId);
        if (audit == null) throw new Exception("Audit not found.");
        
        audit.Status = "Completed";
        
        _context.AuditLogs.Add(new AuditLog
        {
            Action = "COMPLETE_STOCK_AUDIT",
            Details = $"Hoàn tất kiểm kê kho. ID: {auditId}",
            Username = audit.CreatedBy,
            EntityName = "StockAudit",
            Timestamp = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        return await GetAuditAsync(auditId);
    }

    public async Task<StockAuditDTO> GetAuditAsync(Guid auditId)
    {
        var audit = await _context.StockAudits
            .Include(a => a.Details)
            .FirstOrDefaultAsync(a => a.Id == auditId);
            
        if (audit == null) throw new Exception("Audit not found.");

        var barcodes = audit.Details.Select(d => d.Barcode).Distinct().ToList();
        var products = await _context.Products
            .Where(p => barcodes.Contains(p.Barcode))
            .ToDictionaryAsync(p => p.Barcode, p => p.ProductName);
        
        return new StockAuditDTO
        {
            Id = audit.Id,
            AuditDate = audit.AuditDate,
            CreatedBy = audit.CreatedBy,
            Zone = audit.Zone,
            Status = audit.Status,
            Notes = audit.Notes,
            Details = audit.Details.Select(d => new StockAuditDetailDTO
            {
                Id = d.Id,
                Barcode = d.Barcode,
                ProductName = products.ContainsKey(d.Barcode) ? products[d.Barcode] : "Unknown",
                SystemQty = d.SystemQty,
                ActualQty = d.ActualQty,
                VarianceQty = d.VarianceQty
            }).ToList()
        };
    }
}
