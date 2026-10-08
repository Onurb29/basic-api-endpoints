# Serilog Configuration with `appsettings.json`

Instead of hardcoding Serilog configuration directly in `Program.cs`, the logging configuration was moved into `appsettings.json`.

This keeps application code cleaner and makes logging behavior easier to change between environments without recompiling the application.

### `Program.cs`

The Serilog setup was changed from hardcoded sinks and log levels to configuration-based setup:

```csharp
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();
```

This tells Serilog to read its configuration from the ASP.NET Core configuration system.

ASP.NET Core automatically loads configuration from files such as:

```text
appsettings.json
appsettings.Development.json
appsettings.Production.json
```

The application code does not need to change when logging settings change.

---

### `appsettings.json`

Serilog is configured with:

```json
{
  "Serilog": {
    "Using": [
      "Serilog.Sinks.Console",
      "Serilog.Sinks.File"
    ],

    "MinimumLevel": {
      "Default": "Information",

      "Override": {
        "Microsoft": "Warning",
        "Microsoft.Hosting.Lifetime": "Information",
        "System": "Warning"
      }
    },

    "WriteTo": [
      {
        "Name": "Console"
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/myapp-.txt",
          "rollingInterval": "Day"
        }
      }
    ]
  }
}
```

---

### Log Levels

The default application log level is:

```text
Information
```

This includes:

```text
Information
Warning
Error
Critical
```

while excluding lower-level messages such as:

```text
Debug
Trace
```

Framework logging is reduced using:

```json
"Microsoft": "Warning"
```

This prevents the logs from being flooded with normal ASP.NET Core framework activity.

However:

```json
"Microsoft.Hosting.Lifetime": "Information"
```

keeps useful startup and shutdown information such as:

```text
Now listening on: http://localhost:5144
Application started
Hosting environment: Development
```

---

### Why Use Configuration Instead of Hardcoding?

Previously, the logging configuration could be written directly in `Program.cs`:

```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File(
        "logs/myapp-.txt",
        rollingInterval: RollingInterval.Day)
    .CreateLogger();
```

This works, but changing logging behavior requires changing application code.

Using configuration separates the two concerns:

```text
Application Code
      ↓
Logging Abstraction
      ↓
Serilog
      ↓
appsettings.json
      ↓
Console / File
```

This makes the application easier to configure for different environments.

For example:

```text
Development
    ↓
Information or Debug logging

Production
    ↓
Warning or Error logging
```

The application code can stay unchanged.

---

### `ILogger<T>` Still Stays the Same

Controllers continue to use:

```csharp
ILogger<ErrorHandlingController>
```

Example:

```csharp
_logger.LogWarning(
    "Division request rejected because the denominator was zero.");
```

The controller does not need to know that Serilog is being used.

The architecture is:

```text
Controller
    ↓
ILogger<T>
    ↓
ASP.NET Core Logging Abstraction
    ↓
Serilog
    ↓
appsettings.json configuration
    ↓
Console + File
```

This keeps the application decoupled from the specific logging implementation.

---

### Key Takeaway

The final logging setup is:

```text
ILogger<T>
    ↓
Serilog
    ↓
Configuration from appsettings.json
    ↓
Console
    +
Daily rolling log files
```

This is more scalable than hardcoding logging settings in `Program.cs` and makes it easier to use different logging levels and destinations in different environments.