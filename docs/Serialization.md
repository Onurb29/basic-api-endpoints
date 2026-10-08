# Serialization in ASP.NET Core

Serialization converts an object in application memory into a format that can be transmitted or stored.

For a web API, this commonly means converting a C# object into JSON.

```text
C# Object
    ↓
Serialization
    ↓
JSON / XML
    ↓
HTTP Response
```

The receiving application can later perform the reverse operation:

```text
JSON / XML
    ↓
Deserialization
    ↓
Object in memory
```

---

# Why Serialization Matters

Inside the application, data exists as C# objects.

Example:

```csharp
var samplePerson = new SerializationSample
{
    UserName = "Alice",
    UserAge = 30
};
```

That object cannot simply be transferred across a network in its in-memory form.

It must first be converted into a standard data format such as:

```json
{
  "userName": "Alice",
  "userAge": 30
}
```

JSON is the most common format used by modern web APIs.

---

# Project Structure

The serialization examples were added to the existing `BasicApiEndpoints` project instead of creating a separate tutorial project.

```text
BasicApiEndpoints/
├── Controllers/
├── Endpoints/
│   └── SerializationEndpoints.cs
├── Models/
│   └── SerializationSample.cs
├── Service/
├── Program.cs
└── requests.http
```

This keeps the serialization exercise isolated without filling `Program.cs` with additional route logic.

---

# Serialization Model

`Models/SerializationSample.cs`

```csharp
namespace BasicApiEndpoints.Models;

public class SerializationSample
{
    public required string UserName { get; set; }

    public int UserAge { get; set; }
}
```

The model defines the **shape of the data**.

It does not perform serialization itself.

```text
SerializationSample
        ↓
Defines data

System.Text.Json / ASP.NET Core
        ↓
Performs serialization
```

There was no need to create a custom `SerializationService` because .NET already provides the serialization functionality required by this exercise.

A service would make more sense if the application later had reusable business functionality such as:

```text
ExportService
    ├── ExportToJson()
    ├── ExportToXml()
    └── ExportToCsv()
```

---

# Serialization Endpoints

The serialization examples were moved into:

```text
Endpoints/SerializationEndpoints.cs
```

This keeps `Program.cs` focused on application configuration.

At the top of the file:

```csharp
using BasicApiEndpoints.Models;
using System.Text.Json;
using System.Xml.Serialization;

namespace BasicApiEndpoints.Endpoints;
```

The endpoints are grouped inside an extension method:

```csharp
public static class SerializationEndpoints
{
    public static void MapSerializationEndpoints(
        this WebApplication app)
    {
        // Serialization examples
    }
}
```

Then `Program.cs` only needs:

```csharp
using BasicApiEndpoints.Endpoints;
```

and:

```csharp
app.MapSerializationEndpoints();
```

The relationship becomes:

```text
Program.cs
    ↓
MapSerializationEndpoints()
    ↓
SerializationEndpoints.cs
    ↓
SerializationSample
```

---

# Sample Data

Instead of keeping a shared `samplePerson` object in `Program.cs`, the serialization endpoint file creates the sample data.

```csharp
private static SerializationSample CreateSamplePerson()
{
    return new SerializationSample
    {
        UserName = "Alice",
        UserAge = 30
    };
}
```

Each request can receive a fresh sample object.

```text
Request 1 → new SerializationSample
Request 2 → new SerializationSample
Request 3 → new SerializationSample
```

This keeps the demo data with the demo endpoints rather than placing it in application startup code.

---

# 1. Manual JSON Serialization

Manual serialization explicitly converts the C# object into JSON.

```csharp
app.MapGet("/serialization/manual-json", () =>
{
    var samplePerson = CreateSamplePerson();

    var jsonString =
        JsonSerializer.Serialize(samplePerson);

    return TypedResults.Text(
        jsonString,
        "application/json");
});
```

The important operation is:

```csharp
JsonSerializer.Serialize(samplePerson);
```

Flow:

```text
SerializationSample object
        ↓
JsonSerializer.Serialize()
        ↓
JSON string
        ↓
TypedResults.Text()
        ↓
HTTP Response
```

The content type is explicitly set to:

```text
application/json
```

This tells the client that the returned text contains JSON.

---

# 2. Custom JSON Serialization

Serialization behavior can be customized using:

```csharp
JsonSerializerOptions
```

Example:

```csharp
app.MapGet("/serialization/custom-json", () =>
{
    var samplePerson = CreateSamplePerson();

    var options = new JsonSerializerOptions
    {
        PropertyNamingPolicy =
            JsonNamingPolicy.SnakeCaseLower
    };

    var jsonString =
        JsonSerializer.Serialize(
            samplePerson,
            options);

    return TypedResults.Text(
        jsonString,
        "application/json");
});
```

The C# model uses PascalCase:

```text
UserName
UserAge
```

The custom serializer changes those properties to snake_case:

```json
{
  "user_name": "Alice",
  "user_age": 30
}
```

This demonstrates that serialization can change how data is represented externally without changing the C# model.

```text
Internal C# Model
      ↓
UserName
UserAge

Serialization Policy
      ↓
snake_case

External JSON
      ↓
user_name
user_age
```

---

# 3. `TypedResults.Json`

ASP.NET Core can handle the serialization and HTTP content type automatically.

```csharp
app.MapGet("/serialization/json", () =>
{
    var samplePerson = CreateSamplePerson();

    return TypedResults.Json(samplePerson);
});
```

This removes the need to manually call:

```csharp
JsonSerializer.Serialize(...)
```

or manually specify:

```text
application/json
```

Flow:

```text
C# Object
    ↓
TypedResults.Json()
    ↓
ASP.NET Core serialization
    ↓
JSON HTTP Response
```

---

# 4. Automatic JSON Serialization

ASP.NET Core can simplify this even further.

```csharp
app.MapGet("/serialization/auto", () =>
{
    return CreateSamplePerson();
});
```

The endpoint simply returns a C# object.

ASP.NET Core automatically converts it into JSON.

```text
return C# object
       ↓
ASP.NET Core
       ↓
JSON serialization
       ↓
HTTP Response
```

This is usually the preferred approach when no special serialization behavior is required.

It also explains something that was already happening in the existing controllers.

For example:

```csharp
return Ok(blog);
```

takes a C# `Blog` object and eventually sends JSON to the client.

Serialization was already happening automatically.

The lab simply exposed what ASP.NET Core was doing underneath.

---

# 5. XML Serialization

XML requires more explicit setup.

```csharp
app.MapGet("/serialization/xml", () =>
{
    var samplePerson = CreateSamplePerson();

    var serializer =
        new XmlSerializer(
            typeof(SerializationSample));

    using var stringWriter =
        new StringWriter();

    serializer.Serialize(
        stringWriter,
        samplePerson);

    var xmlOutput =
        stringWriter.ToString();

    return TypedResults.Text(
        xmlOutput,
        "application/xml");
});
```

Flow:

```text
SerializationSample
        ↓
XmlSerializer
        ↓
StringWriter
        ↓
XML string
        ↓
HTTP Response
```

The API returned:

```xml
<?xml version="1.0" encoding="utf-16"?>

<SerializationSample
    xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
    xmlns:xsd="http://www.w3.org/2001/XMLSchema">

  <UserName>Alice</UserName>
  <UserAge>30</UserAge>

</SerializationSample>
```

The additional XML namespaces are normal serializer metadata.

The response used:

```text
Content-Type: application/xml
```

---

# JSON vs XML

The JSON example can be as simple as:

```csharp
return samplePerson;
```

XML requires considerably more setup:

```text
Create XmlSerializer
        ↓
Create StringWriter
        ↓
Serialize object
        ↓
Convert writer to string
        ↓
Return XML response
```

For normal modern web APIs, JSON is usually significantly easier to work with.

---

# Required Namespaces

For the current JSON examples:

```csharp
using System.Text.Json;
```

provides:

```text
JsonSerializer
JsonSerializerOptions
JsonNamingPolicy
```

`System.Text.Json.Serialization` is not currently required.

That namespace becomes useful for serialization-specific attributes and converters such as:

```csharp
[JsonPropertyName("user_name")]
```

or:

```csharp
[JsonIgnore]
```

Mental model:

```text
System.Text.Json
    ↓
Serializer
Options
Naming policies

System.Text.Json.Serialization
    ↓
Attributes
Converters
Serialization metadata
```

---

# Minimal API vs Controller

These serialization examples are implemented as Minimal API endpoints:

```csharp
app.MapGet(...)
```

They are not controller actions.

The distinction in this project is:

```text
Controllers/
    ↓
Structured application APIs

Endpoints/
    ↓
Course demonstrations / Minimal API examples
```

This keeps learning examples separate from the Blog and Product controller APIs.

---

# Testing with `requests.http`

The serialization routes were added to `requests.http`.

```http
### Serialization: manual JSON

GET http://localhost:5144/serialization/manual-json

###

### Serialization: custom JSON with snake_case

GET http://localhost:5144/serialization/custom-json

###

### Serialization: built-in TypedResults.Json

GET http://localhost:5144/serialization/json

###

### Serialization: automatic JSON

GET http://localhost:5144/serialization/auto

###

### Serialization: XML

GET http://localhost:5144/serialization/xml
```

This makes it easy to compare how the same C# object is returned using different serialization techniques.

---

# Endpoint Registration

The serialization routes are registered once from `Program.cs`:

```csharp
app.MapSerializationEndpoints();
```

It is important to register the endpoint group only once.

During the exercise, the method was accidentally called twice:

```csharp
app.MapSerializationEndpoints();

...

app.MapSerializationEndpoints();
```

This caused:

```text
AmbiguousMatchException
```

because ASP.NET Core registered identical routes twice.

For example:

```text
GET /serialization/manual-json
GET /serialization/manual-json
```

When a request arrived, ASP.NET Core could not determine which endpoint should handle it.

The correct setup is:

```text
Program.cs
    ↓
ONE MapSerializationEndpoints()
    ↓
SerializationEndpoints.cs
    ↓
ONE definition of each route
```

---

# Useful Troubleshooting

To find duplicate routes or endpoint registration calls:

```bash
grep -Rni \
  --exclude-dir=bin \
  --exclude-dir=obj \
  -E "MapSerializationEndpoints|serialization/manual-json|serialization/custom-json" .
```

The expected result should contain:

```text
Program.cs
    → one MapSerializationEndpoints() call

SerializationEndpoints.cs
    → one MapSerializationEndpoints() definition
    → one manual-json route
    → one custom-json route
```

---

# Serialization Progression

The exercise demonstrates increasing levels of abstraction.

## Manual

```csharp
JsonSerializer.Serialize(samplePerson);
```

You control the serialization explicitly.

```text
Object
 ↓
JsonSerializer
 ↓
JSON String
 ↓
HTTP
```

---

## Custom

```csharp
JsonSerializer.Serialize(
    samplePerson,
    options);
```

You control serialization behavior.

```text
Object
 ↓
Custom Options
 ↓
JsonSerializer
 ↓
Customized JSON
```

---

## ASP.NET Helper

```csharp
TypedResults.Json(samplePerson);
```

ASP.NET Core handles the normal serialization work.

```text
Object
 ↓
TypedResults.Json()
 ↓
JSON
```

---

## Automatic

```csharp
return samplePerson;
```

ASP.NET Core handles everything automatically.

```text
Object
 ↓
ASP.NET Core
 ↓
JSON Response
```

---

# Key Takeaways

- Serialization converts an object into a transferable format.
- Deserialization converts transferred data back into an object.
- JSON is the normal data format for modern ASP.NET Core APIs.
- `System.Text.Json` provides JSON serialization functionality.
- `JsonSerializer.Serialize()` performs explicit serialization.
- `JsonSerializerOptions` can customize serialization behavior.
- `TypedResults.Json()` handles JSON serialization and the HTTP content type.
- ASP.NET Core can automatically serialize returned objects.
- XML serialization requires more explicit setup.
- Models define data; they do not need to perform serialization themselves.
- A custom serialization service is unnecessary unless the application develops reusable serialization or export logic.
- Course demonstration endpoints can be separated from `Program.cs` using extension methods.
- Endpoint groups must only be registered once.
- Existing controller responses were already using automatic serialization behind the scenes.

---

# Mental Model

```text
C# Object
    ↓
Serialization
    ↓
JSON / XML
    ↓
HTTP
    ↓
Client
```

Most of the time in ASP.NET Core:

```csharp
return myObject;
```

is enough.

The framework handles the plumbing underneath.