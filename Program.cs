using BasicApiEndpoints.Models;
using BasicApiEndpoints.Service;


var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<BlogService>();

var app = builder.Build();


app.MapGet("/", () => "Root Path");
app.MapGet("/downloads", () => "Downloads");

app.MapGet("/blogs", (BlogService blogService) =>
    Results.Ok(blogService.GetAllBlogs()));

app.MapGet("/blogs/{id:int}", (int id, BlogService blogService) =>
{
    var blog = blogService.GetBlog(id);

    IResult result = blog is null
        ? Results.NotFound($"Blog with ID {id} not found.")
        : Results.Ok(blog);

    return result;
});

app.MapPost("/blogs", (CreateBlogRequest request, BlogService blogService) =>
{
    var blog = blogService.AddBlog(request.Title, request.Content);
    // In a real application, you would save the blog to a database or perform other actions.
    // For this example, we'll just return the created blog with a 201 Created status.
    return Results.Created($"/blogs/{blog.Id}", blog);
});

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


