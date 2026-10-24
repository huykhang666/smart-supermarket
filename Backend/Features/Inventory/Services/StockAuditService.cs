using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Domain.Enums;
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
            CreatedBy = string.IsNullOrWhiteSpace(request.CreatedBy) ? "NV-Kho" : request.CreatedBy,
            Zone = string.IsNullOrWhiteSpace(request.Zone) ? "Tất cả kệ" : request.Zone,
            Notes = request.Notes ?? string.Empty,
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
            
        if (audit == null) throw new KeyNotFoundException("Không tìm thấy biên bản kiểm kê.");
        if (audit.Status == "Completed" || audit.Status == "Reconciled") 
            throw new InvalidOperationException($"Biên bản kiểm kê đang ở trạng thái '{audit.Status}', không thể chỉnh sửa.");

        var product = await _context.Products.FirstOrDefaultAsync(p => p.Barcode == request.Barcode);
        if (product == null) throw new KeyNotFoundException($"Không tìm thấy sản phẩm có mã barcode '{request.Barcode}'.");

        // Lấy số tồn hệ thống hiện tại từ bảng Inventory (chi nhánh 1), fallback sang InventoryBatches nếu chưa có
        var inv = await _context.Inventories.FirstOrDefaultAsync(i => i.ProductId == product.ProductId && i.BranchId == 1);
        int systemQty = inv != null 
            ? inv.QuantityOnHand 
            : (await _context.InventoryBatches.Where(b => b.ProductId == product.ProductId).SumAsync(b => (int?)b.Quantity) ?? 0);

        decimal unitPrice = product.CostPrice ?? product.Price;
        var existingDetail = audit.Details.FirstOrDefault(d => d.Barcode == request.Barcode);
        
        if (existingDetail != null)
        {
            existingDetail.ActualQty = request.ActualQty;
            existingDetail.SystemQty = systemQty;
            existingDetail.UnitPrice = unitPrice;
            if (!string.IsNullOrWhiteSpace(request.LossReason)) existingDetail.LossReason = request.LossReason;
            if (request.Notes != null) existingDetail.Notes = request.Notes;
        }
        else
        {
            var detail = new StockAuditDetail
            {
                StockAuditId = auditId,
                Barcode = request.Barcode,
                SystemQty = systemQty,
                ActualQty = request.ActualQty,
                UnitPrice = unitPrice,
                LossReason = request.LossReason,
                Notes = request.Notes ?? string.Empty
            };
            _context.StockAuditDetails.Add(detail);
        }

        await _context.SaveChangesAsync();
        return await GetAuditAsync(auditId);
    }

    public async Task<StockAuditDTO> DeleteAuditDetailAsync(Guid auditId, string barcode)
    {
        var audit = await _context.StockAudits
            .Include(a => a.Details)
            .FirstOrDefaultAsync(a => a.Id == auditId);

        if (audit == null) throw new KeyNotFoundException("Không tìm thấy biên bản kiểm kê.");
        if (audit.Status == "Completed" || audit.Status == "Reconciled")
            throw new InvalidOperationException($"Biên bản kiểm kê đang ở trạng thái '{audit.Status}', không thể chỉnh sửa.");

        var detail = audit.Details.FirstOrDefault(d => d.Barcode == barcode);
        if (detail != null)
        {
            _context.StockAuditDetails.Remove(detail);
            await _context.SaveChangesAsync();
        }

        return await GetAuditAsync(auditId);
    }


    public async Task<StockAuditDTO> CompleteAuditAsync(Guid auditId)
    {
        var audit = await _context.StockAudits
            .Include(a => a.Details)
            .FirstOrDefaultAsync(a => a.Id == auditId);
            
        if (audit == null) throw new KeyNotFoundException("Không tìm thấy biên bản kiểm kê.");
        if (audit.Status == "Completed" || audit.Status == "Reconciled") 
            throw new InvalidOperationException("Biên bản kiểm kê đã được chốt hoặc cân bằng kho từ trước.");

        if (audit.Details.Count > 0)
        {
            // Tự động gán lý do mặc định cho các dòng thiếu nếu chưa chọn
            foreach (var detail in audit.Details)
            {
                if (string.IsNullOrWhiteSpace(detail.LossReason) || detail.LossReason == "⚠️ Chưa chọn" || detail.LossReason == "Chưa chọn")
                {
                    detail.LossReason = detail.VarianceQty < 0 
                        ? "Thất thoát chưa rõ nguyên nhân" 
                        : (detail.VarianceQty > 0 ? "Thừa hàng" : "Khớp");
                }
            }

            // Cân bằng kho thực tế
            await ReconcileStockInternalAsync(audit, audit.CreatedBy, "COMPLETE_STOCK_AUDIT");
        }
        else
        {
            _context.AuditLogs.Add(new AuditLog
            {
                Action = "COMPLETE_STOCK_AUDIT",
                Details = $"Hoàn tất kiểm đếm biên bản kiểm kê kho. ID: {audit.Id}",
                Username = audit.CreatedBy,
                EntityName = "StockAudit",
                Timestamp = DateTime.UtcNow
            });
        }

        audit.Status = "Completed";
        audit.ReconciledAt = DateTime.UtcNow;
        audit.ReconciledBy = audit.CreatedBy;

        await _context.SaveChangesAsync();
        return await GetAuditAsync(auditId);
    }

    /// <summary>
    /// Thực hiện Cân Bằng Kho (Reconciliation):
    /// Cập nhật số tồn thực tế vào Inventory & InventoryBatch, ghi vết StockHistory cho từng mặt hàng chênh lệch.
    /// </summary>
    public async Task<StockAuditDTO> ReconcileAuditAsync(Guid auditId, ReconcileStockAuditRequest request)
    {
        var audit = await _context.StockAudits
            .Include(a => a.Details)
            .FirstOrDefaultAsync(a => a.Id == auditId);

        if (audit == null) throw new KeyNotFoundException("Không tìm thấy biên bản kiểm kê.");
        if (audit.Status == "Reconciled") throw new InvalidOperationException("Biên bản kiểm kê này đã được cân bằng kho trước đó.");
        if (audit.Details.Count == 0) throw new InvalidOperationException("Biên bản kiểm kê chưa có sản phẩm nào được kiểm đếm.");

        // Ràng buộc nghiệp vụ: Bắt buộc phân loại nguyên nhân cho tất cả các mặt hàng bị thiếu
        var unclassified = audit.Details
            .Where(d => d.VarianceQty < 0 && (string.IsNullOrWhiteSpace(d.LossReason) || d.LossReason == "⚠️ Chưa chọn" || d.LossReason == "Chưa chọn"))
            .ToList();
        if (unclassified.Any())
        {
            throw new InvalidOperationException($"Còn {unclassified.Count} mặt hàng bị thiếu chưa được phân loại nguyên nhân hao hụt. Vui lòng phân loại trước khi cân bằng kho.");
        }

        string reconciler = string.IsNullOrWhiteSpace(request.ReconciledBy) ? audit.CreatedBy : request.ReconciledBy;
        await ReconcileStockInternalAsync(audit, reconciler, "RECONCILE_STOCK_AUDIT");

        audit.Status = "Reconciled";
        audit.ReconciledAt = DateTime.UtcNow;
        audit.ReconciledBy = reconciler;
        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            audit.Notes = string.IsNullOrWhiteSpace(audit.Notes) 
                ? request.Notes 
                : $"{audit.Notes} | [Cân bằng]: {request.Notes}";
        }

        await _context.SaveChangesAsync();
        return await GetAuditAsync(auditId);
    }

    private async Task ReconcileStockInternalAsync(StockAudit audit, string actor, string actionName)
    {
        var isInMemory = _context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";
        var transaction = isInMemory ? null : await _context.Database.BeginTransactionAsync();
        try
        {
            foreach (var detail in audit.Details)
            {
                if (detail.VarianceQty == 0) continue;

                var product = await _context.Products.FirstOrDefaultAsync(p => p.Barcode == detail.Barcode);
                if (product == null) continue;

                // 1. Cập nhật bảng Inventory (Chi nhánh 1)
                var inventory = await _context.Inventories.FirstOrDefaultAsync(i => i.ProductId == product.ProductId && i.BranchId == 1);
                int qtyBefore = inventory?.QuantityOnHand ?? detail.SystemQty;
                int qtyAfter = detail.ActualQty;
                int qtyChange = detail.VarianceQty;

                if (inventory != null)
                {
                    inventory.QuantityOnHand = Math.Max(0, qtyAfter);
                    inventory.LastUpdated = DateTime.UtcNow;
                }
                else
                {
                    _context.Inventories.Add(new Domain.Entities.Inventory
                    {
                        ProductId = product.ProductId,
                        BranchId = 1,
                        QuantityOnHand = Math.Max(0, qtyAfter),
                        MinStockLevel = 10,
                        LastUpdated = DateTime.UtcNow
                    });
                }

                // 2. Cập nhật số lượng trong lô hàng InventoryBatch (Tuân thủ BR-INV-14 FEFO)
                var batches = await _context.InventoryBatches
                    .Where(b => b.ProductId == product.ProductId)
                    .ToListAsync();

                if (qtyChange < 0)
                {
                    // Giảm tồn (Hao hụt/Thất thoát): ưu tiên trừ vào lô cận date nhất trước
                    int remainingDeduct = Math.Abs(qtyChange);
                    foreach (var b in batches.OrderBy(b => b.ExpiryDate))
                    {
                        if (remainingDeduct <= 0) break;
                        int deduct = Math.Min(b.Quantity, remainingDeduct);
                        b.Quantity -= deduct;
                        remainingDeduct -= deduct;
                    }
                }
                else if (qtyChange > 0)
                {
                    // Tăng tồn (Thừa hàng): cộng vào lô mới nhất
                    var targetBatch = batches.OrderByDescending(b => b.BatchId).FirstOrDefault();
                    if (targetBatch != null)
                    {
                        targetBatch.Quantity += qtyChange;
                    }
                    else
                    {
                        _context.InventoryBatches.Add(new InventoryBatch
                        {
                            ProductId = product.ProductId,
                            BatchCode = $"LOT-{product.ProductId:D4}-AUDIT",
                            Quantity = qtyAfter,
                            ManufacturingDate = DateTime.UtcNow.AddMonths(-1),
                            ExpiryDate = DateTime.UtcNow.AddMonths(12),
                            StorageLocation = string.IsNullOrWhiteSpace(audit.Zone) ? "Kệ A1" : audit.Zone,
                            Status = "Good"
                        });
                    }
                }

                // 3. Ghi vết biến động kho StockHistory (BR-INV-21, BR-INV-26, BR-INV-27)
                bool isExpired = (detail.LossReason?.Contains("Expired", StringComparison.OrdinalIgnoreCase) == true)
                    || (detail.LossReason?.Contains("Hết hạn", StringComparison.OrdinalIgnoreCase) == true);

                string reason = string.IsNullOrWhiteSpace(detail.LossReason) ? "Điều chỉnh kiểm kê kho" : detail.LossReason;
                string historyNote = $"[Cân bằng kho đợt {audit.AuditDate:dd/MM/yyyy}] {reason}. Lệch: {(qtyChange > 0 ? "+" : "")}{qtyChange}. Ghi chú: {detail.Notes}".Trim();

                _context.StockHistories.Add(new StockHistory
                {
                    ProductId = product.ProductId,
                    BranchId = 1,
                    ChangeType = isExpired ? StockChangeType.Expired : StockChangeType.Adjustment,
                    QuantityBefore = qtyBefore,
                    QuantityChange = qtyChange,
                    QuantityAfter = qtyAfter,
                    Note = historyNote,
                    CreatedByUserId = 1 // User ID quản lý thực hiện cân bằng
                });
            }

            _context.AuditLogs.Add(new AuditLog
            {
                Action = actionName,
                Details = $"Cân bằng kho thành công cho biên bản {audit.Id}. Tổng mặt hàng lệch: {audit.Details.Count(d => d.VarianceQty != 0)}",
                Username = actor,
                EntityName = "StockAudit",
                Timestamp = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            if (transaction != null)
            {
                await transaction.CommitAsync();
            }
        }
        catch
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync();
            }
            throw;
        }
        finally
        {
            if (transaction != null)
            {
                await transaction.DisposeAsync();
            }
        }
    }

    public async Task<StockAuditDTO> GetAuditAsync(Guid auditId)
    {
        var audit = await _context.StockAudits
            .Include(a => a.Details)
            .FirstOrDefaultAsync(a => a.Id == auditId);
            
        if (audit == null) throw new KeyNotFoundException("Không tìm thấy biên bản kiểm kê.");

        var barcodes = audit.Details.Select(d => d.Barcode).Distinct().ToList();
        var products = await _context.Products
            .Where(p => barcodes.Contains(p.Barcode))
            .ToDictionaryAsync(p => p.Barcode, p => new { p.ProductName, Price = p.CostPrice ?? p.Price });
        
        var detailsDto = audit.Details.Select(d =>
        {
            string name = products.ContainsKey(d.Barcode) ? products[d.Barcode].ProductName : "Sản phẩm không xác định";
            decimal price = d.UnitPrice > 0 ? d.UnitPrice : (products.ContainsKey(d.Barcode) ? products[d.Barcode].Price : 0);
            return new StockAuditDetailDTO
            {
                Id = d.Id,
                Barcode = d.Barcode,
                ProductName = name,
                SystemQty = d.SystemQty,
                ActualQty = d.ActualQty,
                VarianceQty = d.VarianceQty,
                LossReason = d.LossReason,
                UnitPrice = price,
                VarianceAmount = d.VarianceQty * price,
                Notes = d.Notes
            };
        }).ToList();

        return new StockAuditDTO
        {
            Id = audit.Id,
            AuditDate = audit.AuditDate,
            CreatedBy = audit.CreatedBy,
            Zone = audit.Zone,
            Status = audit.Status,
            Notes = audit.Notes,
            ReconciledAt = audit.ReconciledAt,
            ReconciledBy = audit.ReconciledBy,
            TotalItems = detailsDto.Count,
            TotalDiscrepancyQty = detailsDto.Sum(d => Math.Abs(d.VarianceQty)),
            TotalLossAmount = detailsDto.Where(d => d.VarianceQty < 0).Sum(d => Math.Abs(d.VarianceAmount)),
            Details = detailsDto
        };
    }

    public async Task<List<StockAuditDTO>> GetAuditListAsync(string? status = null, string? zone = null)
    {
        var query = _context.StockAudits.Include(a => a.Details).AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(a => a.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(zone) && zone != "Tất cả")
        {
            query = query.Where(a => a.Zone == zone);
        }

        var audits = await query.OrderByDescending(a => a.AuditDate).Take(100).ToListAsync();
        var result = new List<StockAuditDTO>();

        foreach (var a in audits)
        {
            int totalItems = a.Details.Count;
            int totalDiff = a.Details.Sum(d => Math.Abs(d.VarianceQty));
            decimal totalLoss = a.Details.Where(d => d.VarianceQty < 0).Sum(d => Math.Abs(d.VarianceAmount));

            result.Add(new StockAuditDTO
            {
                Id = a.Id,
                AuditDate = a.AuditDate,
                CreatedBy = a.CreatedBy,
                Zone = a.Zone,
                Status = a.Status,
                Notes = a.Notes,
                ReconciledAt = a.ReconciledAt,
                ReconciledBy = a.ReconciledBy,
                TotalItems = totalItems,
                TotalDiscrepancyQty = totalDiff,
                TotalLossAmount = totalLoss,
                Details = new List<StockAuditDetailDTO>() // Rút gọn khi lấy danh sách tổng hợp
            });
        }

        return result;
    }
}
