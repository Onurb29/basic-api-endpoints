# Basic API Endpoints

A small ASP.NET Core Minimal API project demonstrating endpoint routing, route and query parameters, and dependency injection with an in-memory blog service.

## Requirements

- .NET 10 SDK

## Run

From the project directory:

```sh
dotnet run
```

The application prints the local address it is listening on. The sample HTTP requests are in [`requests.http`](requests.http); send them with the VS Code REST Client extension or another HTTP client.

## Blog endpoints

| Method | Route | Description |
| --- | --- | --- |
| `GET` | `/blogs` | Returns all blogs. |
| `GET` | `/blogs/{id}` | Returns a blog by ID, or `404` if it is not found. |
| `POST` | `/blogs` | Creates a blog and returns `201 Created`. The server assigns its ID. |

Other endpoints in `Program.cs` demonstrate route constraints, optional and catch-all parameters, and query-string binding.

## Data storage

Blogs are held in an in-memory list by `BlogService`. They remain available while the application is running, but newly created blogs are lost when it stops or restarts.

## License

This project is licensed under the MIT License. See [`LICENSE`](LICENSE).
