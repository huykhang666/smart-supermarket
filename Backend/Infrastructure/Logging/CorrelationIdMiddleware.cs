using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace SmartSupermarket.Backend.Infrastructure.Logging;

public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-ID";
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ICorrelationIdAccessor correlationIdAccessor)
    {
        string correlationId;
        if (context.Request.Headers.TryGetValue(CorrelationIdHeader, out var headerValue) &&
            !string.IsNullOrWhiteSpace(headerValue))
        {
            correlationId = headerValue.ToString();
        }
        else
        {
            correlationId = Guid.NewGuid().ToString("N");
        }

        correlationIdAccessor.SetCorrelationId(correlationId);
        context.Response.Headers[CorrelationIdHeader] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("[HTTP IN] {Method} {Path} (CorrelationId: {CorrelationId})",
                context.Request.Method, context.Request.Path, correlationId);

            try
            {
                await _next(context);
                stopwatch.Stop();

                _logger.LogInformation("[HTTP OUT] {Method} {Path} phản hồi {StatusCode} trong {ElapsedMs}ms (CorrelationId: {CorrelationId})",
                    context.Request.Method, context.Request.Path, context.Response.StatusCode, stopwatch.ElapsedMilliseconds, correlationId);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "[HTTP ERROR] {Method} {Path} thất bại sau {ElapsedMs}ms (CorrelationId: {CorrelationId}): {Message}",
                    context.Request.Method, context.Request.Path, stopwatch.ElapsedMilliseconds, correlationId, ex.Message);
                throw;
            }
        }
    }
}
