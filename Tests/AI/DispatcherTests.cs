using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using SmartSupermarket.Backend.Features.AI.Dispatcher;
using SmartSupermarket.Backend.Features.AI.Monitoring.Interfaces;
using Xunit;

namespace SmartSupermarket.Backend.Tests.AI;

public class DispatcherTests
{
    private readonly Mock<ILogger<ToolDispatcher>> _loggerMock = new();
    private readonly Mock<IAiMonitoringService> _monitoringMock = new();

    [Fact]
    public async Task DispatchAsync_ShouldExecuteCustomHandler_WhenRegistered()
    {
        var dispatcher = new ToolDispatcher(_loggerMock.Object, monitoringService: _monitoringMock.Object);

        dispatcher.RegisterHandler("Custom.Calculate", (p, ct) =>
        {
            var a = Convert.ToInt32(p["a"]);
            var b = Convert.ToInt32(p["b"]);
            return Task.FromResult<object?>(a + b);
        });

        var parameters = new Dictionary<string, object>
        {
            ["a"] = 10,
            ["b"] = 25
        };

        var result = await dispatcher.DispatchAsync("Custom.Calculate", parameters);

        Assert.True(result.IsSuccess);
        Assert.Equal(35, result.Data);
        _monitoringMock.Verify(m => m.RecordToolExecution("Custom.Calculate", It.IsAny<double>(), true, null, null), Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_ShouldReturnFailure_WhenToolNotRegistered()
    {
        var dispatcher = new ToolDispatcher(_loggerMock.Object, monitoringService: _monitoringMock.Object);

        var result = await dispatcher.DispatchAsync("NonExistent.Tool", new Dictionary<string, object>());

        Assert.False(result.IsSuccess);
        Assert.Contains("chưa được đăng ký", result.ErrorMessage);
    }

    [Fact]
    public async Task DispatchAsync_ShouldRetryAndRecordFailure_WhenHandlerThrows()
    {
        var dispatcher = new ToolDispatcher(_loggerMock.Object, monitoringService: _monitoringMock.Object);

        int callCount = 0;
        dispatcher.RegisterHandler("Failing.Tool", (p, ct) =>
        {
            callCount++;
            throw new InvalidOperationException("Mô phỏng lỗi kết nối");
        });

        var result = await dispatcher.DispatchAsync("Failing.Tool", new Dictionary<string, object>());

        Assert.False(result.IsSuccess);
        Assert.Equal(2, callCount); // maxRetries = 2
        _monitoringMock.Verify(m => m.RecordToolExecution("Failing.Tool", It.IsAny<double>(), false, null, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_ShouldSucceedOnSecondAttempt_WhenTransientErrorOccurs()
    {
        var dispatcher = new ToolDispatcher(_loggerMock.Object, monitoringService: _monitoringMock.Object);

        int attempts = 0;
        dispatcher.RegisterHandler("Flaky.Tool", (p, ct) =>
        {
            attempts++;
            if (attempts == 1)
            {
                throw new TimeoutException("Tạm thời timeout lần 1");
            }
            return Task.FromResult<object?>("Thành công ở lần 2");
        });

        var result = await dispatcher.DispatchAsync("Flaky.Tool", new Dictionary<string, object>());

        Assert.True(result.IsSuccess);
        Assert.Equal("Thành công ở lần 2", result.Data);
        Assert.Equal(2, attempts);
        _monitoringMock.Verify(m => m.RecordToolExecution("Flaky.Tool", It.IsAny<double>(), true, null, null), Times.Once);
    }
}
