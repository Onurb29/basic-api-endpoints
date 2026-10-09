# Deserialization in ASP.NET Core

Deserialization converts incoming data such as JSON or XML into a C# object that the application can use.

It is the reverse of serialization.

```text
Serialization

C# Object
    ↓
JSON / XML
    ↓
HTTP Response
```

```text
Deserialization

HTTP Request
    ↓
JSON / XML
    ↓
C# Object
```

In a web API, deserialization usually happens when a client sends data using an HTTP method such as:

```text
POST
PUT
PATCH
```

---

# Why Deserialization Matters

A client might send:

```json
{
  "userName": "Alice",
  "userAge": 30
}
```

ASP.NET Core can convert that JSON into:

```csharp
var person = new DeserializationSample
{
    UserName = "Alice",
    UserAge = 30
};
```

Once deserialization has completed, the application works with a normal C# object.

```text
HTTP JSON
    ↓
ASP.NET Core
    ↓
DeserializationSample
    ↓
Application Logic
```

---

# Project Structure

The deserialization lab was added to the existing `BasicApiEndpoints` project instead of creating another temporary project.

```text
BasicApiEndpoints/
├── Controllers/
├── Endpoints/
│   ├── SerializationEndpoints.cs
│   └── DeserializationEndpoints.cs
├── Models/
│   ├── SerializationSample.cs
│   └── DeserializationSample.cs
├── Service/
├── Program.cs
└── requests.http
```

This keeps the course exercises separated while continuing to build on the same API project.

---

# Deserialization Model

`Models/DeserializationSample.cs`

```csharp
namespace BasicApiEndpoints.Models;

public class DeserializationSample
{
    public required string UserName { get; set; }

    public int? UserAge { get; set; }
}
```

There are two important differences between these properties.

```text
UserName
    ↓
required

UserAge
    ↓
optional / nullable
```

The nullable integer:

```csharp
int?
```

means the value may be missing or `null`.

For example, this can still be deserialized:

```json
{
  "userName": "Alice"
}
```

because `UserAge` is optional.

---

# Deserialization Endpoints

The examples live in:

```text
Endpoints/DeserializationEndpoints.cs
```

Required namespaces:

```csharp
using BasicApiEndpoints.Models;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace BasicApiEndpoints.Endpoints;
```

The endpoint group is defined using an extension method:

```csharp
public static class DeserializationEndpoints
{
    public static void MapDeserializationEndpoints(
        this WebApplication app)
    {
        // Deserialization examples
    }
}
```

Then `Program.cs` only needs:

```csharp
app.MapDeserializationEndpoints();
```

The relationship becomes:

```text
Program.cs
    ↓
MapDeserializationEndpoints()
    ↓
DeserializationEndpoints.cs
    ↓
DeserializationSample
```

---

# 1. Automatic JSON Deserialization

The simplest approach is to let ASP.NET Core deserialize the request automatically.

```csharp
app.MapPost(
    "/deserialization/auto",
    (DeserializationSample personFromClient) =>
    {
        return TypedResults.Ok(personFromClient);
    });
```

The endpoint declares:

```csharp
DeserializationSample personFromClient
```

ASP.NET Core sees the expected type and attempts to convert the request body into that object.

```text
POST Request
    ↓
JSON Body
    ↓
ASP.NET Core
    ↓
DeserializationSample
    ↓
personFromClient
```

Once this happens:

```csharp
personFromClient.UserName
```

is just a normal C# property.

There is nothing special about the object after deserialization.

---

# Testing Automatic Deserialization

Request:

```http
POST http://localhost:5144/deserialization/auto
Content-Type: application/json

{
  "userName": "Alice",
  "userAge": 30
}
```

ASP.NET Core converts the request into:

```csharp
DeserializationSample
```

and the endpoint returns the object.

---

# 2. Explicit JSON Deserialization

Instead of letting ASP.NET Core bind the object automatically, the request body can be read explicitly.

```csharp
app.MapPost(
    "/deserialization/json",
    async (HttpContext context) =>
    {
        var person =
            await context.Request
                .ReadFromJsonAsync<DeserializationSample>();

        return TypedResults.Ok(person);
    });
```

The important operation is:

```csharp
ReadFromJsonAsync<DeserializationSample>()
```

This means:

> Read the HTTP request body as JSON and try to create a `DeserializationSample`.

Flow:

```text
HTTP Request
    ↓
Request.Body
    ↓
ReadFromJsonAsync<T>()
    ↓
JSON Deserializer
    ↓
DeserializationSample
```

---

# Automatic vs Explicit Deserialization

Automatic:

```csharp
(DeserializationSample personFromClient)
```

means:

```text
ASP.NET Core
    ↓
Handle deserialization automatically
```

Explicit:

```csharp
await context.Request
    .ReadFromJsonAsync<DeserializationSample>();
```

means:

```text
Application
    ↓
Explicitly read and deserialize request body
```

Both approaches can produce the same C# object.

The explicit approach becomes useful when more control is required.

---

# 3. Custom JSON Deserialization Options

By default, JSON may contain properties that do not exist in the target C# class.

For example:

```json
{
  "userName": "Alice",
  "userAge": 30,
  "favoriteFurnace": 64
}
```

The model does not contain:

```text
favoriteFurnace
```

With normal deserialization, extra properties are typically ignored.

Sometimes an API should reject data that does not match the expected contract.

This can be configured using:

```csharp
JsonSerializerOptions
```

Example:

```csharp
app.MapPost(
    "/deserialization/custom-options",
    async (HttpContext context) =>
    {
        var options =
            new JsonSerializerOptions
            {
                UnmappedMemberHandling =
                    JsonUnmappedMemberHandling.Disallow
            };

        var person =
            await context.Request
                .ReadFromJsonAsync<DeserializationSample>(
                    options);

        return TypedResults.Ok(person);
    });
```

The important setting is:

```csharp
JsonUnmappedMemberHandling.Disallow
```

Without it:

```text
Unknown JSON property
    ↓
Ignored
```

With it:

```text
Unknown JSON property
    ↓
Rejected
```

---

# Why `System.Text.Json.Serialization` Is Needed Here

Earlier serialization examples only required:

```csharp
using System.Text.Json;
```

The custom deserialization example also uses:

```csharp
JsonUnmappedMemberHandling
```

which requires:

```csharp
using System.Text.Json.Serialization;
```

Mental model:

```text
System.Text.Json
    ↓
JsonSerializer
JsonSerializerOptions

System.Text.Json.Serialization
    ↓
JsonUnmappedMemberHandling
Attributes
Converters
Serialization metadata
```

---

# Required vs Optional Properties

The model contains:

```csharp
public required string UserName { get; set; }

public int? UserAge { get; set; }
```

This request contains both:

```json
{
  "userName": "Alice",
  "userAge": 30
}
```

This request omits the optional age:

```json
{
  "userName": "Alice"
}
```

That is allowed because:

```csharp
UserAge
```

is nullable.

However, this request is missing the required property:

```json
{
  "userAge": 30
}
```

The payload does not satisfy the expected model because:

```csharp
UserName
```

is required.

---

# Wrong Data Types

The serializer also checks whether incoming JSON values can be converted to the expected C# types.

Valid:

```json
{
  "userName": "Alice",
  "userAge": 30
}
```

Invalid:

```json
{
  "userName": "Alice",
  "userAge": "buffalo"
}
```

The model expects:

```csharp
int? UserAge
```

but receives:

```text
string
```

Conceptually:

```text
JSON value
"buffalo"
    ↓
Expected Int32
    ↓
Cannot deserialize
```

This is one reason strongly typed models are useful.

The model itself helps enforce the shape and types of incoming data.

---

# Content Type Matters

The HTTP request should accurately describe the format of the request body.

For JSON:

```http
Content-Type: application/json
```

For XML:

```http
Content-Type: application/xml
```

Changing only the header does not transform the payload.

For example:

```http
Content-Type: application/json

<DeserializationSample>
    <UserName>Alice</UserName>
</DeserializationSample>
```

is still XML.

Calling it JSON does not make it JSON.

The deserializer expects:

```text
Content-Type
    +
Actual Payload
    ↓
Compatible format
```

---

# 4. XML Deserialization

XML deserialization requires more manual work.

```csharp
app.MapPost(
    "/deserialization/xml",
    async (HttpContext context) =>
    {
        using var reader =
            new StreamReader(
                context.Request.Body);

        var body =
            await reader.ReadToEndAsync();

        var serializer =
            new XmlSerializer(
                typeof(DeserializationSample));

        using var stringReader =
            new StringReader(body);

        var person =
            serializer.Deserialize(stringReader)
            as DeserializationSample;

        return TypedResults.Ok(person);
    });
```

The process is:

```text
HTTP Request Body
      ↓
StreamReader
      ↓
XML string
      ↓
StringReader
      ↓
XmlSerializer.Deserialize()
      ↓
DeserializationSample
```

Compared with automatic JSON:

```csharp
(DeserializationSample personFromClient)
```

XML requires considerably more plumbing.

---

# JSON vs XML Deserialization

JSON:

```text
HTTP JSON
    ↓
ASP.NET Core
    ↓
C# Object
```

often requires only:

```csharp
(DeserializationSample person)
```

XML:

```text
HTTP XML
    ↓
StreamReader
    ↓
String
    ↓
StringReader
    ↓
XmlSerializer
    ↓
C# Object
```

For normal ASP.NET Core APIs, JSON is usually the easier path.

XML remains useful when integrating with systems that require it.

---

# Binary Serialization and Deserialization Round Trip

The binary examples can be tested as a complete round trip.

First, serialize the sample object into `person.dat`:

```http
POST http://localhost:5144/serialization/binary
```

Response:

```json
{
  "message": "Binary serialization complete.",
  "file": "person.dat"
}
```

This creates the binary file:

```text
person.dat
```

The object is written using:

```csharp
writer.Write(samplePerson.UserName);
writer.Write(samplePerson.UserAge);
```

Then deserialize the same file:

```http
GET http://localhost:5144/deserialization/binary
```

Response:

```json
{
  "userName": "Alice",
  "userAge": 30
}
```

The file is read using:

```csharp
var person = new DeserializationSample
{
    UserName = reader.ReadString(),
    UserAge = reader.ReadInt32()
};
```

The read order must match the write order exactly:

```text
Serialization

UserName
    ↓
Write(String)

UserAge
    ↓
Write(Int32)


Deserialization

ReadString()
    ↓
UserName

ReadInt32()
    ↓
UserAge
```

This confirms the complete flow:

```text
C# Object
    ↓
BinaryWriter
    ↓
person.dat
    ↓
BinaryReader
    ↓
C# Object
    ↓
ASP.NET Core serializes response as JSON
```

An important detail is that the final HTTP response is JSON only because ASP.NET Core automatically serializes the deserialized C# object for the client.

The stored data itself is still binary.

---

# Testing with `requests.http`

## Automatic JSON

```http
### Deserialization: automatic JSON

POST http://localhost:5144/deserialization/auto
Content-Type: application/json

{
  "userName": "Alice",
  "userAge": 30
}
```

---

## Optional Property Missing

```http
### Deserialization: JSON without optional age

POST http://localhost:5144/deserialization/auto
Content-Type: application/json

{
  "userName": "Alice"
}
```

---

## Required Property Missing

```http
### Deserialization: missing required username

POST http://localhost:5144/deserialization/auto
Content-Type: application/json

{
  "userAge": 30
}
```

---

## Explicit JSON

```http
### Deserialization: explicit JSON

POST http://localhost:5144/deserialization/json
Content-Type: application/json

{
  "userName": "Alice",
  "userAge": 30
}
```

---

## Extra Property with Default Behavior

```http
### Deserialization: extra field

POST http://localhost:5144/deserialization/json
Content-Type: application/json

{
  "userName": "Alice",
  "userAge": 30,
  "favoriteFurnace": 64
}
```

The extra property is normally ignored.

---

## Extra Property with Strict Options

```http
### Deserialization: reject extra fields

POST http://localhost:5144/deserialization/custom-options
Content-Type: application/json

{
  "userName": "Alice",
  "userAge": 30,
  "favoriteFurnace": 64
}
```

Because:

```csharp
JsonUnmappedMemberHandling.Disallow
```

is configured, the additional field should be rejected.

---

## Wrong Data Type

```http
### Deserialization: wrong data type

POST http://localhost:5144/deserialization/json
Content-Type: application/json

{
  "userName": "Alice",
  "userAge": "buffalo"
}
```

The model expects an integer but receives a string.

---

## XML

```http
### Deserialization: XML

POST http://localhost:5144/deserialization/xml
Content-Type: application/xml

<DeserializationSample>
  <UserName>Alice</UserName>
  <UserAge>30</UserAge>
</DeserializationSample>
```

---

# Deserialization Was Already Happening in the Project

The project already used automatic deserialization before this lab.

For example, the Blog controller accepts:

```csharp
CreateBlogRequest request
```

from an HTTP request body.

A client sends:

```json
{
  "title": "Following Blog Post",
  "content": "This is the content of the new blog post."
}
```

ASP.NET Core converts that JSON into:

```csharp
CreateBlogRequest
```

Conceptually:

```text
POST /api/blogs
      ↓
JSON Request Body
      ↓
ASP.NET Core Deserialization
      ↓
CreateBlogRequest
      ↓
BlogsController
      ↓
BlogService
```

The lab does not introduce an entirely new mechanism.

It exposes what ASP.NET Core was already doing automatically.

---

# Serialization vs Deserialization

## Serialization

```text
C# Object
    ↓
Serializer
    ↓
JSON / XML / Binary
    ↓
External System
```

Example:

```csharp
JsonSerializer.Serialize(samplePerson);
```

---

## Deserialization

```text
External System
    ↓
JSON / XML
    ↓
Deserializer
    ↓
C# Object
```

Example:

```csharp
await context.Request
    .ReadFromJsonAsync<DeserializationSample>();
```

Together:

```text
Application A
    ↓
C# Object
    ↓
Serialize
    ↓
JSON
    ↓
HTTP
    ↓
Deserialize
    ↓
C# Object
    ↓
Application B
```

---

# Industrial Integration Example

The same concept applies outside normal web applications.

An external system might send:

```json
{
  "furnaceId": 64,
  "temperature": 755.4,
  "state": "POURING"
}
```

A .NET application could deserialize it into:

```csharp
public class FurnaceStatus
{
    public int FurnaceId { get; set; }

    public double Temperature { get; set; }

    public string State { get; set; }
}
```

Flow:

```text
External System
    ↓
JSON
    ↓
HTTP / MQTT / Message Broker
    ↓
Deserializer
    ↓
FurnaceStatus
    ↓
Application Logic
```

The transport format is converted into a strongly typed object that the application understands.

---

# Note About Error Handling

Malformed JSON, missing required properties, wrong data types, or strict custom deserialization options can cause deserialization exceptions.

Conceptually:

```text
Invalid Client Payload
    ↓
Deserialization Failure
```

This is normally a **client input problem**, not an unexpected server failure.

A useful HTTP distinction is:

```text
400 Bad Request
    ↓
Client supplied invalid data

500 Internal Server Error
    ↓
Unexpected server failure
```

The current project also has global exception middleware.

As the project evolves, deserialization failures should be handled so invalid client payloads return appropriate `4xx` responses rather than being treated as unexpected `500` server errors.

---

# Key Takeaways

- Deserialization converts incoming data into C# objects.
- It is the reverse of serialization.
- ASP.NET Core can deserialize JSON automatically from HTTP request bodies.
- Declaring the expected C# type is usually enough.
- `ReadFromJsonAsync<T>()` provides more explicit control.
- `JsonSerializerOptions` can customize deserialization behavior.
- `JsonUnmappedMemberHandling.Disallow` rejects unknown JSON properties.
- Required and optional C# properties help define the expected API contract.
- Wrong JSON data types cannot be converted into incompatible C# types.
- `Content-Type` should match the actual payload format.
- JSON is the normal happy path for modern ASP.NET Core APIs.
- XML deserialization requires more manual processing.
- Existing controller models such as `CreateBlogRequest` were already being deserialized automatically.
- Invalid client data should normally result in a `4xx` response rather than an unexpected `500`.

---

# Mental Model

Most of the time:

```text
Client JSON
    ↓
ASP.NET Core
    ↓
Your C# Model
```

which can be as simple as:

```csharp
app.MapPost(
    "/example",
    (MyModel model) =>
    {
        // model is already deserialized
    });
```

Use explicit deserialization only when additional control is actually required.

