using BasicApiEndpoints.Models;
using System.Text.Json;
using System.Xml.Serialization;

namespace BasicApiEndpoints.Endpoints;

public static class SerializationEndpoints
{
    public static void MapSerializationEndpoints(
        this WebApplication app)
    {
        app.MapGet("/serialization/manual-json", () =>
        {
            var samplePerson = CreateSamplePerson();

            var jsonString =
                JsonSerializer.Serialize(samplePerson);

            return TypedResults.Text(
                jsonString,
                "application/json");
        });

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

        app.MapGet("/serialization/json", () =>
        {
            var samplePerson = CreateSamplePerson();

            return TypedResults.Json(samplePerson);
        });

        app.MapGet("/serialization/auto", () =>
        {
            return CreateSamplePerson();
        });

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