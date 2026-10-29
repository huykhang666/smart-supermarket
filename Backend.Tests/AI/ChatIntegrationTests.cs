using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SmartSupermarket.Backend.Common.Results;
using SmartSupermarket.Backend.Features.AI.DTOs.Requests;
using SmartSupermarket.Backend.Features.AI.DTOs.Responses;
using Xunit;

namespace SmartSupermarket.Backend.Tests.AI;

public class ChatIntegrationTests : IAsyncLifetime
{
    private readonly AiIntegrationTestFixture _fixture = new();

    public async Task InitializeAsync()
    {
        await _fixture.SeedSampleDataAsync();
    }

    public async Task DisposeAsync()
    {
        await _fixture.DisposeAsync();
    }

    [Fact]
    public async Task Chat_MealPlanningFlow_ShouldReturnOkWithCookpadCardsAndSaveHistory()
    {
        var request = new AiChatRequestDto
        {
            SessionId = "chat-e2e-session-01",
            Message = "Gợi ý cho mình mâm cơm 2 người ngân sách 50k",
            NumberOfPeople = 2,
            TargetBudget = 50000m
        };

        var actionResult = await _fixture.Controller.Chat(request);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var apiResult = Assert.IsType<ApiResult<AiChatResponseDto>>(okResult.Value);

        Assert.True(apiResult.IsSuccess);
        Assert.NotNull(apiResult.Data);
        Assert.Equal("chat-e2e-session-01", apiResult.Data.SessionId);
        Assert.True(apiResult.Data.HasMealSuggestion);
        Assert.Equal(2, apiResult.Data.Servings);
        Assert.NotNull(apiResult.Data.OptionCards);
        Assert.NotEmpty(apiResult.Data.OptionCards);

        // Verify conversation history was recorded in ConversationCache
        var history = await _fixture.ConversationCache.GetHistoryAsync("chat-e2e-session-01");
        Assert.NotNull(history);
        Assert.Equal(2, history.Count); // 1 user + 1 assistant
        Assert.Equal("user", history[0].Role);
        Assert.Equal("assistant", history[1].Role);
    }

    [Fact]
    public async Task Chat_ConsecutiveTurns_ShouldAccumulateContextAcrossRequests()
    {
        var sessionId = "multi-turn-sess";

        var turn1 = new AiChatRequestDto
        {
            SessionId = sessionId,
            Message = "Chào bạn, hôm nay có món gì ngon?",
            NumberOfPeople = 3,
            TargetBudget = 100000m
        };
        await _fixture.Controller.Chat(turn1);

        var turn2 = new AiChatRequestDto
        {
            SessionId = sessionId,
            Message = "Gợi ý mâm cơm tiết kiệm nhé",
            NumberOfPeople = 3,
            TargetBudget = 100000m
        };
        await _fixture.Controller.Chat(turn2);

        var history = await _fixture.ConversationCache.GetHistoryAsync(sessionId);
        Assert.NotNull(history);
        Assert.Equal(4, history.Count); // 2 user turns + 2 assistant responses

        var context = await _fixture.ConversationCache.GetContextAsync(sessionId);
        Assert.NotNull(context);
        Assert.Equal(3, context.Servings);
        Assert.Equal(100000m, context.TargetBudget);
    }

    [Fact]
    public async Task Chat_EmptyMessage_ShouldReturnBadRequest()
    {
        var request = new AiChatRequestDto
        {
            SessionId = "empty-sess",
            Message = "   "
        };

        var actionResult = await _fixture.Controller.Chat(request);
        Assert.IsType<BadRequestObjectResult>(actionResult.Result);
    }
}
