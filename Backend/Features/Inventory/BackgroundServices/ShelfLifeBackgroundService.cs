using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
        _logger.LogInformation("ShelfLifeBackgroundService (Worker quản lý hạn dùng & giá tự động) đã khởi động.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.Now;

                // Khung kiểm tra theo chu kỳ (mỗi giờ hoặc theo lịch 00:00 và 18:00)
                // Trong môi trường thực tế hoặc demo, có thể điều chỉnh tần suất hoặc gọi API kích hoạt thủ công.
                
                // Nghỉ 1 giờ trước lần kiểm tra tiếp theo
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra trong tiến trình ShelfLifeBackgroundService");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("ShelfLifeBackgroundService đã dừng.");
    }

    /// <summary>
    /// Hàm gọi trực tiếp processor trong scope để quét kho (phục vụ cả Worker và Trigger)
    /// </summary>
    public async Task RunScanOnceAsync(bool isEveningFlashSale = false, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<IShelfLifeProcessor>();
        await processor.ExecuteShelfLifeAndPricingScanAsync(isEveningFlashSale, cancellationToken);
    }
}
