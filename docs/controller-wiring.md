# Controller Wiring and Service Abstraction

This project uses MVC controllers for its blog and product resource APIs. Controllers handle HTTP concerns, services perform application operations, and models represent data and request contracts.

## Register Services and Controllers

Register services and controller support before building the application. Map controller routes after `Build()`:

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IBlogService, BlogService>();
builder.Services.AddSingleton<ProductService>();
builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();
```

`AddControllers()` registers MVC support. `MapControllers()` exposes attribute-routed actions. The `IBlogService` registration tells dependency injection to provide `BlogService` whenever a consumer requests `IBlogService`.

## The Service Interface

`IBlogService` defines which blog operations are available without specifying how they are implemented:

```csharp
public interface IBlogService
{
    IReadOnlyList<Blog> GetAllBlogs();
    Blog? GetBlog(int id);
    Blog AddBlog(string title, string content);
    bool DeleteBlog(int id);
    Blog? UpdateBlog(int id, string title, string content);
}
```

`BlogService` implements this contract using the current in-memory list. A future implementation could use a database or remote API while preserving the same operations.

## Controller Dependency Injection

`BlogsController` depends on `IBlogService`, not on the concrete `BlogService` class:

```csharp
[ApiController]
[Route("api/blogs")]
public class BlogsController : ControllerBase
{
    private readonly IBlogService _blogService;

    public BlogsController(IBlogService blogService)
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

When ASP.NET Core creates `BlogsController`, it sees the `IBlogService` constructor parameter and asks the dependency injection container for it. The container uses the registration in `Program.cs` and supplies a `BlogService` instance.

`[Route("api/blogs")]` sets the controller's route prefix. `[HttpGet("{id}")]` adds an ID segment, so that action responds to `GET /api/blogs/{id}`. `ProductController` uses the `/api/products` prefix.

## Request and Startup Flow

Application startup follows this order:

```text
CreateBuilder
      |
Register services
      |
Build application
      |
Map controllers and endpoints
      |
Run
```

A blog request flows through these layers:

```text
HTTP request
     |
     v
BlogsController (HTTP input and response)
     |
     v
IBlogService (stable application contract)
     |
     v
BlogService (application logic)
     |
     v
In-memory list (current data source)
```

The controller understands HTTP. The service understands the application's operations. The model represents the data. The interface lets the controller depend on the operations it needs rather than the class that performs them.

In this project, `Controllers/` contains the structured REST APIs, `Services/` contains application logic and service contracts, `Models/` contains data and request contracts, and the remaining Minimal API mappings in `Program.cs` are routing demonstrations.

## PI AF and OT Analogy

The architectural idea has a useful, though not exact, parallel in PI and OT systems:

```text
Application consumer
       |
       v
IBlogService (stable software contract)
       |
       v
BlogService (selected implementation)
       |
       v
In-memory list, database, or remote API
```

Conceptually, this resembles a consumer reading through an AF Attribute and its data reference or calculation to a PI Point or another data source:

```text
Consumer
   |
   v
AF Attribute
   |
   v
Data reference or calculation
   |
   v
PI Point or another data source
```

This is an analogy about decoupling consumers from data sources, not a one-to-one equivalence between AF objects and C# interfaces. `IBlogService` gives the controller a stable contract, and dependency injection selects the implementation. If a new service implements the same interface, the controller can keep working while the registration changes.

## Blog Routes

| Method | Route | Result |
| --- | --- | --- |
| `GET` | `/api/blogs` | Returns all blogs. |
| `GET` | `/api/blogs/{id}` | Returns one blog, or `404 Not Found`. |
| `POST` | `/api/blogs` | Creates a blog and returns `201 Created`. |
| `PUT` | `/api/blogs/{id}` | Updates a blog and returns `200 OK`, or `404 Not Found`. |
| `DELETE` | `/api/blogs/{id}` | Deletes a blog and returns `204 No Content`, or `404 Not Found`. |

The corresponding sample requests are in `requests.http`, for example:

```http
GET http://localhost:5144/api/blogs
```

The services currently use in-memory lists. Data created at runtime is available while the application is running but is lost when it restarts.
