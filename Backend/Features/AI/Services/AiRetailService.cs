using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Domain.Enums;
using SmartSupermarket.Backend.Features.AI.DTOs;
using SmartSupermarket.Backend.Infrastructure.External.AI;
using SmartSupermarket.Backend.Infrastructure.Persistence;

namespace SmartSupermarket.Backend.Features.AI.Services;

public class AiRetailService : IAiRetailService
{
    private readonly AppDbContext _dbContext;
    private readonly ILlmClient _llmClient;
    private readonly ILogger<AiRetailService> _logger;

    public AiRetailService(
        AppDbContext dbContext, 
        ILlmClient llmClient,
        ILogger<AiRetailService> logger)
    {
        _dbContext = dbContext;
        _llmClient = llmClient;
        _logger = logger;
    }

    // 1. Phân tích rủi ro tồn kho, dự báo đứt gãy và tính EOQ
    public async Task<AiProductRiskReport> AnalyzeProductRisksAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var thirtyDaysAgo = now.AddDays(-30);

        // Lấy tất cả sản phẩm đang hoạt động kèm danh mục và tồn kho
        var products = await _dbContext.Products
            .Include(p => p.Category)
            .Where(p => p.Status == ProductStatus.Active)
            .ToListAsync(cancellationToken);

        // Tính tồn kho hiện tại cho từng sản phẩm từ bảng Inventory (hoặc InventoryBatches)
        var inventories = await _dbContext.Inventories
            .Where(i => i.BranchId == 1)
            .ToDictionaryAsync(i => i.ProductId, i => i.QuantityOnHand, cancellationToken);

        var batchGroups = await _dbContext.InventoryBatches
            .Where(b => b.Quantity > 0)
            .GroupBy(b => b.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                TotalBatchQty = g.Sum(b => b.Quantity),
                EarliestExpiry = g.Min(b => b.ExpiryDate)
            })
            .ToDictionaryAsync(g => g.ProductId, cancellationToken);

        // Tính doanh số bán 30 ngày gần nhất của từng sản phẩm từ OrderDetails
        var sales30Days = await _dbContext.OrderDetails
            .Where(d => d.Order.CreatedAt >= thirtyDaysAgo && d.Order.Status != OrderStatus.Cancelled)
            .GroupBy(d => d.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                TotalSold = g.Sum(d => d.Quantity)
            })
            .ToDictionaryAsync(g => g.ProductId, g => g.TotalSold, cancellationToken);

        var report = new AiProductRiskReport
        {
            AnalyzedAt = now,
            TotalScannedItems = products.Count
        };

        var promptRiskItems = new List<object>();

        foreach (var p in products)
        {
            // Xác định số lượng tồn thực tế
            int stock = inventories.TryGetValue(p.ProductId, out var invQty) 
                ? invQty 
                : (batchGroups.TryGetValue(p.ProductId, out var bg) ? bg.TotalBatchQty : 0);

            int sold30d = sales30Days.TryGetValue(p.ProductId, out var sVal) ? sVal : 0;
            decimal avgDailySales = Math.Round(sold30d / 30.0m, 2);

            // Tính số ngày cạn kho
            int daysUntilStockout = avgDailySales > 0 ? (int)(stock / avgDailySales) : (stock == 0 ? 0 : 999);

            DateTime? earliestExp = batchGroups.TryGetValue(p.ProductId, out var bInfo) ? bInfo.EarliestExpiry : p.ExpiryDate;
            int? daysUntilExpiry = earliestExp.HasValue ? (int)(earliestExp.Value.Date - now.Date).TotalDays : null;

            // Tính EOQ (Số lượng đặt hàng tối ưu theo nhu cầu năm D và chi phí lưu kho H)
            decimal annualDemand = Math.Max(avgDailySales * 365, 50);
            decimal cost = p.CostPrice ?? (p.Price * 0.7m);
            decimal holdingCost = Math.Max(cost * 0.15m, 1000m);
            decimal orderCost = 50000m;
            int eoq = (int)Math.Max(10, Math.Round(Math.Sqrt((double)(2 * annualDemand * orderCost / holdingCost)), 0));

            // Đánh giá mức độ rủi ro: Đỏ (CRITICAL), Vàng (HIGH/MEDIUM)
            string riskType = "";
            string urgencyLevel = "LOW";
            string recommendedAction = "MONITOR";
            string reasoning = "";
            decimal suggestedDiscount = 0;
            decimal suggestedPrice = p.Price;

            if (stock == 0 || daysUntilStockout <= 3)
            {
                riskType = "OUT_OF_STOCK_RISK";
                urgencyLevel = "CRITICAL";
                recommendedAction = "CREATE_RESTOCK_ORDER";
                reasoning = $"Tồn kho còn {stock} cái, tốc độ bán {avgDailySales}/ngày. Dự kiến đứt hàng trong {daysUntilStockout} ngày tới! Cần nhập gấp {eoq} cái.";
            }
            else if (daysUntilExpiry.HasValue && daysUntilExpiry <= 2)
            {
                riskType = "NEAR_EXPIRY";
                urgencyLevel = "CRITICAL";
                recommendedAction = "APPLY_MARKDOWN";
                suggestedDiscount = 50;
                suggestedPrice = Math.Round(p.Price * 0.5m, 0);
                reasoning = $"Hạn dùng chỉ còn {daysUntilExpiry} ngày. Cần giảm giá 50% thanh lý khẩn cấp tránh tiêu hủy.";
            }
            else if (daysUntilStockout <= 7)
            {
                riskType = "OUT_OF_STOCK_RISK";
                urgencyLevel = "HIGH";
                recommendedAction = "CREATE_RESTOCK_ORDER";
                reasoning = $"Tồn kho còn {stock} cái, dự kiến hết hàng trong {daysUntilStockout} ngày. Đề xuất tạo phiếu nhập {eoq} cái.";
            }
            else if (daysUntilExpiry.HasValue && daysUntilExpiry <= 7)
            {
                riskType = "NEAR_EXPIRY";
                urgencyLevel = "HIGH";
                recommendedAction = "APPLY_MARKDOWN";
                suggestedDiscount = 30;
                suggestedPrice = Math.Round(p.Price * 0.7m, 0);
                reasoning = $"Lô hàng hết hạn trong {daysUntilExpiry} ngày. Đề xuất giảm 30% xả hàng.";
            }
            else if (stock > 40 && sold30d <= 2)
            {
                riskType = "SLOW_MOVING";
                urgencyLevel = "MEDIUM";
                recommendedAction = "BUNDLE_PROMOTION";
                suggestedDiscount = 15;
                suggestedPrice = Math.Round(p.Price * 0.85m, 0);
                reasoning = $"Tồn cao ({stock} cái) nhưng 30 ngày qua chỉ bán {sold30d} cái. Nên tạo combo bán kèm để xả vốn.";
            }

            if (!string.IsNullOrEmpty(riskType))
            {
                var item = new ProductRiskItem
                {
                    ProductId = p.ProductId,
                    Barcode = p.Barcode,
                    ProductName = p.ProductName,
                    CategoryName = p.Category?.CategoryName ?? "Tổng hợp",
                    CurrentStock = stock,
                    Price = p.Price,
                    CostPrice = cost,
                    ExpiryDate = earliestExp,
                    DaysUntilExpiry = daysUntilExpiry,
                    AverageSalesPerDay = avgDailySales,
                    RiskType = riskType,
                    UrgencyLevel = urgencyLevel,
                    AiAnalysisReasoning = reasoning,
                    SuggestedDiscountPercent = suggestedDiscount,
                    SuggestedNewPrice = suggestedPrice,
                    RecommendedAction = recommendedAction
                };

                if (urgencyLevel == "CRITICAL")
                {
                    report.HighRiskItems.Add(item);
                    report.CriticalIssuesCount++;
                }
                else
                {
                    report.ModerateRiskItems.Add(item);
                }

                if (promptRiskItems.Count < 10)
                {
                    promptRiskItems.Add(new
                    {
                        item.ProductName,
                        item.CurrentStock,
                        item.AverageSalesPerDay,
                        DaysLeft = daysUntilStockout,
                        item.RiskType,
                        EOQ = eoq
                    });
                }
            }
        }

        // Gọi Groq API (Llama 3.3 70B) để tổng hợp chiến lược điều hành bán lẻ
        try
        {
            var systemPrompt = "Bạn là Giám đốc Vận hành Bán lẻ (COO) siêu thị. Hãy phân tích ngắn gọn, súc tích chiến lược nhập hàng và xả hàng cận date dựa trên dữ liệu cung cấp.";
            var userPrompt = $"Dữ liệu rủi ro kho hàng:\n{JsonSerializer.Serialize(promptRiskItems)}\n\nHãy tóm tắt 3 hành động khẩn cấp nhất cho người quản lý hôm nay.";
            
            var aiText = await _llmClient.GenerateAsync(userPrompt, systemPrompt, false, cancellationToken);
            if (!string.IsNullOrWhiteSpace(aiText) && !aiText.Contains("\"error\""))
            {
                report.SummaryStrategy = aiText.Trim();
            }
            else
            {
                report.SummaryStrategy = $"Hệ thống phát hiện {report.CriticalIssuesCount} mặt hàng báo động Đỏ (nguy cơ cháy hàng/hết date trong $\\le 3$ ngày) và {report.ModerateRiskItems.Count} mặt hàng báo động Vàng. Quản lý cần duyệt tạo phiếu nhập kho khẩn cấp cho các mặt hàng có nguy cơ đứt gãy.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể gọi LLM, dùng tóm lược thuật toán mặc định.");
            report.SummaryStrategy = $"Phát hiện {report.CriticalIssuesCount} mặt hàng khẩn cấp (Đỏ) và {report.ModerateRiskItems.Count} mặt hàng cảnh báo (Vàng). Ưu tiên nhập hàng cho các sản phẩm tồn kho $\\le 3$ ngày.";
        }

        return report;
    }

    // 2. Thuật toán phân tích rổ hàng (Market Basket Analysis) tìm cặp sản phẩm bán kèm
    public async Task<AiMarketBasketReport> AnalyzeMarketBasketAsync(int daysBack = 30, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var fromDate = now.AddDays(-daysBack);

        // Lấy chi tiết các đơn hàng thành công trong khoảng thời gian
        var orderDetails = await _dbContext.OrderDetails
            .Include(d => d.Product)
            .Where(d => d.Order.CreatedAt >= fromDate && d.Order.Status != OrderStatus.Cancelled)
            .Select(d => new
            {
                d.OrderId,
                d.ProductId,
                ProductName = d.Product.ProductName,
                Price = d.Product.Price
            })
            .ToListAsync(cancellationToken);

        var report = new AiMarketBasketReport
        {
            AnalyzedAt = now
        };

        // Nhóm sản phẩm theo từng đơn hàng (Giỏ hàng)
        var baskets = orderDetails
            .GroupBy(d => d.OrderId)
            .Select(g => g.GroupBy(x => x.ProductId).Select(x => x.First()).ToList())
            .Where(items => items.Count >= 2)
            .ToList();

        report.AnalyzedOrdersCount = baskets.Count;

        if (baskets.Count == 0)
        {
            report.GeneralRecommendations = "Chưa có đủ dữ liệu đơn hàng mua cùng nhau trong 30 ngày qua để lập ma trận rổ hàng.";
            return report;
        }

        // Đếm tần suất xuất hiện cặp (A, B)
        var pairCounts = new Dictionary<(int, int), int>();
        var productLookup = new Dictionary<int, (string Name, decimal Price)>();

        foreach (var basket in baskets)
        {
            for (int i = 0; i < basket.Count; i++)
            {
                int idA = basket[i].ProductId;
                if (!productLookup.ContainsKey(idA)) productLookup[idA] = (basket[i].ProductName, basket[i].Price);

                for (int j = i + 1; j < basket.Count; j++)
                {
                    int idB = basket[j].ProductId;
                    if (!productLookup.ContainsKey(idB)) productLookup[idB] = (basket[j].ProductName, basket[j].Price);

                    var key = idA < idB ? (idA, idB) : (idB, idA);
                    pairCounts[key] = pairCounts.TryGetValue(key, out int count) ? count + 1 : 1;
                }
            }
        }

        // Sắp xếp các cặp có tần suất mua kèm cao nhất
        var topPairs = pairCounts
            .OrderByDescending(kv => kv.Value)
            .Take(10)
            .ToList();

        foreach (var (key, freq) in topPairs)
        {
            var prodA = productLookup[key.Item1];
            var prodB = productLookup[key.Item2];

            double support = (double)freq / baskets.Count;
            decimal totalPrice = prodA.Price + prodB.Price;
            decimal comboPrice = Math.Round(totalPrice * 0.9m, 0); // Giảm 10% khi mua combo

            report.TopCombinations.Add(new MarketBasketPairItem
            {
                ProductAId = key.Item1,
                ProductAName = prodA.Name,
                ProductBId = key.Item2,
                ProductBName = prodB.Name,
                PairOrderFrequency = freq,
                CorrelationScore = Math.Round(support * 100, 1),
                SuggestedComboName = $"Combo Tiết Kiệm: {prodA.Name} + {prodB.Name}",
                ComboPrice = comboPrice,
                DiscountPercent = 10,
                PromotionalReason = $"Được mua cùng nhau trong {freq} đơn hàng (Tỷ lệ {support * 100:F1}%). Gợi ý đặt cạnh nhau trên quầy hoặc kích cầu bằng combo giảm 10%."
            });
        }

        // Dùng Groq API đặt tên combo sáng tạo và đưa ra chiến lược trưng bày
        try
        {
            var promptData = report.TopCombinations.Take(5).Select(c => new
            {
                ProductA = c.ProductAName,
                ProductB = c.ProductBName,
                c.PairOrderFrequency
            });

            var sysPrompt = "Bạn là chuyên gia Merchandising siêu thị. Dựa trên các cặp sản phẩm khách hàng hay mua cùng, hãy đưa ra gợi ý cách sắp xếp quầy kệ và tạo combo hấp dẫn nhất.";
            var userPrompt = $"Top các cặp sản phẩm bán kèm nhiều nhất:\n{JsonSerializer.Serialize(promptData)}\nHãy tóm tắt 2 đề xuất trưng bày kích cầu hiệu quả.";

            var groqInsight = await _llmClient.GenerateAsync(userPrompt, sysPrompt, false, cancellationToken);
            if (!string.IsNullOrWhiteSpace(groqInsight) && !groqInsight.Contains("\"error\""))
            {
                report.GeneralRecommendations = groqInsight.Trim();
            }
            else
            {
                report.GeneralRecommendations = $"Đã phân tích {baskets.Count} đơn hàng đa sản phẩm. Đề xuất đặt các cặp sản phẩm có tần suất mua kèm cao liền kề nhau trên kệ hoặc tại ụ khuyến mãi gần quầy thu ngân.";
            }
        }
        catch
        {
            report.GeneralRecommendations = $"Phát hiện {report.TopCombinations.Count} cặp sản phẩm bán chạy cùng nhau. Áp dụng combo giảm 10% để gia tăng kích thước giỏ hàng (Basket Size).";
        }

        return report;
    }

    // 3. Tự động sinh phiếu nhập kho nháp (Draft Import Order) cho các mặt hàng sắp hết
    public async Task<AiDraftImportOrder> GenerateDraftImportOrderAsync(int supplierId, CancellationToken cancellationToken = default)
    {
        var supplier = await _dbContext.Suppliers.FindAsync(new object[] { supplierId }, cancellationToken);
        string supplierName = supplier?.SupplierName ?? $"Nhà cung cấp #{supplierId}";

        // Lấy danh sách sản phẩm của nhà cung cấp này qua bảng ProductSupplier hoặc lấy chung nếu chưa gán
        var supplierProductIds = await _dbContext.ProductSuppliers
            .Where(ps => ps.SupplierId == supplierId)
            .Select(ps => ps.ProductId)
            .ToListAsync(cancellationToken);

        var query = _dbContext.Products
            .Include(p => p.Category)
            .Where(p => p.Status == ProductStatus.Active);

        if (supplierProductIds.Count > 0)
        {
            query = query.Where(p => supplierProductIds.Contains(p.ProductId));
        }

        var candidateProducts = await query.ToListAsync(cancellationToken);

        // Lấy tồn kho hiện tại
        var inventories = await _dbContext.Inventories
            .Where(i => i.BranchId == 1)
            .ToDictionaryAsync(i => i.ProductId, i => i.QuantityOnHand, cancellationToken);

        // Lấy doanh số 30 ngày gần nhất
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var sales30Days = await _dbContext.OrderDetails
            .Where(d => d.Order.CreatedAt >= thirtyDaysAgo && d.Order.Status != OrderStatus.Cancelled)
            .GroupBy(d => d.ProductId)
            .Select(g => new { ProductId = g.Key, TotalSold = g.Sum(d => d.Quantity) })
            .ToDictionaryAsync(g => g.ProductId, g => g.TotalSold, cancellationToken);

        var draftOrder = new AiDraftImportOrder
        {
            SuggestedSupplierId = supplierId,
            SupplierName = supplierName,
            OrderNote = $"[AI Tự Động] Đề xuất phiếu nhập kho tối ưu EOQ dựa trên tốc độ bán thực tế ngày {DateTime.UtcNow:dd/MM/yyyy HH:mm}."
        };

        foreach (var p in candidateProducts)
        {
            int stock = inventories.TryGetValue(p.ProductId, out var invQty) ? invQty : 0;
            int sold30d = sales30Days.TryGetValue(p.ProductId, out var sVal) ? sVal : 0;
            decimal avgDaily = sold30d / 30.0m;
            int daysLeft = avgDaily > 0 ? (int)(stock / avgDaily) : (stock == 0 ? 0 : 999);

            // Lọc sản phẩm sắp hết hàng (tồn <= 15 hoặc còn <= 7 ngày bán)
            if (stock <= 15 || daysLeft <= 7)
            {
                decimal cost = p.CostPrice ?? (p.Price * 0.7m);
                // Công thức tính số lượng nhập: bù đủ bán trong 25 ngày tới + tồn an toàn
                int importQty = (int)Math.Max(20, Math.Ceiling(avgDaily * 25) + 10 - stock);

                draftOrder.Items.Add(new DraftImportOrderItem
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    Barcode = p.Barcode,
                    Unit = p.Unit,
                    CurrentStock = stock,
                    RecommendedImportQuantity = importQty,
                    ExpectedCostPrice = cost,
                    UrgencyReason = stock == 0 
                        ? "Hết sạch hàng trong kho (0 cái)" 
                        : $"Tồn còn {stock} cái, dự kiến hết sau {daysLeft} ngày (bán {avgDaily:F1}/ngày)"
                });
            }
        }

        // Nếu nhà cung cấp chưa có hàng thiếu, gợi ý top 5 sản phẩm tồn thấp nhất
        if (draftOrder.Items.Count == 0)
        {
            var lowestStock = candidateProducts.Take(5).ToList();
            foreach (var p in lowestStock)
            {
                int stock = inventories.TryGetValue(p.ProductId, out var invQty) ? invQty : 5;
                decimal cost = p.CostPrice ?? (p.Price * 0.7m);

                draftOrder.Items.Add(new DraftImportOrderItem
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    Barcode = p.Barcode,
                    Unit = p.Unit,
                    CurrentStock = stock,
                    RecommendedImportQuantity = 30,
                    ExpectedCostPrice = cost,
                    UrgencyReason = "Nhập bổ sung định kỳ duy trì tồn an toàn"
                });
            }
        }

        draftOrder.TotalEstimatedCost = draftOrder.Items.Sum(i => i.SubTotalCost);
        return draftOrder;
    }

    // 4. Lưu trực tiếp đơn nhập hàng nháp vào cơ sở dữ liệu (ImportReceipt)
    public async Task<ImportReceipt> CommitDraftImportOrderAsync(AiDraftImportOrder draftOrder, CancellationToken cancellationToken = default)
    {
        var receipt = new ImportReceipt
        {
            ReceiptCode = $"IR-AI-{DateTime.UtcNow:yyyyMMddHHmmss}",
            SupplierId = draftOrder.SuggestedSupplierId > 0 ? draftOrder.SuggestedSupplierId : 1,
            BranchId = 1,
            ImportedByUserId = 1,
            ImportDate = DateTime.UtcNow,
            TotalAmount = draftOrder.TotalEstimatedCost,
            Status = ImportStatus.Draft,
            Note = draftOrder.OrderNote
        };

        foreach (var item in draftOrder.Items)
        {
            receipt.ImportDetails.Add(new ImportDetail
            {
                ProductId = item.ProductId,
                Quantity = item.RecommendedImportQuantity,
                CostPrice = item.ExpectedCostPrice,
                SubTotal = item.SubTotalCost
            });
        }

        _dbContext.ImportReceipts.Add(receipt);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("1-Click AI: Đã sinh thành công phiếu nhập kho nháp {Code} với {Count} sản phẩm.", 
            receipt.ReceiptCode, receipt.ImportDetails.Count);

        return receipt;
    }
}
