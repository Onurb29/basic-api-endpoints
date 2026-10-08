# Error Handling and Logging

This exercise added **local error handling**, **global exception handling**, and **logging with Serilog** to the ASP.NET Core API.

---

## Local Error Handling

Local error handling is useful when the application already knows that a specific condition can occur.

Example: dividing by zero.

In `ErrorHandlingController.cs`:

```csharp
[ApiController]
[Route("api/[controller]")]
public class ErrorHandlingController : ControllerBase
{
    private readonly ILogger<ErrorHandlingController> _logger;

    public ErrorHandlingController(
        ILogger<ErrorHandlingController> logger)
    {
        _logger = logger;
    }

    [HttpGet("division")]
    public IActionResult GetDivisionResult(
        int numerator,
        int denominator)
    {
        if (denominator == 0)
        {
            _logger.LogWarning(
                "Division request rejected because the denominator was zero.");

            return BadRequest("Cannot divide by zero.");
        }

        return Ok(new
        {
            result = numerator / denominator
        });
    }
}
```

If the denominator is `0`, the API returns:

```text
400 Bad Request
```

This is an **expected error**, so it is handled directly inside the controller.

### Key Idea

```text
Known / expected problem
        ↓
Controller handles it
        ↓
400 Bad Request
```

---

## Global Error Handling

Not every possible exception should require a `try/catch` block inside every controller.

A global exception handler can act as a safety net for errors that were not handled elsewhere.

The global error handler was added as middleware in `Program.cs`.

```csharp
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
```

The important part is:

```csharp
try
{
    await next();
}
catch (Exception ex)
{
    // Handle unexpected exception
}
```

`await next()` allows the HTTP request to continue through the rest of the ASP.NET Core request pipeline.

If something later in the pipeline throws an exception, control returns to this middleware and the `catch` block handles it.

---

## Request Flow

```text
HTTP Request
    ↓
Global Error Middleware
    ↓
Routing
    ↓
Controller / Endpoint
    ↓
Service
    ↓
Response
```

If an exception occurs:

```text
HTTP Request
    ↓
Global Error Middleware
    ↓
Controller
    ↓
Exception thrown
    ↑
Exception propagates
    ↑
Global Middleware catches it
    ↓
Log exception
    ↓
Return HTTP 500
```

---

## Testing the Global Error Handler

A temporary test endpoint was created:

```csharp
[HttpGet("global-test")]
public IActionResult GlobalErrorTest()
{
    throw new InvalidOperationException(
        "Testing global exception handling.");
}
```

Request:

```http
GET http://localhost:5144/api/ErrorHandling/global-test
```

The controller intentionally throws:

```text
System.InvalidOperationException
```

The global middleware catches it and returns:

```text
HTTP/1.1 500 Internal Server Error
```

with:

```json
{
  "message": "Something went wrong."
}
```

---

## Why Not Return the Exception to the Client?

The API should not normally expose internal implementation details such as:

- stack traces
- file paths
- database connection information
- server names
- internal class names
- detailed exception messages

Instead:

```text
Client
  ↓
Safe error message

Server logs
  ↓
Full technical details
```

Example client response:

```json
{
  "message": "Something went wrong."
}
```

The detailed exception remains in the server logs.

---

# Logging with `ILogger<T>`

ASP.NET Core provides a logging abstraction through:

```csharp
ILogger<T>
```

Example:

```csharp
private readonly ILogger<ErrorHandlingController> _logger;
```

The logger is injected into the controller through Dependency Injection:

```csharp
public ErrorHandlingController(
    ILogger<ErrorHandlingController> logger)
{
    _logger = logger;
}
```

This is another example of Dependency Injection:

```text
ErrorHandlingController
        ↓
asks for ILogger<ErrorHandlingController>
        ↓
ASP.NET Core DI Container
        ↓
Logging implementation
```

The controller does not need to manually create a logger.

---

# Serilog

Serilog was added as the application's logging provider.

Packages used:

```bash
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.File
```

Serilog is configured in `Program.cs`.

```csharp
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File(
        "logs/myapp-.txt",
        rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();
```

The application now writes logs to:

```text
Console
   +
Daily log files
```

Example:

```text
logs/
└── myapp-20261008.txt
```

---

## Keep Using `ILogger<T>`

Even though Serilog is the logging provider, controllers can continue using:

```csharp
ILogger<ErrorHandlingController>
```

instead of depending directly on Serilog.

Architecture:

```text
Controller
    ↓
ILogger<T>
    ↓
ASP.NET Core Logging
    ↓
Serilog
    ↓
Console + File
```

This keeps the controller decoupled from the specific logging provider.

---

# Example Serilog Output

When the `global-test` endpoint threw an exception, Serilog recorded:

```text
[ERR] Unhandled exception occurred while processing
GET /api/ErrorHandling/global-test

System.InvalidOperationException:
Testing global exception handling.
```

The log also contained the stack trace and the location where the exception occurred.

The request finished with:

```text
500 Internal Server Error
```

This confirms that:

1. The controller threw the exception.
2. The exception propagated through ASP.NET Core.
3. The global middleware caught it.
4. Serilog recorded the exception.
5. The client received a safe HTTP `500` response.

---

# Local vs Global Error Handling

## Local Error Handling

Use local handling when the error is expected and meaningful to the specific operation.

Example:

```text
denominator == 0
```

Result:

```text
400 Bad Request
```

Flow:

```text
Expected problem
    ↓
Controller handles it
    ↓
4xx response
```

---

## Global Error Handling

Use global handling for unexpected exceptions that were not handled elsewhere.

Example:

```csharp
throw new InvalidOperationException();
```

Flow:

```text
Unexpected exception
    ↓
Global middleware
    ↓
Log exception
    ↓
500 Internal Server Error
```

---

# HTTP Error Categories

A useful distinction is:

```text
4xx = Client-side problem
5xx = Server-side problem
```

Examples:

```text
400 Bad Request
→ Invalid input

404 Not Found
→ Requested resource does not exist

500 Internal Server Error
→ Unexpected application failure
```

---

# Middleware and Error Handling

The global error handler is middleware because it participates in the HTTP request pipeline.

```text
Request
   ↓
Middleware
   ↓
Controller
   ↓
Response
```

Middleware can inspect, modify, log, reject, or pass a request to the next component.

The exception middleware surrounds the rest of the pipeline:

```text
┌──────────────────────────────┐
│ Global Exception Middleware  │
│                              │
│ try                          │
│   ↓                          │
│ Controller                   │
│   ↓                          │
│ Service                      │
│                              │
│ catch Exception              │
│   ↓                          │
│ Log + HTTP 500               │
└──────────────────────────────┘
```

---

# `app.UseRouting()`

With the modern ASP.NET Core `WebApplication` hosting model, routing can be configured automatically when endpoints are mapped with methods such as:

```csharp
app.MapControllers();
app.MapGet(...);
app.MapPost(...);
```

Therefore, explicitly adding:

```csharp
app.UseRouting();
```

is not always required in a modern Minimal Hosting application.

---

# Current Architecture

```text
HTTP Client
     ↓
Global Exception Middleware
     ↓
ASP.NET Core Routing
     ↓
Controllers / Minimal APIs
     ↓
Services
     ↓
Models / Data
```

Logging runs across the application:

```text
Application
    ↓
ILogger<T>
    ↓
Serilog
    ├── Console
    └── Log File
```

---

# Key Takeaways

- Handle **known and expected errors locally**.
- Handle **unexpected exceptions globally**.
- Return appropriate HTTP status codes.
- Use `400 Bad Request` for invalid client input.
- Use `500 Internal Server Error` for unexpected application failures.
- Do not expose detailed stack traces to API clients.
- Log technical details on the server.
- `ILogger<T>` provides a logging abstraction.
- Serilog can act as the logging implementation behind `ILogger<T>`.
- Global exception handling is implemented as middleware.
- `await next()` allows the request to continue through the HTTP pipeline.
- An exception later in the pipeline propagates back to the global middleware.
- Logging and error handling are separate concerns:
  - Error handling controls the response.
  - Logging records what happened.

---

## Mental Model

```text
Expected Error
    ↓
Handle locally
    ↓
Return useful 4xx response


Unexpected Exception
    ↓
Global middleware catches it
    ↓
ILogger logs it
    ↓
Serilog stores it
    ↓
Return safe 500 response
```