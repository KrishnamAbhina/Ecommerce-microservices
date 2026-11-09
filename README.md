# ECom

A microservices e-commerce backend (.NET 10) behind an Ocelot API gateway. The frontend is not in this repository.

## Services

| Project        | Port | Purpose                                              |
|----------------|------|------------------------------------------------------|
| GatewayEcom    | 7145 | Ocelot gateway; single entry point for clients       |
| AuthAPI        | 7193 | Register, login, logout (JWT, revoked-token list)    |
| ProductsAPI    | 7000 | Product and category catalog, stock, image uploads   |
| CartAPI        | 7001 | Shopping cart                                        |
| OrdersAPI      | 7002 | Order placement and history                          |

Each service has its own SQL Server database and EF Core migrations.

## Routing

Clients call the gateway at `https://localhost:7145`. Ocelot routes are in `GatewayEcom/ocelot.json`:

- `/api/AuthAPI/*` to AuthAPI
- `/api/ProductsAPI/*` to ProductsAPI (`POST /upload` requires a Bearer token)
- `/api/CartAPI/*` to CartAPI (Bearer required)
- `/api/OrdersAPI/*` to OrdersAPI (Bearer required)
- `/images/*` to ProductsAPI static files

## Authentication

- Passwords are hashed with ASP.NET Core `PasswordHasher`.
- Login returns a JWT with `Sub`, `Jti`, `Email`, and `Role` claims.
- Roles are `User` (1) and `Admin` (2).
- Admin accounts are created with `POST /api/AuthAPI/register/admin`, which requires `setupKey` to match `Admin:SetupKey` in config.
- Product create, update, and delete, and image upload, require the Admin role.

## Product images

1. Admin uploads with `POST /api/ProductsAPI/upload` (multipart field `file`, max 5 MB, `.jpg/.jpeg/.png/.webp/.gif`).
2. The response is `{ "imagePath": "/images/<guid>.<ext>" }`.
3. Store that path as `imagePath` on the product.
4. Render with the gateway prefix, e.g. `https://localhost:7145` + `imagePath`.

Files are saved to `ProductsAPI/wwwroot/images/`.

## Orders

`PaymentMode` is an enum with values `Cash = 1`, `Card = 2`, `UPI = 3`. Order responses return the name (`"Cash"`, `"Card"`, `"UPI"`). Requests still accept the number.

## Running locally

1. Start SQL Server and set each service's connection string (`ConnectionStrings:*`) and `Jwt:*` settings in `appsettings.Development.json` or user secrets.
2. Run AuthAPI, ProductsAPI, CartAPI, OrdersAPI, and GatewayEcom from Visual Studio or with `dotnet run`.
3. Open Swagger on each service in Development (e.g. `https://localhost:7193/swagger`).

Do not commit real secrets. The dev values in `appsettings.Development.json` are for local use only.
