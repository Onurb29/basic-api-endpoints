# Service Dependency Injection

ASP.NET Core minimal API endpoints can receive registered services as handler parameters. This lets endpoint code use a service without constructing it directly.

## Register the service

Register `BlogService` before building the application:

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<BlogService>();

var app = builder.Build();
```

The service container uses this registration to provide `BlogService` to endpoint handlers.

## Receive the service in an endpoint

```csharp
app.MapGet("/blogs/{id:int}", (int id, BlogService blogService) =>
{
    var blog = blogService.GetBlog(id);

    IResult result = blog is null
        ? Results.NotFound($"Blog with ID {id} not found.")
        : Results.Ok(blog);

    return result;
});
```

The handler parameters come from different sources:

- `id` is bound from `{id:int}` in the route.
- `blogService` is resolved from dependency injection because `BlogService` was registered with the service container.

`BlogService` is the parameter type, and `blogService` is the local name used inside the handler. ASP.NET Core supplies the registered service when the endpoint runs; the handler does not need to call `new BlogService()`.

## Get all blogs

`BlogService` owns the in-memory list, so add a method there to return its contents:

```csharp
public IReadOnlyList<Blog> GetAllBlogs()
{
    return _blogs.ToList();
}
```

Map `GET /blogs` to that method. This route returns the collection, while `GET /blogs/{id:int}` returns one blog by ID:

```csharp
app.MapGet("/blogs", (BlogService blogService) =>
    Results.Ok(blogService.GetAllBlogs()));
```

The HTTP request for the collection is:

```http
GET http://localhost:5144/blogs
```

## Add a blog

The in-memory list also belongs to `BlogService`, so add the new blog there. The service assigns the ID and returns the created object:

```csharp
public Blog AddBlog(string title, string content)
{
    var newBlog = new Blog
    {
        Id = _nextId++,
        Title = title,
        Content = content
    };
    _blogs.Add(newBlog);
    return newBlog;
}
```

The POST endpoint accepts a `CreateBlogRequest` without an ID, asks the service to create and store the blog, then returns `201 Created` with its URL and representation:

```csharp
app.MapPost("/blogs", (CreateBlogRequest request, BlogService blogService) =>
{
    var blog = blogService.AddBlog(request.Title, request.Content);
    return Results.Created($"/blogs/{blog.Id}", blog);
});
```

Keep `AddBlog` in `BlogService.cs`, not `Program.cs`, because `_blogs` is private to the service. The request body contains `title` and `content`; the server supplies the ID.

## Delete a blog

Keep the collection operation in `BlogService`. `DeleteBlog` looks up the blog by ID, removes it when found, and returns `true` only when an item was removed:

```csharp
public bool DeleteBlog(int id)
{
    var blog = GetBlog(id);
    return blog is not null && _blogs.Remove(blog);
}
```

The endpoint calls the service and translates that boolean into an HTTP response:

```csharp
app.MapDelete("/blogs/{id:int}", (int id, BlogService blogService) =>
{
    IResult result = blogService.DeleteBlog(id)
        ? Results.NoContent()
        : Results.NotFound($"Blog with ID {id} not found.");

    return result;
});
```

Send a DELETE request from `requests.http` to test it:

```http
DELETE http://localhost:5144/blogs/2
```

Deleting an existing blog returns `204 No Content`, which intentionally has no response body. Deleting an ID that is not present returns `404 Not Found`. A subsequent `GET /blogs/2` can confirm that the item was removed.

## Service lifetime in this example

`AddSingleton<BlogService>()` creates one service instance for the lifetime of the application. All requests handled by that running process share the same in-memory list, so a blog added by POST is available to later GET requests. This is not durable storage: stopping or restarting the application recreates the seeded list and loses blogs added at runtime. Persisting data across restarts requires storage such as a database. A database-backed service commonly uses a scoped lifetime so its instance is shared within a request.
