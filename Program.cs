using BasicApiEndpoints.Models;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var blogs = new List<Blog>
{
    new Blog { Id = 1, Title = "First Blog", Content = "This is the first blog post." },
    new Blog { Id = 2, Title = "Second Blog", Content = "This is the second blog post." },
    new Blog { Id = 3, Title = "Third Blog", Content = "This is the third blog post." }
};  

app.MapGet("/", () => "Root Path");
app.MapGet("/downloads", () => "Downloads");

app.MapGet("/blogs/{id}", (int id) => {
    return blogs[id - 1]; // Adjusting for 0-based indexing     
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


