using BasicApiEndpoints.Service;
using Serilog;


var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File(
        "logs/myapp-.txt",
        rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddSingleton<IBlogService, BlogService>();
builder.Services.AddSingleton<ProductService>();
builder.Services.AddControllers();

var app = builder.Build();

// Global exception handling middleware
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

        context.Response.StatusCode =
            StatusCodes.Status500InternalServerError;

        await context.Response.WriteAsJsonAsync(new
        {
            message = "Something went wrong."
        });
    }
});

app.MapControllers();

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
app.Run();


