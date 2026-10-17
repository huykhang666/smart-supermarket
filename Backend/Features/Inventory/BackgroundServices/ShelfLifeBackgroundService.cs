using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmartSupermarket.Backend.Features.Inventory.DTOs.Requests;
using SmartSupermarket.Backend.Features.Inventory.Services;

namespace SmartSupermarket.Backend.Features.Inventory.BackgroundServices;

/// <summary>
/// Background Worker tự động quét kho theo định kỳ:
/// 1. Lúc 00:00: Quét toàn bộ sản phẩm cận hạn (<= 3 ngày giảm 30%, <= 1 ngày giảm 50%, hết hạn khóa bán).
/// 2. Lúc 18:00: Tự động kích hoạt Flash Sale chiều tối xả hàng tươi sống, bánh mì, rau củ.
/// </summary>
public class ShelfLifeBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ShelfLifeBackgroundService> _logger;

    public ShelfLifeBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<ShelfLifeBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 [Worker Khởi động] Hệ thống tự động quản lý hạn dùng & giá động đã chạy.");

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(30));
        int lastScannedHour = -1;

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                var now = DateTime.Now;

                // 1. Quét lúc 00:00 (Nửa đêm): Quét hạn sử dụng & giảm giá cận date
                if (now.Hour == 0 && lastScannedHour != 0)
                {
                    _logger.LogInformation("⏰ [00:00 Nửa đêm] Kích hoạt quét toàn bộ hạn sử dụng hệ thống...");
                    await RunScanAsync(new ShelfLifeScanRequest { IsEveningFlashSale = false, DryRun = false }, stoppingToken);
                    lastScannedHour = 0;
                }
                // 2. Quét lúc 18:00 (6h Chiều): Kích hoạt Flash Sale xả hàng tươi sống
                else if (now.Hour == 18 && lastScannedHour != 18)
                {
                    _logger.LogInformation("⚡ [18:00 Chiều tối] Kích hoạt Flash Sale hàng tươi sống trong ngày...");
                    await RunScanAsync(new ShelfLifeScanRequest { IsEveningFlashSale = true, DryRun = false }, stoppingToken);
                    lastScannedHour = 18;
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Lỗi xảy ra trong chu kỳ quét ShelfLifeBackgroundService");
            }
        }

        _logger.LogInformation("🛑 ShelfLifeBackgroundService đã dừng an toàn.");
    }

    private async Task RunScanAsync(ShelfLifeScanRequest request, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<IShelfLifeProcessor>();
        await processor.ExecuteShelfLifeAndPricingScanAsync(request, cancellationToken);
    }
}
