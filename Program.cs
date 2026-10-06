var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Root Path");
app.MapGet("/downloads", () => "Downloads");
app.MapPut("/" , () => "This is a PUT request to the root path");
app.MapDelete("/", () => "Delete request to the root path");
app.MapPost("/", () => "POST request to the root path");
app.Run();
