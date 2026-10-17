namespace BasicApiEndpoints.Middleware;

public class PipelineDemoMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PipelineDemoMiddleware> _logger;

    public PipelineDemoMiddleware(
        RequestDelegate next,
        ILogger<PipelineDemoMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        _logger.LogInformation(
            "Middleware: BEFORE {Method} {Path}",
            context.Request.Method,
            context.Request.Path);

        if (context.Request.Path == "/middleware/blocked")
        {
            _logger.LogInformation(
                "Middleware: stopping this demonstration request");

            context.Response.StatusCode =
                StatusCodes.Status400BadRequest;

            await context.Response.WriteAsJsonAsync(new
            {
                message = "Request stopped by middleware."
            });

            return;
        }

        await _next(context);

        _logger.LogInformation(
            "Middleware: AFTER {Path}, status {StatusCode}",
            context.Request.Path,
            context.Response.StatusCode);
    }
}