# Serialization Security

This exercise adds basic security concepts around serialization and deserialization in ASP.NET Core.

The goal is to demonstrate that successfully deserializing data does **not** automatically mean the data is safe or appropriate to serialize back to a client.

The application now:

- accepts user data from an HTTP request
- automatically deserializes JSON into a C# object
- validates required values
- prevents invalid data from continuing
- maps sensitive input into a safer response model
- avoids returning the password in the serialized response

---

# The Security Problem

Consider an incoming model containing:

```csharp
public class UserRegistrationRequest
{
    public required string Name { get; set; }

    public required string Email { get; set; }

    public required string Password { get; set; }
}
```

A client could send:

```json
{
  "name": "Alice",
  "email": "alice@example.com",
  "password": "SecureP@ss123"
}
```

ASP.NET Core can deserialize this into a C# object automatically.

```text
JSON Request
    ↓
ASP.NET Core Deserialization
    ↓
UserRegistrationRequest
```

However, returning the same object directly would also serialize the password:

```text
UserRegistrationRequest
    ↓
Serialization
    ↓
Password exposed in response
```

That is undesirable.

---

# Separate Input and Output Models

Instead of returning the incoming model, the application uses two different data contracts.

## Incoming Model

`UserRegistrationRequest` represents data received from the client.

```csharp
namespace BasicApiEndpoints.Models;

public class UserRegistrationRequest
{
    public required string Name { get; set; }

    public required string Email { get; set; }

    public required string Password { get; set; }
}
```

This model contains the password because the application needs to receive it.

---

## Response Model

`UserResponse` represents data that is safe to return.

```csharp
namespace BasicApiEndpoints.Models;

public class UserResponse
{
    public required string Name { get; set; }

    public required string Email { get; set; }
}
```

Notice that it does **not** contain:

```text
Password
```

The separation becomes:

```text
UserRegistrationRequest
├── Name
├── Email
└── Password
        ↓
     Validate
        ↓
UserResponse
├── Name
└── Email
```

This prevents sensitive data from being serialized into the response.

---

# Validation Before Serialization

The endpoint checks incoming values before creating the response.

```csharp
if (string.IsNullOrWhiteSpace(user.Name))
{
    return Results.BadRequest(new
    {
        message = "Name is required."
    });
}
```

The same validation is applied to:

```text
Name
Email
Password
```

`IsNullOrWhiteSpace()` rejects:

```text
null
""
"   "
tabs
new lines
```

This is more appropriate for user-entered text than checking only:

```csharp
string.IsNullOrEmpty(...)
```

---

# Security Endpoint

The endpoint is implemented as a Minimal API route.

```csharp
app.MapPost(
    "/security/serialize-user",
    (UserRegistrationRequest user) =>
    {
        if (string.IsNullOrWhiteSpace(user.Name))
        {
            return Results.BadRequest(new
            {
                message = "Name is required."
            });
        }

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            return Results.BadRequest(new
            {
                message = "Email is required."
            });
        }

        if (string.IsNullOrWhiteSpace(user.Password))
        {
            return Results.BadRequest(new
            {
                message = "Password is required."
            });
        }

        var safeUser = new UserResponse
        {
            Name = user.Name.Trim(),
            Email = user.Email.Trim()
        };

        return Results.Ok(safeUser);
    });
```

---

# Request Flow

```text
HTTP POST
    ↓
JSON Request Body
    ↓
ASP.NET Core Deserialization
    ↓
UserRegistrationRequest
    ↓
Input Validation
    ↓
Create UserResponse
    ↓
ASP.NET Core Serialization
    ↓
Safe JSON Response
```

The password enters the application but does not leave through the API response.

---

# Testing with `requests.http`

```http
### Serialization security: validated user

POST http://localhost:5144/security/serialize-user
Content-Type: application/json

{
  "name": "Alice",
  "email": "alice@example.com",
  "password": "SecureP@ss123"
}
```

Expected response:

```json
{
  "name": "Alice",
  "email": "alice@example.com"
}
```

The password is intentionally excluded.

---

# Invalid Input Example

Request:

```http
POST http://localhost:5144/security/serialize-user
Content-Type: application/json

{
  "name": "",
  "email": "alice@example.com",
  "password": "SecureP@ss123"
}
```

The endpoint returns:

```text
400 Bad Request
```

with a response such as:

```json
{
  "message": "Name is required."
}
```

This is an expected client-input problem, so it should return a `4xx` response rather than throwing an exception and becoming a `500 Internal Server Error`.

---

# Deserialization Is Not Validation

These are separate operations.

## Deserialization

```text
Can this JSON become the expected C# object?
```

Example:

```json
{
  "name": "",
  "email": "alice@example.com",
  "password": "SecureP@ss123"
}
```

This can deserialize successfully because all three values are strings.

---

## Validation

```text
Are the values actually acceptable?
```

The empty name:

```json
"name": ""
```

is technically a string, but it is not valid application data.

Therefore:

```text
JSON
    ↓
Deserialize
    ↓
Validate
    ↓
Accept or Reject
```

---

# Why Not Return the Original Object?

This would be unsafe:

```csharp
return Results.Ok(user);
```

because the response could contain:

```json
{
  "name": "Alice",
  "email": "alice@example.com",
  "password": "SecureP@ss123"
}
```

Instead:

```csharp
var safeUser = new UserResponse
{
    Name = user.Name,
    Email = user.Email
};

return Results.Ok(safeUser);
```

The application deliberately controls what data crosses the API boundary.

---

# DTO / API Contract Concept

This exercise also demonstrates why separate request and response models are useful.

```text
External Client
      ↓
UserRegistrationRequest
      ↓
Application
      ↓
UserResponse
      ↓
External Client
```

The application does not need to expose every property it receives or stores.

This provides control over:

- sensitive fields
- internal implementation details
- API contracts
- validation
- future model changes

---

# Base64 Is Not Encryption

A common teaching example converts a password using:

```csharp
Convert.ToBase64String(...)
```

This does **not** encrypt the password.

Base64 is only an encoding format.

```text
Password
    ↓
Base64 encode
    ↓
Different-looking text
    ↓
Base64 decode
    ↓
Original password
```

Anyone who receives the Base64 value can reverse it.

Therefore, Base64 should not be treated as password protection.

---

# Password Handling in a Real Application

A real registration flow would typically look more like:

```text
Client
    ↓
HTTPS
    ↓
POST registration request
    ↓
Deserialize
    ↓
Validate
    ↓
Password hashing
    ↓
Store password hash
    ↓
Return safe response DTO
```

The raw password should generally not be:

- logged
- returned from the API
- stored in plain text
- written into serialized files
- placed in normal application logs

Password hashing is a separate topic and is not implemented in this lab.

---

# Trusted vs Untrusted Input

External input should generally be treated as untrusted.

Avoid assuming that a Boolean such as:

```csharp
isTrustedSource = true
```

makes incoming data trustworthy.

A better model is:

```text
External Input
    ↓
Untrusted
    ↓
Deserialize
    ↓
Validate structure
    ↓
Validate values
    ↓
Authenticate / authorize if needed
    ↓
Process
```

Trust should come from real controls such as authentication, authorization, certificates, signatures, or secure communication channels.

---

# What Has Been Implemented

```text
✓ JSON deserialization
✓ Required input model
✓ Input validation
✓ IsNullOrWhiteSpace checks
✓ 400 responses for invalid input
✓ Separate request and response models
✓ Password excluded from response serialization
✓ Safe response DTO
```

Not yet implemented:

```text
○ Password hashing
○ Authentication
○ Authorization
○ Database storage
○ Cryptographic encryption
○ HMAC integrity verification
○ Digital signatures
```

---

# Key Takeaways

- Deserialization does not automatically validate business rules.
- Validate input after deserialization.
- Use `IsNullOrWhiteSpace()` for required user-entered text.
- Do not automatically serialize sensitive input back to clients.
- Separate request and response models when their responsibilities differ.
- Treat external data as untrusted.
- Return `400 Bad Request` for normal invalid client input.
- Do not use Base64 as encryption.
- Password protection requires proper password hashing, not reversible encoding.
- `System.Text.Json` is appropriate for normal JSON serialization and deserialization.
- Security should be applied around the serialization process, not assumed to come from serialization itself.

---

# Mental Model

```text
Client Input
    ↓
Deserialize
    ↓
Validate
    ↓
Process Sensitive Data
    ↓
Map to Safe Response
    ↓
Serialize
    ↓
Client
```

The core principle is:

```text
Accept only valid data.
Expose only the data that should leave the application.
```