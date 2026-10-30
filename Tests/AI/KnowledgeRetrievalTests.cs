using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using SmartSupermarket.Backend.Features.AI.DTOs.Common;
using SmartSupermarket.Backend.Features.AI.DTOs.Requests;
using SmartSupermarket.Backend.Features.AI.Services;
using SmartSupermarket.Backend.Features.AI.Services.Implementations;
using SmartSupermarket.Backend.Features.AI.Services.Knowledge.Implementations;
using SmartSupermarket.Backend.Features.AI.Tools;
using SmartSupermarket.Backend.Infrastructure.External.AI;
using SmartSupermarket.Backend.Infrastructure.Persistence;
using Xunit;

namespace SmartSupermarket.Backend.Tests.AI;

public class KnowledgeRetrievalTests
{
    private readonly KnowledgeRetrievalService _retrievalService;
    private readonly AppDbContext _dbContext;
    private readonly Mock<ILlmClient> _llmClientMock;
    private readonly AiShoppingConciergeService _conciergeService;

    public KnowledgeRetrievalTests()
    {
        _retrievalService = new KnowledgeRetrievalService(null);

        var dbName = $"KnowledgeTestDb_{Guid.NewGuid()}";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        _dbContext = new AppDbContext(options);
        _llmClientMock = new Mock<ILlmClient>();

        _llmClientMock
            .Setup(l => l.GenerateAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<bool>(), default))
            .ReturnsAsync((string)null!);

        var retailTool = new RetailTool(_dbContext);
        var retailService = new RetailShoppingIntelligenceService(_dbContext, retailTool, _llmClientMock.Object);
        _conciergeService = new AiShoppingConciergeService(_dbContext, _llmClientMock.Object, retailService, _retrievalService);
    }

    [Fact]
    public async Task RetrieveAsync_ReturnsRelevantChunks_WhenQueryMatchesFaq()
    {
        // Act
        var result = await _retrievalService.RetrieveAsync("Hàng flash sale cận date có an toàn không?");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.HasRelevantKnowledge);
        Assert.NotEmpty(result.Chunks);
        var top = result.Chunks[0];
        Assert.Contains("Flash Sale", top.Title);
        Assert.Contains("0°C - 4°C", top.Content);
        Assert.Equal("docs/10_AI/knowledge/business_rules/faq/customer_faq.md", top.SourceDoc);
    }

    [Fact]
    public async Task RetrieveAsync_ReturnsPolicy_WhenQueryMatchesReturnPolicy()
    {
        // Act
        var result = await _retrievalService.RetrieveAsync("Đổi trả thực phẩm tươi sống");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.HasRelevantKnowledge);
        var chunk = result.Chunks.Find(c => c.Id == "faq-return-policy");
        Assert.NotNull(chunk);
        Assert.Contains("24h", chunk.Content);
    }

    [Fact]
    public async Task GetExactMatchFaqAsync_ReturnsDirectMatch_ForPerishableReturn()
    {
        // Act
        var match = await _retrievalService.GetExactMatchFaqAsync("Tôi có được đổi trả thực phẩm tươi sống không?");

        // Assert
        Assert.NotNull(match);
        Assert.Equal("faq-return-policy", match.Id);
        Assert.Equal(KnowledgeType.FaqPolicy, match.Type);
    }

    [Fact]
    public async Task GroundPromptWithKnowledgeAsync_InjectsOfficialKnowledgeChunk()
    {
        // Act
        var grounded = await _retrievalService.GroundPromptWithKnowledgeAsync("bảo quản thịt tươi sống");

        // Assert
        Assert.NotEmpty(grounded);
        Assert.Contains("[OFFICIAL SUPERMARKET KNOWLEDGE & POLICIES]", grounded);
        Assert.Contains("0°C - 4°C", grounded);
    }

    [Fact]
    public async Task PlanMealAndShopAsync_AnswersFaqWithGroundTruthAndSource_WithoutHallucination()
    {
        // Act
        var request = new MealPlanChatRequest { UserPrompt = "Hàng flash sale cận date có an toàn không?" };
        var response = await _conciergeService.PlanMealAndShopAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.False(response.HasMealSuggestion);
        Assert.False(response.HasRetailRecommendation);
        Assert.NotNull(response.KnowledgeSource);
        Assert.Contains("customer_faq.md", response.KnowledgeSource);
        Assert.NotEmpty(response.GroundedFacts);
        Assert.Contains("0°C - 4°C", response.AssistantMessage);
        Assert.Contains("Căn cứ chính sách siêu thị", response.AssistantMessage);
    }

    [Fact]
    public async Task PlanMealAndShopAsync_HandlesOutDomainSafely_WithFallback()
    {
        // Act: Non-supermarket query
        var request = new MealPlanChatRequest { UserPrompt = "Thời tiết ngày mai thế nào?" };
        var response = await _conciergeService.PlanMealAndShopAsync(request);

        // Assert: System gracefully responds without fabricating supermarket products
        Assert.NotNull(response);
        Assert.False(response.HasMealSuggestion);
        Assert.False(response.HasRetailRecommendation);
    }
}
