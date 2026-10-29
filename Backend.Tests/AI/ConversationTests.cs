using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using SmartSupermarket.Backend.Features.AI.Cache.Implementations;
using SmartSupermarket.Backend.Features.AI.Cache.Interfaces;
using SmartSupermarket.Backend.Features.AI.DTOs.Common;
using SmartSupermarket.Backend.Features.AI.DTOs.Requests;
using SmartSupermarket.Backend.Features.AI.Services.Context;
using Xunit;

namespace SmartSupermarket.Backend.Tests.AI;

public class ConversationTests
{
    private readonly Mock<ICacheService> _cacheServiceMock = new();
    private readonly Mock<ILogger<ConversationCache>> _cacheLoggerMock = new();
    private readonly Mock<ILogger<ConversationContextService>> _serviceLoggerMock = new();

    [Fact]
    public async Task ConversationCache_GetContextAsync_ShouldReturnCachedContext()
    {
        var expected = new AgentContextDto { SessionId = "sess-123", Servings = 4, TargetBudget = 200000m };
        _cacheServiceMock
            .Setup(c => c.GetAsync<AgentContextDto>("ai:conv:ctx:sess-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var convCache = new ConversationCache(_cacheServiceMock.Object, _cacheLoggerMock.Object);
        var actual = await convCache.GetContextAsync("sess-123");

        Assert.NotNull(actual);
        Assert.Equal(4, actual.Servings);
        Assert.Equal(200000m, actual.TargetBudget);
    }

    [Fact]
    public async Task ConversationCache_AppendMessageAsync_ShouldLimitHistoryToMax10Turns()
    {
        var existingHistory = new List<ChatMessageDto>();
        for (int i = 1; i <= 10; i++)
        {
            existingHistory.Add(new ChatMessageDto { Role = "user", Content = $"Tin nhắn {i}" });
        }

        _cacheServiceMock
            .Setup(c => c.GetAsync<List<ChatMessageDto>>("ai:conv:hist:sess-limit", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingHistory);

        List<ChatMessageDto>? savedList = null;
        _cacheServiceMock
            .Setup(c => c.SetAsync("ai:conv:hist:sess-limit", It.IsAny<List<ChatMessageDto>>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .Callback<string, List<ChatMessageDto>, TimeSpan?, CancellationToken>((k, v, exp, ct) => savedList = v)
            .Returns(Task.CompletedTask);

        var convCache = new ConversationCache(_cacheServiceMock.Object, _cacheLoggerMock.Object);
        await convCache.AppendMessageAsync("sess-limit", "user", "Tin nhắn thứ 11");

        Assert.NotNull(savedList);
        Assert.Equal(10, savedList.Count);
        Assert.Equal("Tin nhắn thứ 11", savedList[^1].Content);
        Assert.Equal("Tin nhắn 2", savedList[0].Content); // tin nhan 1 da bi loai bo
    }

    [Fact]
    public async Task ConversationCache_ClearAsync_ShouldRemoveBothContextAndHistory()
    {
        var convCache = new ConversationCache(_cacheServiceMock.Object, _cacheLoggerMock.Object);
        await convCache.ClearAsync("sess-to-clear");

        _cacheServiceMock.Verify(c => c.RemoveAsync("ai:conv:ctx:sess-to-clear", It.IsAny<CancellationToken>()), Times.Once);
        _cacheServiceMock.Verify(c => c.RemoveAsync("ai:conv:hist:sess-to-clear", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConversationContextService_GetOrCreateContextAsync_ShouldCreateNewWhenAbsent()
    {
        var convCacheMock = new Mock<IConversationCache>();
        convCacheMock
            .Setup(c => c.GetContextAsync("new-session", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AgentContextDto?)null);

        var service = new ConversationContextService(convCacheMock.Object, _serviceLoggerMock.Object);
        var context = await service.GetOrCreateContextAsync("new-session", "user-01");

        Assert.NotNull(context);
        Assert.Equal("new-session", context.SessionId);
        Assert.Equal("user-01", context.UserId);
        Assert.Equal(2, context.Servings);
        Assert.Equal(350000m, context.TargetBudget);

        convCacheMock.Verify(c => c.SetContextAsync("new-session", It.IsAny<AgentContextDto>(), null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConversationContextService_GetHistoryAsync_ShouldHonorLimit()
    {
        var convCacheMock = new Mock<IConversationCache>();
        var sampleHistory = new List<ChatMessageDto>
        {
            new() { Role = "user", Content = "M1" },
            new() { Role = "assistant", Content = "M2" },
            new() { Role = "user", Content = "M3" },
            new() { Role = "assistant", Content = "M4" }
        };

        convCacheMock
            .Setup(c => c.GetHistoryAsync("session-test", 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChatMessageDto>
            {
                new() { Role = "user", Content = "M3" },
                new() { Role = "assistant", Content = "M4" }
            });

        var service = new ConversationContextService(convCacheMock.Object, _serviceLoggerMock.Object);
        var result = await service.GetHistoryAsync("session-test", limit: 2);

        Assert.Equal(2, result.Count);
        Assert.Equal("M3", result[0].Content);
        Assert.Equal("M4", result[1].Content);
    }
}
