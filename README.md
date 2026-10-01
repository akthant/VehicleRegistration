# Vehicle Registration Web Application & API

Vehicle registration application built with an ASP.NET Core Web API, SQL database, and React/TypeScript frontend. Vehicle categories are calculated from weight ranges. Category ranges can be changed as a complete configuration so the application can validate that no gaps between the categories or assigned to overlapping categories.

## Contents

1. [Features](#features)
2. [Requirements](#requirements)
3. [Setup](#setup)
4. [Database and migrations](#database-and-migrations)
5. [Run the application](#run-the-application)
6. [Build and test](#build-and-test)
7. [API overview](#api-overview)
8. [Testing overview](#testing-overview)
9. [Architecture and domain rules](#architecture-and-domain-rules)
10. [Known limitations and potential improvements](#known-limitations-and-potential-improvements)

## Features

- Create, read, update, and delete vehicle records, including owner, manufacturer, manufacture year, and weight.
- Calculate a vehicle's category from its weight when vehicle details are returned. The vehicle table does not store category as a foreign key.
- Configure category ranges with validation for gaps, overlaps, duplicate names, and invalid boundaries.
- Replace the complete category configuration in one request. The service validates the new configuration before saving it in a database transaction, so a relational database either saves the complete update or rolls it back.
- Delete a category by extending an adjacent category over its range (`absorbInto=next` or `absorbInto=previous`).
- Set category icons using local image paths. In development, the Vite server provides a local upload route that saves images under the frontend's `public/icons` directory.
- Validate API inputs and return JSON success/error envelopes for responses with a body. `204 No Content` responses, such as vehicle deletion, have no response body.

## Requirements

- .NET 10 SDK. All .NET projects target `net10.0`.
- SQL Server 2019 or later, SQL Server Express, or LocalDB for running the API.
- Node.js `^20.19.0` or `>=22.12.0` and npm. These versions satisfy the Vite 8.3.1 engine requirement.
- **Optional:** the `dotnet-ef` command-line tool to run migrations manually. The API project includes the EF Core design-time packages.

Check installed versions:

```sh
dotnet --version
node --version
npm --version
```

## Setup

### 1. Get the source

```sh
git clone <repository-url>
cd VehicleRegistration
```

Run the remaining commands from the repository root unless a step says otherwise.

### 2. Configure the database connection

The API requires a connection string named `DefaultConnection`. The checked-in `appsettings.json` intentionally leaves it empty. For local development, configure it with User Secrets:

```sh
dotnet user-secrets set --project VehicleRegistration.API "ConnectionStrings:DefaultConnection" "Server=localhost;Database=VehicleRegistrationDB;Trusted_Connection=True;TrustServerCertificate=True;"
```

Change the server value to match your SQL Server instance. For example, a Windows LocalDB connection can use `Server=(localdb)\MSSQLLocalDB;Database=VehicleRegistrationDB;Trusted_Connection=True;TrustServerCertificate=True;`.

For deployment or CI, provide the setting through the environment instead of committing credentials. PowerShell:

```powershell
$env:ConnectionStrings__DefaultConnection = "Server=(localdb)\MSSQLLocalDB;Database=VehicleRegistrationDB;Trusted_Connection=True;TrustServerCertificate=True;"
```

Bash:

```sh
export ConnectionStrings__DefaultConnection='Server=(localdb)\MSSQLLocalDB;Database=VehicleRegistrationDB;Trusted_Connection=True;TrustServerCertificate=True;'
```

**In Visual Studio:**
1. Right-click `VehicleManagement.API` project
2. Select "Manage User Secrets"
3. Add your connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=VehicleManagement_Dev;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

The API checks this setting at startup and stops with an error if it is missing.

### 3. Frontend API URL and CORS

The checked-in `VehicleRegistration.UI/vehicle-registration-ui/.env.development` points the frontend directly to `https://localhost:7287/api`. The API's Development CORS configuration permits `http://localhost:5173` and `http://localhost:3000`.

If you change the API URL or frontend port, update `VITE_API_URL` and the API's `Cors:AllowedOrigins` configuration to match. The Vite config also has a `/api` proxy fallback targeting `https://localhost:7001`; the checked-in environment variable overrides that fallback during development. If you remove or change that environment variable, update the proxy target to the API's actual URL.

### 4. Folder structure

**Frontend:**
```
VehicleRegistration.UI/
└── vehicle-registration-ui/
    ├── public/
    │   └── icons/
    ├── src/
    │   ├── api/
    │   ├── assets/
    │   ├── components/
    │   ├── hooks/
    │   ├── pages/
    │   ├── types/
    │   ├── validation/
    │   ├── App.tsx
    │   ├── App.css
    │   ├── index.css
    │   └── main.tsx
    ├── .env.development
    ├── package.json
    ├── package-lock.json
    ├── tsconfig.json
    └── vite.config.ts
```

**Backend:**
```
VehicleRegistration/
├── VehicleRegistration.slnx
├── VehicleRegistration.API/
│   ├── Controllers/
│   ├── Filters/
│   ├── Middleware/
│   ├── Properties/
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   ├── Program.cs
│   └── VehicleRegistration.API.csproj
├── src/
│   ├── VehicleRegistration.Application/
│   │   ├── DTOs/
│   │   ├── Exceptions/
│   │   ├── Services/
│   │   ├── Validators/
│   │   └── VehicleRegistration.Application.csproj
│   ├── VehicleRegistration.Core/
│   │   ├── Entities/
│   │   ├── Rules/
│   │   └── VehicleRegistration.Core.csproj
│   └── VehicleRegistration.Infrastructure/
│       ├── Data/
│       │   └── Migrations/
│       ├── Migrations/
│       └── VehicleRegistration.Infrastructure.csproj
└── tests/
    ├── VehicleRegistration.IntegrationTests/
    │   ├── ApiIntegrationTests.cs
    │   ├── CategoryServiceTests.cs
    │   ├── TestDbContextFactory.cs
    │   └── VehicleServiceTests.cs
    └── VehicleRegistration.UnitTests/
        ├── CategoryRangeRulesTests.cs
        └── VehicleValidatorTests.cs
```

## Database and migrations

### Database design

| Table | Purpose |
|-------|---------|
| `Vehicles` | Core records — FK to `Owners` and `Manufacturers`, stores weight and year |
| `Owners` | Simple name table; unique index on `Name` |
| `Manufacturers` | Master data (seeded); unique index on `Name` |
| `VehicleCategories` | Category configuration — `Name`, `MinWeight`, `MaxWeight` (nullable = unbounded), `IconUrl` |

#### Owners

| Column | SQL Type | Nullable | Constraints |
|--------|----------|----------|-------------|
| `Id` | `INT` | No | **PK**, identity |
| `Name` | `NVARCHAR(300)` | No | **Unique** |

#### Manufacturers

| Column | SQL Type | Nullable | Constraints |
|--------|----------|----------|-------------|
| `Id` | `INT` | No | **PK**, identity |
| `Name` | `NVARCHAR(300)` | No | **Unique** |

#### VehicleCategories

| Column | SQL Type | Nullable | Constraints |
|--------|----------|----------|-------------|
| `Id` | `INT` | No | **PK**, identity |
| `Name` | `NVARCHAR(100)` | No | **Unique** |
| `MinWeight` | `DECIMAL(10,2)` | No | `CHECK (MinWeight >= 0)`, indexed |
| `MaxWeight` | `DECIMAL(10,2)` | **Yes** | `CHECK (MaxWeight IS NULL OR MaxWeight > MinWeight)` — `NULL` = no upper limit |
| `IconUrl` | `NVARCHAR(500)` | No | — |

#### Vehicles

| Column | SQL Type | Nullable | Constraints |
|--------|----------|----------|-------------|
| `Id` | `INT` | No | **PK**, identity |
| `OwnerId` | `INT` | No | **FK → Owners(Id)**, `ON DELETE RESTRICT`, indexed |
| `ManufacturerId` | `INT` | No | **FK → Manufacturers(Id)**, `ON DELETE RESTRICT`, indexed |
| `YearOfManufacture` | `INT` | No | Validated: 1886 – current year + 1 |
| `Weight` | `DECIMAL(10,2)` | No | Validated: ≥ 0, indexed |

> There is deliberately **no FK from `Vehicles` to `VehicleCategories`**. Category is computed at runtime from `Weight` — see [Category calculation approach](#category-calculation-approach).

**Key decisions:**

- **Category is not stored on `Vehicle`.** It is computed at query time by matching the vehicle's weight against the current `VehicleCategories` rows. This means category changes are reflected instantly — no backfill migration needed.

- **Category icons live in `VehicleCategories` as a URL string.** This is intentional: the URL can point to anything (static asset, CDN, data URI) without any schema change.

- **`MaxWeight` is nullable.** A `NULL` value means "no upper limit" — there is exactly one such row, the heaviest category. This is simpler and more readable than a sentinel value like `9999999`.

- **Boundaries are inclusive, with a 0.01 kg step between consecutive categories.** Light ends at 500.00 kg; Medium starts at 500.01 kg. This avoids any floating-point ambiguity at boundary values.

- **No foreign key from `Vehicle` to `VehicleCategory`.** Because category is computed at runtime, there is nothing to reference. This is how "instant reflection" works without triggers or cascades.


EF Core migrations are in `src/VehicleRegistration.Infrastructure`. When the API runs in the `Development` environment, it automatically applies pending migrations at startup, provided the configured database is relational and reachable. **Automatic migration is only enabled in Development**.

To apply migrations manually, install the EF Core command-line tool if needed, then run the command from the repository root:

```sh
dotnet tool install --global dotnet-ef --version 10.0.12
dotnet ef database update --project src/VehicleRegistration.Infrastructure --startup-project VehicleRegistration.API
```

If `dotnet-ef` is already installed, update it to a compatible 10.0 version rather than installing it again.

The database model seeds these lookup records when migrations are applied:

- Manufacturers: `Mazda`, `Mercedes`, `Honda`, `Ferrari`, and `Toyota`.
- Categories:
  - `Light`: 0.00 kg through 500.00 kg (`/icons/light.svg`)
  - `Medium`: 500.01 kg through 2500.00 kg (`/icons/medium.svg`)
  - `Heavy`: 2500.01 kg and above (`/icons/heavy.svg`)

Owners are not seeded. Create an owner through `POST /api/owners` before registering a vehicle.

## Run the application

### API

From the repository root:

```sh
dotnet run --project VehicleRegistration.API --launch-profile https
```

**In Visual Studio:**
1. Set `VehicleManagement.API` as Startup Project
2. Press F5 or click "Run"

The HTTPS profile listens on `https://localhost:7287` and `http://localhost:5147`. In Development, the OpenAPI document is available at `https://localhost:7287/openapi/v1.json`, and the Scalar API reference is at `https://localhost:7287/scalar/v1`. If HTTPS requests fail because of the local development certificate, trust the ASP.NET Core development certificate for your OS.

### Frontend

In a second terminal, from the repository root:

```sh
cd VehicleRegistration.UI/vehicle-registration-ui
npm ci
npm run dev
```

Vite normally serves the UI at `http://localhost:5173`. Use that URL in a browser. Keep the API running in the first terminal.

## Build and test

Run the .NET build and test suites from the repository root:

```sh
dotnet build VehicleRegistration.slnx
dotnet test VehicleRegistration.slnx --verbosity normal
```

Or run the test projects separately:

```sh
dotnet test tests/VehicleRegistration.UnitTests
dotnet test tests/VehicleRegistration.IntegrationTests
```

### Run all tests in Visual Studio

1. **Test** menu → **Run All Tests** (Ctrl+R, A)
2. View results in the **Test Explorer** panel

The API validates `DefaultConnection` during startup. Configure it before running integration tests, even though those tests replace SQL Server with EF Core InMemory for their test database.

There is currently no frontend test script. The .NET tests use xUnit and FluentAssertions. Integration tests use EF Core InMemory; it does not enforce SQL Server foreign-key or check constraints, and it ignores database transactions. Therefore, those tests verify application behavior but do not verify transaction rollback or SQL Server constraint behavior.

## API overview

JSON success responses have this general shape:

```json
{
  "success": true,
  "data": {},
  "message": "Operation completed successfully."
}
```

JSON error responses have this general shape:

```json
{
  "success": false,
  "message": "One or more validation errors occurred.",
  "errors": {
    "Weight": ["Weight must be a non-negative value."]
  }
}
```

Some successful operations return `204 No Content` and therefore have no JSON body.

| Method | Endpoint | Description |
| --- | --- | --- |
| `GET` | `/api/vehicles` | List vehicles. Supports `sortBy=owner`, `ownerName`, `manufacturer`, `year`, or `weight`; add `desc=true` for descending order. |
| `GET` | `/api/vehicles/{id}` | Get one vehicle, including its calculated category and icon URL. |
| `POST` | `/api/vehicles` | Create a vehicle. Owner and manufacturer IDs must exist. |
| `PUT` | `/api/vehicles/{id}` | Replace the vehicle's owner, manufacturer, year, and weight. |
| `DELETE` | `/api/vehicles/{id}` | Delete a vehicle; returns `204 No Content` on success. |
| `GET` | `/api/categories` | List categories ordered by minimum weight. |
| `GET` | `/api/categories/{id}` | Get one category. |
| `POST` | `/api/categories` | Create a category by taking a contiguous lower or upper portion of an existing range. |
| `PUT` | `/api/categories` | Replace the complete category configuration. All ranges are validated together before saving. |
| `PUT` | `/api/categories/{id}` | Update a category's name and icon URL. Range changes belong in the complete configuration request. |
| `DELETE` | `/api/categories/{id}?absorbInto=next` or `?absorbInto=previous` | Delete a category and extend the selected adjacent category over its range. The last category cannot be deleted. |
| `GET` | `/api/owners` | List owners. |
| `GET` | `/api/owners/{id}` | Get one owner. |
| `POST` | `/api/owners` | Register an owner. |
| `GET` | `/api/manufacturers` | List manufacturers. |
| `POST` | `/api/manufacturers` | Add a manufacturer. |

The local icon upload route is provided by the Vite development server, not the Web API: `POST /__local/category-icons`. It accepts PNG, JPEG, or WebP image bytes up to 2 MB and returns an icon URL. Uploaded files are local development assets and are not automatically deployed or stored in SQL Server.

## Testing overview

| Test Type | Framework / Storage | Coverage Area | Key Verification Focus |
| :--- | :--- | :--- | :--- |
| **Unit Tests** | xUnit, Moq | `CategoryRangeRules`, FluentValidation | Gap/overlap detection, boundary steps, precision limits, DTO rules |
| **Integration Tests** | xUnit, EF Core InMemory | `CategoryService`, `VehicleService`, Controllers | Atomic transactions, absorb-on-delete, runtime category calculation, HTTP responses |

*Note: Tests execute against an EF Core In-Memory database, enabling fast execution without requiring a live SQL Server instance.*

## Design notes and domain rules

### Solution design

The backend follows a classic **Clean Architecture** split across four projects:
- `VehicleRegistration.Core`: domain entities and category range rules.
- `VehicleRegistration.Application`: DTOs, validators, service interfaces, and application services.
- `VehicleRegistration.Infrastructure`: EF Core `VehicleDbContext`, SQL Server provider, and migrations.
- `VehicleRegistration.API`: controllers, startup configuration, exception handling, and OpenAPI/Scalar setup.

This business logic is testable in isolation and means the database can be swapped without touching service code.
- `tests/VehicleRegistration.UnitTests`: domain and validator tests.
- `tests/VehicleRegistration.IntegrationTests`: service and HTTP API tests.

The frontend is a separate **React + TypeScript** SPA scaffolded with Vite. It talks to the API through TanStack Query hooks and validates all inputs with Zod before any network request.
- `VehicleRegistration.UI/vehicle-registration-ui`: React, TypeScript, and Vite frontend.

The code is organized into these layers, but the dependency direction is not a strict Clean Architecture boundary: the Application project references Infrastructure and its services use `VehicleDbContext` directly.

### Important assumptions & rules

- Category weights are stored with two decimal places and must be non-negative.
- The lightest category starts at `0.00 kg`.
- Ranges include both endpoints. For adjacent categories, the next minimum is the previous maximum plus `0.01 kg`; for example, 500.00 kg belongs to `Light` and 500.01 kg belongs to `Medium`.
- Exactly one category has no maximum weight, and it must be the heaviest range.
- Category names must be unique. The database also enforces unique category names.
- Vehicle years are validated from 1886 through the current year plus one.
- Vehicle weights must be non-negative and have no more than two decimal places.

Vehicle categories are resolved from the current category configuration when vehicles are read. Updating category ranges changes the displayed category for existing vehicles without updating vehicle rows.

### Significant design decisions

**Why replaces the entire category set rather than updating individual rows**

An update per category approach would require the client to send changes in the right order and would expose a window where the database is in an invalid state. Replace-all is simpler, safer, and matches how an admin thinks: "here is my new configuration" rather than "apply this diff".

**Why no stored procedures**

The gap/overlap validation logic is in C# (`CategoryRangeRules`). Moving it to T-SQL would duplicate it (or remove it from the unit tests). EF Core's `BeginTransactionAsync` provides the same atomicity guarantee without the maintenance overhead.

**Why the sort field allowlist is enforced in the controller, not the service**

The service accepts an arbitrary string for `sortBy` and falls back to ID order for unknown values. This keeps the service testable without a controller.


## Known limitations and potential improvements

- There is no authentication or authorization. Do not expose the API publicly without adding access controls.
- Vehicle lists are not paginated, which may become inefficient for large data sets.
- Manufacturer edit and delete endpoints are not currently provided. The vehicle-to-manufacturer foreign key is configured to restrict deleting a manufacturer that is in use.
- Category icon uploads are local to the Vite development server. Production deployments need a hosted upload/storage solution and a corresponding API or storage integration.
- Integration tests use EF Core InMemory and do not cover SQL Server-specific constraints or real database transaction behavior. Add SQL Server-backed tests if those behaviors need verification.
- Consider optimistic concurrency for category updates so simultaneous administrators do not overwrite each other's changes.
- The application only requires owner names to be unique. In practice, owners should be created separately and linked to a verified identity (e.g., driver licence). This would prevent duplicate vehicle registrations under different owner names and make it easy to see how many vehicles each owner has.
- Images are currently saved in the React app’s `public/icons` folder. In a real application, they should be uploaded to a file server or Azure Blob Storage. An image preview should also be shown during upload.
- Pagination on vehicles and filter by owner, manufacturer, year range to be provided in future releases.
- To track who changed the category configuration and when.
- To check race-condition or concurrent changes before updating vehicle category configuration to prevent users from overwriting each other’s changes and maintain data integrity.