# Basic API Endpoints

A small ASP.NET Core Web API learning project demonstrating Minimal API routing, controller-based REST APIs, dependency injection, and service abstractions.

## Requirements

- .NET 10 SDK

## Run

From the project directory:

```sh
dotnet run
```

The app prints its local address when it starts. Sample requests for the Minimal API examples and controllers are in [`requests.http`](requests.http); run them with the VS Code REST Client extension or another HTTP client.

## Blog API

The blog controller uses the `/api/blogs` route prefix.

| Method | Route | Description |
| --- | --- | --- |
| `GET` | `/api/blogs` | Returns all blogs. |
| `GET` | `/api/blogs/{id}` | Returns one blog, or `404 Not Found`. |
| `POST` | `/api/blogs` | Creates a blog and returns `201 Created`; the server assigns its ID. |
| `PUT` | `/api/blogs/{id}` | Updates a blog, or returns `404 Not Found`. |
| `DELETE` | `/api/blogs/{id}` | Deletes a blog and returns `204 No Content`, or `404 Not Found`. |

## Product API

The product controller uses the `/api/products` route prefix.

| Method | Route | Description |
| --- | --- | --- |
| `GET` | `/api/products` | Returns all products. |
| `GET` | `/api/products/{id}` | Returns one product, or `404 Not Found`. |
| `POST` | `/api/products` | Creates a product and returns `201 Created`. |
| `PUT` | `/api/products/{id}` | Updates a product, or returns `404 Not Found`. |
| `DELETE` | `/api/products/{id}` | Deletes a product and returns `204 No Content`, or `404 Not Found`. |

## Minimal API examples

The remaining Minimal API mappings in `Program.cs` demonstrate route constraints, route and query parameters, optional segments, catch-all parameters, and basic HTTP verbs.

## Data storage

Both `BlogService` and `ProductService` currently use in-memory lists. Data created or changed at runtime is available while the app is running, but resets when it stops or restarts.

## License

This project is licensed under the MIT License. See [`LICENSE`](LICENSE).
