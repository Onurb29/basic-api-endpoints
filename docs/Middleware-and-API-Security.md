# Middleware and API Security

Project: `BasicApiEndpoints`  
Framework: ASP.NET Core / .NET 10  
Branch: `feat/middleware-openapi`

## Purpose

Extend the existing API with shared request processing, diagnostics, and a protected endpoint. These exercises use the same application as the earlier routing and serialization work.

Implemented and tested:

- Custom middleware that allows or stops requests.
- Request timing using `Stopwatch`.
- Built-in HTTP logging connected to Serilog.
- Global exception handling with a generic error response.
- JWT bearer authentication and an administrator authorization policy.
- Tests demonstrating `200`, `400`, `401`, `403`, and `404` responses.

OpenAPI, Swagger UI, and generated API clients are subsequent steps, not part of this implementation.

## 1. What middleware does

Middleware is a component in the HTTP request pipeline. It can inspect a request, perform work, and either call the next component or return a response itself.

The key operation is:

```csharp
await _next(context);
```

This runs and awaits the next component. It does not insert an artificial delay. When downstream processing returns normally, execution continues after this statement.

| Term | Meaning |
| --- | --- |
| `HttpContext` | Holds the current HTTP request, response, user, and other request-specific information. |
| `RequestDelegate` | A function that receives an `HttpContext` and returns a `Task`. |
| `_next` | The delegate for the next component in the pipeline. |
| Short-circuiting | Returning a response without invoking the remaining downstream components. |
| Endpoint handler | The application code selected for a route, such as the lambda passed to `MapGet`. |

An industrial analogy: middleware can act like a permissive before an operation. The condition determines whether execution proceeds. Logging and timing middleware observe the operation around that check.

## 2. Our pipeline order

The explicitly configured components are ordered as follows:

1. `RequestTimingMiddleware`
2. Built-in HTTP logging
3. Global exception handler
4. `PipelineDemoMiddleware`
5. Authentication
6. Authorization
7. Endpoint execution

ASP.NET Core also supplies infrastructure such as routing. This list focuses on the components configured for the exercise.

```mermaid
flowchart TD
    A["Request timing"] --> B["HTTP logging"]
    B --> C["Exception handler"]
    C --> D{"Demo blocked path?"}
    D -->|Yes| E["400 response"]
    D -->|No| F["Authentication"]
    F --> G{"Endpoint access policy"}
    G -->|No valid identity| H["401 response"]
    G -->|Required role missing| I["403 response"]
    G -->|Allowed| J["Endpoint handler"]
```

The policy branches apply to the protected administrator endpoint. Public endpoints do not require a token unless a policy is applied to them.

When downstream processing finishes, control returns through the earlier middleware. Short-circuiting stops downstream execution; it does not prevent the calling middleware from resuming after its own `await _next(context)`.

`MapGet()` and `MapControllers()` register endpoints. They do not execute the handlers at application startup. In this application's `WebApplication` setup, endpoint execution is added at the end of the pipeline. We group middleware before endpoint registrations to make the file easier to read.

## 3. Custom pipeline demonstration

File: `Middleware/PipelineDemoMiddleware.cs`

```csharp
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
```

Register this middleware once:

```csharp
app.UseMiddleware<PipelineDemoMiddleware>();
```

It runs for requests that reach its position in the pipeline, including controller routes and the serialization-security endpoint. Its blocking condition applies only to `/middleware/blocked`.

The `400` on that path is a deliberate demonstration response. It does not mean the test request was malformed. This path check is not our real authentication mechanism.

### Demonstration endpoints

```csharp
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
    logger.LogInformation("Endpoint: this should never run");

    return Results.Ok(new
    {
        message = "You should never receive this response."
    });
});
```

The blocked endpoint exists to demonstrate that middleware can stop execution before its handler runs.

## 4. Request timing

File: `Middleware/RequestTimingMiddleware.cs`

```csharp
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
```

`GetTimestamp()` marks the beginning of the measurement. `GetElapsedTime(start)` returns the elapsed duration. `Stopwatch` is designed for elapsed-time measurements.

The `finally` block runs when downstream processing exits normally or throws. It records duration without swallowing the exception. The timestamp is a local variable, so each request has its own measurement.

Placing timing before our exception handler includes the time spent constructing a handled error response. It also measures requests stopped by demo or authorization middleware.

This measures elapsed processing time from the timer's position, including awaited work. It is not CPU time or the complete client-observed duration. It excludes client processing and does not measure the full network round trip.

## 5. Built-in HTTP logging and Serilog

Add the import:

```csharp
using Microsoft.AspNetCore.HttpLogging;
```

Register services before `builder.Build()`:

```csharp
builder.Services.AddHttpLogging(logging =>
{
    logging.LoggingFields =
        HttpLoggingFields.RequestMethod |
        HttpLoggingFields.RequestPath |
        HttpLoggingFields.ResponseStatusCode;

    logging.CombineLogs = true;
});
```

`|` combines the selected enum flags. `CombineLogs` groups the selected request and response information into one log event.

Enable the middleware after timing:

```csharp
app.UseMiddleware<RequestTimingMiddleware>();
app.UseHttpLogging();
```

Under the existing `Serilog.MinimumLevel.Override` object in `appsettings.json`, retain the existing entries and include:

```json
"Override": {
  "Microsoft": "Warning",
  "Microsoft.AspNetCore.HttpLogging": "Information",
  "Microsoft.Hosting.Lifetime": "Information",
  "System": "Warning"
}
```

The more specific HTTP logging override allows these information messages through the broader Microsoft warning filter.

`ILogger<T>` uses the application's configured provider, Serilog. Our existing sinks write to the console and daily files using the pattern `logs/myapp-.txt`.

The selected HTTP fields do not include request bodies, response bodies, query strings, or the `Authorization` header. Consequently, this configuration does not record the password body or bearer token. Avoid changing it to `HttpLoggingFields.All` for this exercise.

### Logs and HTTP responses are separate

| Operation | Destination |
| --- | --- |
| `_logger.LogInformation(...)` | Server terminal and configured log files. |
| `Results.Ok(...)` | HTTP response sent to the client. |
| `context.Response.WriteAsJsonAsync(...)` | JSON written to the HTTP response. |

REST Client displays the HTTP response. The timing message appears in the terminal running `dotnet run`, not automatically in REST Client's response body.

An observed successful request produced:

```text
Middleware: BEFORE GET /middleware/pass
Endpoint: handling the request
Middleware: AFTER /middleware/pass, status 200
Request and Response:
Method: GET
PathBase:
Path: /middleware/pass
StatusCode: 200
HTTP GET /middleware/pass: pipeline took 11.01 ms
```

Durations vary. A first request can include startup effects and is not sufficient for judging application performance.

## 6. Global exception handling

The handler logs unexpected exceptions and returns a generic `500` response when the response has not started:

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

        if (context.Response.HasStarted)
        {
            throw;
        }

        context.Response.Clear();
        context.Response.StatusCode =
            StatusCodes.Status500InternalServerError;

        await context.Response.WriteAsJsonAsync(new
        {
            message = "Something went wrong."
        });
    }
});
```

The error is logged before checking `HasStarted`, ensuring this handler records the original exception even when it must rethrow. Once a response has started, the handler cannot replace its status and headers. Otherwise, `Clear()` removes pending response content and headers before constructing the generic response.

Normal validation failures, authentication challenges, and permission denials are expected responses. They do not need to throw exceptions and become `500` errors.

## 7. Authentication and authorization

| Concept | Question answered | Implementation |
| --- | --- | --- |
| Authentication | What identity does this valid token represent? | JWT bearer authentication. |
| Authorization | Is that identity allowed to access this endpoint? | `AdminOnly` policy requiring the `Administrator` role. |

Install the .NET 10 JWT bearer package:

```bash
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version '10.0.*'
```

The floating version selects a compatible .NET 10 package. Use the repository's resolved version when reproducing the exact dependency set.

Add these imports:

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Claims;
```

Register before `builder.Build()`:

```csharp
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("Administrator");
    });
});
```

Enable the security middleware after the demo middleware:

```csharp
app.UseMiddleware<PipelineDemoMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
```

Authentication runs before authorization. Explicit registration controls their position relative to our logging and exception middleware.

### Protected endpoint

```csharp
app.MapGet("/security/admin", (ClaimsPrincipal user) =>
{
    return Results.Ok(new
    {
        message = "Administrator access granted.",
        name = user.Identity?.Name
    });
})
.RequireAuthorization("AdminOnly");
```

The framework checks the policy before executing the handler. This policy applies to this endpoint; the existing public endpoints remain public.

`ClaimsPrincipal` represents the authenticated identity and claims. We explicitly return the name in the successful response. Authentication does not automatically add identity details to every HTTP response.

## 8. Local development tokens

From the API project directory:

```bash
dotnet user-secrets init

dotnet user-jwts create --name Alice --audience http://localhost:5144 --valid-for 1h

dotnet user-jwts create --name Jim --role Administrator --audience http://localhost:5144 --valid-for 1h
```

| Token | Name | Role |
| --- | --- | --- |
| Alice | `Alice` | No administrator role. |
| Jim | `Jim` | `Administrator`. |

The tool creates signed local development tokens and configures matching development authentication options. It stores the signing key through user secrets. It does not create database accounts or implement a login screen.

The bearer handler validates the token's signature, issuer, audience, and lifetime. The role comes from validated token claims; it is not established by a query parameter or by the user's name alone.

A signed JWT is not an encrypted container. Its payload can be read. Editing a claim without generating a valid matching signature does not grant a role.

Run with the project's Development launch profile:

```bash
dotnet run
```

Restart after creating the initial token configuration. Tokens expire; generate replacements when needed. Keep signing keys and actual tokens out of committed documentation and request files. A deployed application would normally obtain tokens from an identity provider and transmit them over HTTPS.

## 9. Test requests and observed results

Add this entry to `.gitignore` before storing local tokens:

```gitignore
requests.local.http
```

Create `requests.local.http` at the project root. Substitute tokens locally; the examples below deliberately contain placeholders.

```http
### No token: expect 401
GET http://localhost:5144/security/admin

### Alice: valid identity, no administrator role — expect 403
GET http://localhost:5144/security/admin
Authorization: Bearer PASTE_ALICE_TOKEN_HERE

### Jim: valid identity and administrator role — expect 200
GET http://localhost:5144/security/admin
Authorization: Bearer PASTE_JIM_TOKEN_HERE
```

Observed test outcomes:

| Request | Status | Endpoint handler executes? | Meaning |
| --- | --- | --- | --- |
| `GET /middleware/pass` | `200` | Yes. | Demo middleware calls the next component. |
| `GET /middleware/blocked` | `400` | No. | Demo middleware constructs the response. |
| `GET /this-route-does-not-exist` | `404` | No matching handler. | Middleware observes the request even without a matching route. |
| `GET /security/admin`, no token | `401` | No. | No authenticated identity established. |
| `GET /security/admin`, Alice token | `403` | No. | Identity accepted; required role missing. |
| `GET /security/admin`, Jim token | `200` | Yes. | Identity and role requirements satisfied. |

Jim's response:

```json
{
  "message": "Administrator access granted.",
  "name": "Jim"
}
```

For this JWT-protected endpoint, the observed `401` response includes `WWW-Authenticate: Bearer`. Both `401` and `403` may have empty bodies. An empty body does not mean the security check failed to run.

### Why Alice's identity is not displayed

Alice's valid token establishes her identity in `HttpContext.User`. The authorization policy then denies access because she lacks the role. The handler containing `name = user.Identity?.Name` never runs, so it cannot return her name.

For this configured endpoint, her `403` is evidence that authentication succeeded and authorization failed. There is no separate automatic "identity validated" field in the HTTP response.

### Existing serialization-security request

`POST /security/serialize-user` also passes through the configured middleware. It is still public because no authorization policy was added to that endpoint.

It continues to validate incoming values and return only the safe response model:

```json
{
  "name": "Alice",
  "email": "alice@example.com"
}
```

Password exclusion from response serialization and endpoint authorization are separate controls. Implementing one does not automatically implement the other.

## 10. Program.cs organization

Use this as a checklist, not as a replacement file with omitted handlers:

| Position | Contents |
| --- | --- |
| Imports | Existing imports plus middleware, HTTP logging, JWT bearer, and claims namespaces. |
| Builder configuration | Existing Serilog setup. |
| Service registrations | Controllers, application services, HTTP logging, authentication, authorization policy. |
| Build | `var app = builder.Build();` |
| Middleware 1 | `app.UseMiddleware<RequestTimingMiddleware>();` |
| Middleware 2 | `app.UseHttpLogging();` |
| Middleware 3 | Complete global exception-handling `app.Use(...)` block. |
| Middleware 4 | `app.UseMiddleware<PipelineDemoMiddleware>();` |
| Middleware 5 | `app.UseAuthentication();` |
| Middleware 6 | `app.UseAuthorization();` |
| Endpoint registrations | Existing controller and Minimal API mappings, demonstration endpoints, protected endpoint. |
| Start application | `app.Run();` |

Register each custom middleware once. Registering the demo middleware twice would cause normal requests to pass through its logging logic twice.

## 11. Troubleshooting

| Symptom | Check |
| --- | --- |
| Timing not visible in REST Client | Check the server terminal or log file. Timing is a log message, not response data. |
| Built-in HTTP logs missing | Check `UseHttpLogging()`, `AddHttpLogging(...)`, and the Serilog category override. |
| Duplicate demo messages | Search for duplicate `UseMiddleware<PipelineDemoMiddleware>()` registrations. |
| Valid-looking token receives `401` | Verify the complete bearer header, expiration, Development configuration, issuer, audience, and signing-key configuration. |
| Alice receives `403` | Expected for `AdminOnly`; her token lacks the required role. |
| Administrator response has an empty body | Confirm the status is `200`, the handler is the expected one, and the correct request was sent. |
| Editing code has no effect | Save, stop the current server, and restart it, or use the project's watch workflow. |

## 12. Commit checkpoints

Suggested commits used for this work:

```text
feat: demonstrate middleware flow and short-circuiting
feat: add request timing middleware
feat: configure built-in HTTP logging
feat: protect admin endpoint with JWT authentication and role authorization
```

After placing this note in the repository's `docs` folder:

```bash
git add docs/Middleware-and-API-Security.md
git diff --staged
git commit -m "docs: explain middleware diagnostics and JWT authorization"
```

These commits remain on `feat/middleware-openapi`. The module's pull request and merge are separate steps after the remaining work is ready.

## 13. Scope and next steps

The observed tests confirm the demonstration paths, timing, HTTP logging, and protected-endpoint outcomes. They do not establish that every existing API route has been tested or protected.

This exercise does not implement user registration, password storage, a production token issuer, refresh tokens, or deployment HTTPS configuration. The development token workflow supports learning and local testing.

Next: add OpenAPI documentation and Swagger UI, then generate a small API client in the same repository.

## References

- [ASP.NET Core middleware](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/?view=aspnetcore-10.0)
- [Write custom middleware](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/write?view=aspnetcore-10.0)
- [Stopwatch.GetElapsedTime](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.stopwatch.getelapsedtime?view=net-10.0)
- [HTTP logging](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-logging/?view=aspnetcore-10.0)
- [Authentication and authorization in Minimal APIs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/security?view=aspnetcore-10.0)
- [Development tokens with dotnet user-jwts](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/jwt-authn?view=aspnetcore-10.0)
