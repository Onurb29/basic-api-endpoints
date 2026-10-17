using System.Diagnostics;

namespace BasicApiEndpoints.Middleware;

public class RequestTimingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestTimingMiddleware> _logger;

    public RequestTimingMiddleware(
        RequestDelegate next,
        ILogger<RequestTimingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            await _next(context);
        }
        finally
        {
            var elapsed = Stopwatch.GetElapsedTime(start);

            _logger.LogInformation(
                "HTTP {Method} {Path}: pipeline took {ElapsedMilliseconds:0.00} ms",
                context.Request.Method,
                context.Request.Path,
                elapsed.TotalMilliseconds);
        }
    }
}