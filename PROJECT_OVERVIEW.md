# ECom - Project Overview

A microservices e-commerce backend built on ASP.NET Core (.NET 10). An Ocelot API gateway fronts five services: authentication, products/catalog, cart, and orders. A React frontend on `http://localhost:3000` is expected (it is the only origin allowed by CORS).

## Architecture

```
            React frontend (localhost:3000)
                        |
                        v
        GatewayEcom (Ocelot)  https://localhost:7145
          - validates JWT Bearer tokens
          - routes /api/* and /images/*
                        |
     +------------------+-------------------+-------------------+
     v                  v                   v                   v
  AuthAPI           ProductsAPI          CartAPI            OrdersAPI
  :7193             :7000                :7001              :7002
  Users, login,     Products, categories, Per-user cart,    Orders created
  JWT issuance,     image upload,        calls Products    from a cart
  token revocation  stock reservation    for price/stock   checkout
     |                  |                   |                   |
  SQL Server       SQL Server          SQL Server          SQL Server
  (Auth DB)        (CatalogDb)         (Cart DB)           (Orders DB)
```

Service-to-service calls:
- **CartAPI -> ProductsAPI**: fetches product price (`GET api/ProductsAPI/{id}`) and reserves stock (`POST api/ProductsAPI/{id}/reserve`).
- **OrdersAPI -> CartAPI**: reads a user's active cart and marks the cart items as checked out. It forwards the caller's `Authorization` header.

## Projects

| Folder | Role | Database / Notes |
|---|---|---|
| `GatewayEcom` | Ocelot gateway. Routing, JWT validation, CORS. Routes are defined in `ocelot.json`. | No database |
| `AuthAPI` | Registration (user and admin), login, logout (revokes the token's `jti`). Issues HS256 JWTs. Rate-limited at 10 requests/min per IP on auth endpoints. | `AuthDbContext`, tables for users and revoked tokens |
| `ProductsAPI` | Product and category CRUD, soft delete (`IsActive`), image upload to `wwwroot/images`, stock reservation. | `CatalogDbContext` (`CatalogDb` connection string) |
| `CartAPI` | Per-user cart with add, update, delete, and checkout. Prices come from ProductsAPI. | `CartDbContext` |
| `OrdersAPI` | Creates orders from the user's active cart. Lists and fetches orders, and deletes them. | `OrdersDbContext` |
| `ECom` | Empty Web SDK project (only `ECom.csproj`, no source). Likely a leftover; nothing references it. | None |

There is no `.sln` file. Each project is built separately.

## Tech Stack

- .NET 10, ASP.NET Core Web API (controllers)
- Entity Framework Core with SQL Server (`UseSqlServer`), with automatic `Migrate()` at startup when migrations are pending
- JWT Bearer authentication (`Microsoft.AspNetCore.Authentication.JwtBearer`)
- Ocelot for the gateway
- AutoMapper (ProductsAPI)
- Swagger/Swashbuckle in development builds

## Endpoints

All routes are reached through the gateway at `https://localhost:7145`.

**Auth** (`/api/AuthAPI`)
| Method | Path | Auth | Purpose |
|---|---|---|---|
| POST | `/register` | none | Register a user (rate-limited) |
| POST | `/register/admin` | setup key in body | Register an admin, validated against `Admin:SetupKey` |
| POST | `/login` | none | Returns a JWT, its expiry, user id, email and role |
| POST | `/logout` | Bearer | Revokes the current token |

**Products** (`/api/ProductsAPI`)
| Method | Path | Auth | Purpose |
|---|---|---|---|
| GET | `/` | none | List active products |
| GET | `/{id}` | none | Get one product |
| POST | `/` | Admin | Create a product |
| PUT | `/{id}` | Admin | Update a product |
| DELETE | `/{id}` | Admin | Soft-delete a product |
| POST | `/{id}/reserve` | Bearer | Decrement stock by quantity; 409 if insufficient |
| POST | `/upload` | Admin | Upload an image (max 5 MB; jpg, jpeg, png, webp, gif) |
| GET | `/images/{file}` | none | Served static images |

**Categories** (`/api/CategoryAPI`) - GET list and GET by id are public; POST, PUT and DELETE require Bearer.

**Cart** (`/api/CartAPI`), all require auth
| Method | Path | Purpose |
|---|---|---|
| GET | `/` | Current user's active cart items |
| GET | `/{id}` | One cart item owned by the user |
| GET | `/user/{userId}` | Cart for a user (403 if not the caller) |
| POST | `/` | Add to cart. Reserves stock in ProductsAPI. Merges into an existing active line for the same product |
| PUT | `/{id}` | Change product or quantity of a cart item |
| DELETE | `/{id}` | Remove a cart item |
| POST | `/user/{userId}/checkout` | Deactivate the given cart item ids (403 if not the caller) |

**Orders** (`/api/OrdersAPI`), all require auth at the gateway
| Method | Path | Purpose |
|---|---|---|
| POST | `/` | Checkout: reads the user's cart, clears it, creates the order |
| GET | `/user/{userId}` | List a user's orders |
| GET | `/{id}` | Get one order |
| DELETE | `/{id}` | Delete an order |

## Authentication and Authorization

- Tokens are HS256 JWTs with claims `sub`, `jti`, `email`, and `role`. Lifetime is `Jwt:ExpiryMinutes` (default 60).
- Logout stores the token's `jti` in a revoked-tokens table. Every service that validates tokens checks this list through AuthAPI's database. In practice only AuthAPI checks revocation, so other services do not see the revocation.
- Roles: `User` (default) and `Admin`. Admins are created via `/register/admin` with the setup key.
- Passwords are hashed with ASP.NET Core's `PasswordHasher`. Login does a dummy verify on unknown emails to reduce timing differences.

## Configuration

Settings are read from `appsettings.json` and environment-specific overrides. Do not commit real values.

| Key | Used by | Notes |
|---|---|---|
| `Jwt:Key` | all services | At least 32 bytes. Startup fails if missing or too short |
| `Jwt:Issuer` | AuthAPI, Gateway | Single issuer |
| `Jwt:Issuers` | ProductsAPI, CartAPI | Array of accepted issuers |
| `Jwt:Audience` | all services | |
| `Jwt:ExpiryMinutes` | AuthAPI | Default 60 |
| `Admin:SetupKey` | AuthAPI | Required for admin registration |
| `ConnectionStrings:DefaultConnection` | AuthAPI, CartAPI, OrdersAPI | |
| `ConnectionStrings:CatalogDb` | ProductsAPI | |
| `CartAPI:BaseUrl`, `ProductsAPI:BaseUrl` | OrdersAPI, CartAPI | Base URLs for service-to-service HttpClients |
| `ocelot.json` | Gateway | Route table and `BaseUrl` |

## Running Locally

1. Start SQL Server and set the connection strings for each service.
2. Set the JWT and admin settings in each service's configuration.
3. Run each project. Migrations apply automatically on startup:
   - AuthAPI (`https://localhost:7193`)
   - ProductsAPI (`https://localhost:7000`)
   - CartAPI (`https://localhost:7001`)
   - OrdersAPI (`https://localhost:7002`)
   - GatewayEcom (`https://localhost:7145`)
4. Use the `.http` files in each service folder, or Swagger in development, to exercise endpoints.

## Known Limitations

These come from reading the code and are worth addressing before production use:

- **Revoked tokens are only checked in AuthAPI.** Other services validate the JWT signature and expiry but do not query the revocation list, so a logged-out token still works against them until it expires.
- **Stock is reserved when an item is added to the cart,** and nothing restores it when the cart item is deleted, updated, or the order is never completed.
- **Checkout is not transactional.** CartAPI clears the cart before OrdersAPI saves the order. If the order save fails, the cart items are lost.
- **OrdersAPI does not validate authentication itself.** It has no JWT setup, so its `GET /user/{userId}`, `GET /{id}` and `DELETE /{id}` endpoints are open to anyone who can reach port 7002 directly. Through the gateway they require a Bearer token, but any authenticated user can read or delete any user's orders.
- **Two different issuer settings.** The gateway reads `Jwt:Issuer` (single), while the services read `Jwt:Issuers` (array). Keep them consistent.
- **Image uploads** are stored on local disk under `wwwroot/images`, so they will not survive a container restart or scale-out without shared storage.
- **Default CORS** only allows `localhost:3000`.
