using System;
using System.Threading;
using Microsoft.AspNetCore.Http;

namespace SmartSupermarket.Backend.Infrastructure.Logging;

public class CorrelationIdAccessor : ICorrelationIdAccessor
{
    private static readonly AsyncLocal<string?> AsyncLocalCorrelationId = new();
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public CorrelationIdAccessor(IHttpContextAccessor? httpContextAccessor = null)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string CorrelationId
    {
        get
        {
            if (_httpContextAccessor?.HttpContext?.Items.TryGetValue("CorrelationId", out var itemVal) == true &&
                itemVal is string httpVal && !string.IsNullOrWhiteSpace(httpVal))
            {
                return httpVal;
            }

            if (!string.IsNullOrWhiteSpace(AsyncLocalCorrelationId.Value))
            {
                return AsyncLocalCorrelationId.Value;
            }

            var generated = Guid.NewGuid().ToString("N");
            AsyncLocalCorrelationId.Value = generated;
            return generated;
        }
    }

    public void SetCorrelationId(string correlationId)
    {
        AsyncLocalCorrelationId.Value = correlationId;
        if (_httpContextAccessor?.HttpContext != null)
        {
            _httpContextAccessor.HttpContext.Items["CorrelationId"] = correlationId;
        }
    }
}
