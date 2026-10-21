using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmartSupermarket.Backend.Features.Inventory.DTOs.Requests;
using SmartSupermarket.Backend.Features.Inventory.Services.Interfaces;

namespace SmartSupermarket.Backend.Features.Inventory.BackgroundServices;

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
        _logger.LogInformation("ShelfLifeBackgroundService đã khởi động.");

        // Quét khởi động lần đầu khi server bật
        try
        {
            await RunScanAsync(new ShelfLifeScanRequest { IsEveningFlashSale = false, DryRun = false }, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi quét khởi động ban đầu.");
        }

        // Kiểm tra định kỳ mỗi 30 phút một lần
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(30));
        int lastScannedHour = -1;

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                var now = DateTime.Now;

                // Quét lúc 00:00: Phân loại hạn dùng và giảm giá cận date
                if (now.Hour == 0 && lastScannedHour != 0)
                {
                    _logger.LogInformation("00:00 - Tự động quét hạn sử dụng hệ thống.");
                    await RunScanAsync(new ShelfLifeScanRequest { IsEveningFlashSale = false, DryRun = false }, stoppingToken);
                    lastScannedHour = 0;
                }
                // Quét lúc 18:00: Flash Sale xả hàng tươi sống trong ngày
                else if (now.Hour == 18 && lastScannedHour != 18)
                {
                    _logger.LogInformation("18:00 - Tự động kích hoạt Flash Sale hàng tươi sống.");
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
                _logger.LogError(ex, "Lỗi xảy ra trong chu kỳ quét ShelfLifeBackgroundService.");
            }
        }

        _logger.LogInformation("ShelfLifeBackgroundService đã dừng.");
    }

    private async Task RunScanAsync(ShelfLifeScanRequest request, CancellationToken cancellationToken)
    {
        // Tạo Scope để lấy IShelfLifeProcessor trong Background Worker
        using var scope = _scopeFactory.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<IShelfLifeProcessor>();
        await processor.ExecuteShelfLifeAndPricingScanAsync(request, cancellationToken);
    }
}
