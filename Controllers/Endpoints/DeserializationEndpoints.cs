using BasicApiEndpoints.Models;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace BasicApiEndpoints.Endpoints;

public static class DeserializationEndpoints
{
    public static void MapDeserializationEndpoints(
        this WebApplication app)
    {
    // Demonstrate deserializing JSON from the request body automatically.
    app.MapPost(
        "/deserialization/auto",
        (DeserializationSample personFromClient) =>
        {
            return TypedResults.Ok(personFromClient);
        });

    // Demonstrate deserializing JSON from the request body manually. 
    app.MapPost(
        "/deserialization/json",
        async (HttpContext context) =>
        {
            var person =
                await context.Request
                    .ReadFromJsonAsync<DeserializationSample>();

            return TypedResults.Ok(person);
        });

    // Demonstrate deserializing JSON from the request body manually with custom options.
    app.MapPost(
        "/deserialization/custom-options",
        async (HttpContext context) =>
        {
            var options = new JsonSerializerOptions
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

    // Demonstrate deserializing XML from the request body manually.
   app.MapPost(
        "/deserialization/xml",
        async (HttpContext context) =>
        {
            using var reader =
                new StreamReader(context.Request.Body);

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

    app.MapGet("/deserialization/binary", () =>
        {
            var filePath = "person.dat";

            if (!File.Exists(filePath))
            {
                return Results.NotFound(new
                {
                    message = "person.dat was not found. Run the binary serialization endpoint first."
                });
            }

            using var stream = new FileStream(
                filePath,
                FileMode.Open);

            using var reader = new BinaryReader(stream);

            var person = new DeserializationSample
            {
                UserName = reader.ReadString(),
                UserAge = reader.ReadInt32()
            };

            return Results.Ok(person);
});

    }   
}