using BasicApiEndpoints.Models;

namespace BasicApiEndpoints.Endpoints;

public static class SerializationSecurityEndpoints
{
    public static void MapSerializationSecurityEndpoints(
        this WebApplication app)
    {
        _ = app.MapPost(
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
    }
}