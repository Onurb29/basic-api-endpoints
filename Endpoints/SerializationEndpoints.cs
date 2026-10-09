using BasicApiEndpoints.Models;
using System.Text.Json;
using System.Xml.Serialization;

namespace BasicApiEndpoints.Endpoints;

public static class SerializationEndpoints
{
    public static void MapSerializationEndpoints(
        this WebApplication app)
    {
        // Serialize to JSON manually, then return the JSON text.
        app.MapGet("/serialization/manual-json", () =>
        {
            var samplePerson = CreateSamplePerson();

            var jsonString =
                JsonSerializer.Serialize(samplePerson);

            return TypedResults.Text(
                jsonString,
                "application/json");
        });

        // Demonstrate customizing JSON property names to snake_case.
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

        // Use ASP.NET Core's typed JSON result.
        app.MapGet("/serialization/json", () =>
        {
            var samplePerson = CreateSamplePerson();

            return TypedResults.Json(samplePerson);
        });

        // ASP.NET Core automatically serializes the returned object as JSON.
        app.MapGet("/serialization/auto", () =>
        {
            return CreateSamplePerson();
        });

        // Serialize the sample object as XML instead of JSON.
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

        // Write the values to person.dat in the app's current working directory.
        // Read them back in the same order: UserName first, then UserAge.
        app.MapPost("/serialization/binary", () =>
        {
            var samplePerson = CreateSamplePerson();

            var filePath = "person.dat";

            using var stream = new FileStream(
                filePath,
                FileMode.Create);

            using var writer = new BinaryWriter(stream);

            writer.Write(samplePerson.UserName);
            writer.Write(samplePerson.UserAge);

            return Results.Ok(
                new
                {
                    message = "Binary serialization complete.",
                    file = filePath
                });
        });
    }

    private static SerializationSample CreateSamplePerson()
    {
        return new SerializationSample
        {
            UserName = "Alice",
            UserAge = 30
        };
    }
}