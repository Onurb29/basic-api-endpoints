# Controller Wiring

This project uses MVC controllers for its blog and product resource endpoints. The controller actions delegate data operations to services, while `Program.cs` configures dependency injection and maps the controllers.

## Register services and controllers

Register the services and MVC controller support before building the app, then map attribute-routed controllers after `Build()`:

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<BlogService>();
builder.Services.AddSingleton<ProductService>();
builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();
```

`AddControllers()` registers the MVC services. `MapControllers()` makes the controller actions available as HTTP endpoints. `BlogService` and `ProductService` are registered so ASP.NET Core can provide them to controllers through dependency injection.

## Controller routing and injection

`BlogsController` inherits from `ControllerBase`. Its route and HTTP method attributes define its URLs, and its constructor receives `BlogService` from dependency injection:

```csharp
[ApiController]
[Route("api/blogs")]
public class BlogsController : ControllerBase
{
    private readonly BlogService _blogService;

    public BlogsController(BlogService blogService)
    {
        _blogService = blogService;
    }

    [HttpGet("{id}")]
    public IActionResult GetBlogById(int id)
    {
        var blog = _blogService.GetBlog(id);
        return blog is null
            ? NotFound($"Blog with ID {id} not found.")
            : Ok(blog);
    }
}
```

`[Route("api/blogs")]` provides the controller's route prefix. `[HttpGet("{id}")]` adds the ID segment, producing `GET /api/blogs/{id}`. The `ProductController` follows the same pattern with the prefix `/api/products`.

## Blog routes

| Method | Route | Result |
| --- | --- | --- |
| `GET` | `/api/blogs` | Returns all blogs. |
| `GET` | `/api/blogs/{id}` | Returns one blog, or `404 Not Found`. |
| `POST` | `/api/blogs` | Creates a blog and returns `201 Created`. |
| `PUT` | `/api/blogs/{id}` | Updates a blog and returns `200 OK`, or `404 Not Found`. |
| `DELETE` | `/api/blogs/{id}` | Deletes a blog and returns `204 No Content`, or `404 Not Found`. |

The corresponding requests are in `requests.http`. For example:

```http
GET http://localhost:5144/api/blogs
```

Keep the HTTP responsibility in the controller: it reads route/body inputs and chooses status codes. Keep list lookup and mutation in the service. The services currently use in-memory lists, so data created at runtime is lost when the app restarts.
