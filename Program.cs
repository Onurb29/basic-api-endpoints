using Microsoft.AspNetCore.HttpLogging;
using BasicApiEndpoints.Service;
using BasicApiEndpoints.Endpoints;
using BasicApiEndpoints.Middleware;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog as the application's logging provider.
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddHttpLogging(logging =>
{
    logging.LoggingFields =
        HttpLoggingFields.RequestMethod |
        HttpLoggingFields.RequestPath |
        HttpLoggingFields.ResponseStatusCode;

    logging.CombineLogs = true;
});

// Register application services and MVC controller support.
builder.Services.AddSingleton<IBlogService, BlogService>();
builder.Services.AddSingleton<ProductService>();
builder.Services.AddControllers();

var app = builder.Build();

app.UseMiddleware<RequestTimingMiddleware>();
app.UseHttpLogging();

// Global exception handling middleware
// Catch unhandled exceptions from requests and return a generic 500 response.
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        app.Logger.LogError(
            ex,
            "Unhandled exception occurred while processing {Method} {Path}",
            context.Request.Method,
            context.Request.Path);

        if (context.Response.HasStarted)
        {
            throw;
        }

        context.Response.Clear();
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        await context.Response.WriteAsJsonAsync(new
        {
            message = "Something went wrong."
        });
    }
});

app.UseMiddleware<PipelineDemoMiddleware>();

// Map attribute-routed controllers and serialization endpoints.
app.MapControllers();
app.MapSerializationEndpoints();
app.MapDeserializationEndpoints();
app.MapSerializationSecurityEndpoints();

// Minimal API route examples.
app.MapGet("/", () => "Root Path");
app.MapGet("/downloads", () => "Downloads");

app.MapGet("/users/{userID}/posts/{slug}", (int userID, string slug) => {
    return $"User ID: {userID}, Post ID: {slug}";
});

app.MapGet("/products/{id:int:min(0)}", (int id) => {
    return $"Product ID: {id}";
});

app.MapGet("/report/{year?}" , (int? year = 2026) => {
    return $"Report for year: {year}";
});

app.MapGet("/files/{*filepath}", (string filePath) => {
    return $"File Path: {filePath}";
});

app.MapGet("/search", (string? q, int? page = 1) => {
    if (string.IsNullOrEmpty(q))
    {
        return "No search query provided.";
    }
    return $"Search results for: {q}, Page: {page}";
});

app.MapGet("/store/{category}/{productID:int?}/{*extraPath}", (string category, int? productID, string? extraPath, bool inStock = true) => {
    return $"Category: {category}, Product ID: {productID}, Extra Path: {extraPath}, In Stock: {inStock}";
});

app.MapPut("/" , () => "This is a PUT request to the root path");
app.MapDelete("/", () => "Delete request to the root path");
app.MapPost("/", () => "POST request to the root path");


app.MapGet("/middleware/pass", (ILogger<Program> logger) =>
{
    logger.LogInformation("Endpoint: handling the request");

    return Results.Ok(new
    {
        message = "Request reached the endpoint."
    });
});

app.MapGet("/middleware/blocked", (ILogger<Program> logger) =>
{
    logger.LogInformation(
        "Endpoint: this should never run");

    return Results.Ok(new
    {
        message = "You should never receive this response."
    });
});

app.Run();


