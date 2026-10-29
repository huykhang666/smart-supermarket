using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using SmartSupermarket.Backend.Features.AI.Cache.Implementations;
using SmartSupermarket.Backend.Features.AI.Cache.Interfaces;
using SmartSupermarket.Backend.Features.AI.Monitoring.Interfaces;
using SmartSupermarket.Backend.Infrastructure.External.AI.Gateway;
using Xunit;

namespace SmartSupermarket.Backend.Tests.AI;

public class GatewayTests
{
    private readonly Mock<IResponseCache> _responseCacheMock = new();
    private readonly Mock<IAiMonitoringService> _monitoringServiceMock = new();
    private readonly Mock<ILogger<LlmGateway>> _loggerMock = new();
    private readonly Mock<IConfiguration> _configMock = new();

    [Fact]
    public async Task LlmGateway_ShouldReturnCachedResult_WhenResponseCacheHits()
    {
        _responseCacheMock
            .Setup(c => c.GetResponseAsync("gợi ý mâm cơm", null, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Kết quả đã lưu trong Response Cache");

        var geminiMock = new Mock<GeminiGateway>(new HttpClient(), _configMock.Object, Mock.Of<ILogger<GeminiGateway>>());
        var groqMock = new Mock<GroqGateway>(new HttpClient(), _configMock.Object, Mock.Of<ILogger<GroqGateway>>());
        var localFallback = new LocalFallbackGateway();

        var gateway = new LlmGateway(
            geminiMock.Object,
            groqMock.Object,
            localFallback,
            _responseCacheMock.Object,
            _loggerMock.Object,
            _monitoringServiceMock.Object);

        var response = await gateway.GenerateTextAsync("gợi ý mâm cơm");

        Assert.Equal("Kết quả đã lưu trong Response Cache", response);
        Assert.Equal("Cache-Hit", gateway.ActiveProvider);
        _monitoringServiceMock.Verify(m => m.RecordLlmInvocation("cache-hit", "Redis/Memory", It.IsAny<double>(), It.IsAny<long>(), It.IsAny<long>(), It.IsAny<decimal>(), true, true, null, null), Times.Once);
    }

    [Fact]
    public async Task LlmGateway_ShouldFallbackToGroq_WhenGeminiThrowsException()
    {
        _responseCacheMock
            .Setup(c => c.GetResponseAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var geminiMock = new Mock<GeminiGateway>(new HttpClient(), _configMock.Object, Mock.Of<ILogger<GeminiGateway>>());
        geminiMock.SetupGet(g => g.IsConfigured).Returns(true);
        geminiMock
            .Setup(g => g.GenerateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Gemini quota exceeded"));

        var groqMock = new Mock<GroqGateway>(new HttpClient(), _configMock.Object, Mock.Of<ILogger<GroqGateway>>());
        groqMock.SetupGet(g => g.IsConfigured).Returns(true);
        groqMock
            .Setup(g => g.GenerateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Phản hồi từ Groq LLaMA 3.3");

        var localFallback = new LocalFallbackGateway();

        var gateway = new LlmGateway(
            geminiMock.Object,
            groqMock.Object,
            localFallback,
            _responseCacheMock.Object,
            _loggerMock.Object,
            _monitoringServiceMock.Object);

        var response = await gateway.GenerateTextAsync("Hỏi thực đơn");

        Assert.Equal("Phản hồi từ Groq LLaMA 3.3", response);
        Assert.Equal("Groq-Llama-3.3-70B", gateway.ActiveProvider);
        _responseCacheMock.Verify(c => c.SetResponseAsync("Hỏi thực đơn", "Phản hồi từ Groq LLaMA 3.3", null, false, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LlmGateway_ShouldFallbackToLocalEngine_WhenBothGeminiAndGroqFail()
    {
        _responseCacheMock
            .Setup(c => c.GetResponseAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var geminiMock = new Mock<GeminiGateway>(new HttpClient(), _configMock.Object, Mock.Of<ILogger<GeminiGateway>>());
        geminiMock.SetupGet(g => g.IsConfigured).Returns(false);

        var groqMock = new Mock<GroqGateway>(new HttpClient(), _configMock.Object, Mock.Of<ILogger<GroqGateway>>());
        groqMock.SetupGet(g => g.IsConfigured).Returns(false);

        var localFallback = new LocalFallbackGateway();

        var gateway = new LlmGateway(
            geminiMock.Object,
            groqMock.Object,
            localFallback,
            _responseCacheMock.Object,
            _loggerMock.Object,
            _monitoringServiceMock.Object);

        var response = await gateway.GenerateTextAsync("chào bạn");

        Assert.NotNull(response);
        Assert.Contains("Smart SuperMarket", response);
        Assert.Equal("Local-Rule-Engine", gateway.ActiveProvider);
    }

    [Fact]
    public async Task LocalFallbackGateway_GenerateAsync_ShouldReturnJson_WhenRequireJsonIsTrue()
    {
        var local = new LocalFallbackGateway();
        var json = await local.GenerateAsync("mâm cơm 30k", null, requireJson: true);

        Assert.NotNull(json);
        Assert.StartsWith("{", json.Trim());
        Assert.Contains("LocalDeterministicRuleEngine", json);
        Assert.Contains("Cơm Trứng Chiên", json);
    }

    [Fact]
    public async Task ResponseCache_ComputeHashAndCache_ShouldHitOnIdenticalPrompt()
    {
        var cacheServiceMock = new Mock<ICacheService>();
        var loggerMock = new Mock<ILogger<ResponseCache>>();

        cacheServiceMock
            .Setup(c => c.GetAsync<string>(It.Is<string>(k => k.StartsWith("ai:resp:")), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Cached Response Value");

        var responseCache = new ResponseCache(cacheServiceMock.Object, loggerMock.Object);
        var result = await responseCache.GetResponseAsync("mâm cơm hôm nay", "system instructions", false);

        Assert.Equal("Cached Response Value", result);
    }
}
