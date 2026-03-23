# NTIS Platform

Enterprise-grade .NET 10 server-side API application built with clean architecture principles.

## 📋 Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Projects](#projects)
- [Getting Started](#getting-started)
- [Configuration](#configuration)
- [Running the Application](#running-the-application)
- [Database Migrations](#database-migrations)
- [API Testing](#api-testing)
- [Running Tests](#running-tests)
- [Deployment](#deployment)
- [Authentication](#authentication)
- [Contributing](#contributing)

## 🎯 Overview

NTIS Platform is a comprehensive enterprise application template that demonstrates best practices in modern .NET development. It features:

- Clean Architecture with clear separation of concerns
- RESTful API with Swagger/OpenAPI documentation
- Entity Framework Core with SQL Server
- Repository and Unit of Work patterns
- Background worker service (Windows Service ready)
- Comprehensive unit testing with xUnit
- Authentication placeholders (ready for JWT implementation)
- CORS support
- Dependency injection throughout

## 🏗️ Architecture

The solution follows Clean Architecture principles with the following layers:

```
┌─────────────────────────────────────────────────┐
│                                                 │
│  Presentation Layer                             │
│  ├─ API (Controllers, Program.cs)               │
│  └─ Worker (Background Service)                 │
│                                                 │
├─────────────────────────────────────────────────┤
│                                                 │
│  Application Layer                              │
│  ├─ Services (Business Logic)                   │
│  ├─ DTOs (Data Transfer Objects)                │
│  └─ Interfaces                                  │
│                                                 │
├─────────────────────────────────────────────────┤
│                                                 │
│  Infrastructure Layer                           │
│  ├─ DbContext (EF Core)                         │
│  ├─ Repositories (Data Access)                  │
│  └─ Unit of Work                                │
│                                                 │
├─────────────────────────────────────────────────┤
│                                                 │
│  Domain/Core Layer                              │
│  ├─ Entities (Domain Models)                    │
│  └─ Interfaces (Abstractions)                   │
│                                                 │
└─────────────────────────────────────────────────┘
```

## 📦 Projects

### `NtisPlatform.Core`
- Domain entities and interfaces
- Business rules and domain logic
- No dependencies on other projects

### `NtisPlatform.Application`
- Application services and business logic
- DTOs for data transfer
- Service interfaces
- Depends on: Core

### `NtisPlatform.Infrastructure`
- EF Core DbContext and configurations
- Repository implementations
- Data access layer
- Depends on: Core, Application

### `NtisPlatform.Api`
- RESTful API endpoints
- Controllers
- Swagger/OpenAPI configuration
- Depends on: Application, Infrastructure

### `NtisPlatform.Worker`
- Background service for long-running tasks
- Can be deployed as Windows Service
- Depends on: Application, Infrastructure

### `NtisPlatform.Tests`
- Unit tests using xUnit
- Mocking with Moq
- Tests for all layers

## 🚀 Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server or SQL Server LocalDB
- Visual Studio 2022, VS Code, or JetBrains Rider

### Installation

1. Clone the repository:
```powershell
git clone <repository-url>
cd ntis-platform
```

2. Restore dependencies:
```powershell
dotnet restore
```

3. Build the solution:
```powershell
dotnet build
```

4. Update the connection string in `appsettings.json` files:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=NtisPlatformDb;Trusted_Connection=True;MultipleActiveResultSets=true"
}
```

## ⚙️ Configuration

### API Configuration (`NtisPlatform.Api/appsettings.json`)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Your SQL Server connection string"
  },
  "Jwt": {
    "Key": "YourSecretKey",
    "Issuer": "NtisPlatform",
    "Audience": "NtisPlatformUsers",
    "ExpiresInMinutes": 60
  }
}
```

### Worker Configuration (`NtisPlatform.Worker/appsettings.json`)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Your SQL Server connection string"
  }
}
```

## 🏃 Running the Application

### Run the API

```powershell
cd src/NtisPlatform.Api
dotnet run
```

The API will be available at:
- HTTPS: `https://localhost:7000`
- HTTP: `http://localhost:5000`
- Swagger UI: `https://localhost:7000/swagger`

### Run the Worker Service

```powershell
cd src/NtisPlatform.Worker
dotnet run
```

### Run Multiple Projects

To run both API and Worker simultaneously, use:
```powershell
# In terminal 1
dotnet run --project src/NtisPlatform.Api/NtisPlatform.Api.csproj

# In terminal 2
dotnet run --project src/NtisPlatform.Worker/NtisPlatform.Worker.csproj
```

## 🗄️ Database Migrations

### Create a migration

```powershell
cd src/NtisPlatform.Infrastructure
dotnet ef migrations add InitialCreate --startup-project ../NtisPlatform.Api/NtisPlatform.Api.csproj
```

### Update the database

```powershell
cd src/NtisPlatform.Infrastructure
dotnet ef database update --startup-project ../NtisPlatform.Api/NtisPlatform.Api.csproj
```

### Remove the last migration

```powershell
cd src/NtisPlatform.Infrastructure
dotnet ef migrations remove --startup-project ../NtisPlatform.Api/NtisPlatform.Api.csproj
```

## 🧪 API Testing

The project includes REST Client test files for easy API testing directly in VS Code.

### Setup

1. Install the **REST Client** extension in VS Code:
   - Extension ID: `humao.rest-client`
   - Or search "REST Client" by Huachao Mao in VS Code Extensions

2. Navigate to the `api-tests/` folder for all test files

### Available Test Files

- **auth.http** - Authentication endpoints (login, refresh, logout, validate session)
- **organization.http** - Organization management endpoints
- **README.md** - Detailed usage guide and tips

### Quick Start

1. Open any `.http` file (e.g., `api-tests/auth.http`)
2. Click **Send Request** above any endpoint
3. Or press `Ctrl+Alt+R` (Windows) or `Cmd+Alt+R` (Mac)
4. View response in split panel

### Example: Test Login Flow

```http
### Login
# @name login
POST http://localhost:5000/api/Auth/login
Content-Type: application/json

{
  "username": "admin",
  "password": "Admin@123",
  "authProvider": "Basic",
  "clientType": "Web",
  "device": {
    "deviceName": "REST Client",
    "ipAddress": "127.0.0.1",
    "userAgent": "VSCode REST Client"
  }
}

### Extract token from response
@accessToken = {{login.response.body.accessToken}}

### Use token in authenticated request
GET http://localhost:5000/api/Organization
Authorization: Bearer {{accessToken}}
```

### Benefits

- ✅ Version controlled with your code
- ✅ No separate app required
- ✅ Auto-extract tokens from responses
- ✅ Share with team via Git
- ✅ Works offline
- ✅ Plain text format

See `api-tests/README.md` for detailed documentation.

## 🧪 Running Tests

### Run all tests

```powershell
dotnet test
```

### Run tests with coverage

```powershell
dotnet test /p:CollectCoverage=true
```

### Run specific test project

```powershell
dotnet test tests/NtisPlatform.Tests/NtisPlatform.Tests.csproj
```

## 📦 Deployment

### Deploy as Windows Service

1. Publish the Worker project:
```powershell
dotnet publish src/NtisPlatform.Worker/NtisPlatform.Worker.csproj -c Release -o ./publish/worker
```

2. Create the Windows Service:
```powershell
sc create "NTIS Platform Worker" binPath="C:\path\to\publish\worker\NtisPlatform.Worker.exe"
```

3. Start the service:
```powershell
sc start "NTIS Platform Worker"
```

### Deploy API to IIS

1. Publish the API:
```powershell
dotnet publish src/NtisPlatform.Api/NtisPlatform.Api.csproj -c Release -o ./publish/api
```

2. Configure IIS with the published folder
3. Ensure the Application Pool is set to "No Managed Code"

### Deploy to Azure

```powershell
# Using Azure CLI
az webapp up --name ntis-platform-api --resource-group myResourceGroup
```

## 🔐 Authentication

The application includes placeholders for JWT Bearer authentication. To enable it:

1. Uncomment the authentication configuration in `Program.cs`:
```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Configuration here
    });
```

2. Uncomment the middleware in `Program.cs`:
```csharp
app.UseAuthentication();
app.UseAuthorization();
```

3. Uncomment the `[Authorize]` attributes on controllers

4. Implement login/registration endpoints to issue JWT tokens

## 🛠️ Development

### Project Structure

```
ntis-platform/
├── src/
│   ├── NtisPlatform.Core/
│   │   ├── Entities/
│   │   └── Interfaces/
│   ├── NtisPlatform.Application/
│   │   ├── DTOs/
│   │   ├── Interfaces/
│   │   └── Services/
│   ├── NtisPlatform.Infrastructure/
│   │   ├── Data/
│   │   └── Repositories/
│   ├── NtisPlatform.Api/
│   │   └── Controllers/
│   └── NtisPlatform.Worker/
├── tests/
│   └── NtisPlatform.Tests/
│       ├── Application/
│       └── Core/
└── NtisPlatform.sln
```

### Coding Standards

- Follow C# naming conventions
- Use async/await for I/O operations
- Implement proper error handling and logging
- Write unit tests for business logic
- Use dependency injection
- Keep methods small and focused
- Add XML documentation comments for public APIs

## 📝 API Endpoints

### Sample Endpoints

- `GET /api/sample` - Get all samples
- `GET /api/sample/{id}` - Get sample by ID
- `POST /api/sample` - Create new sample
- `PUT /api/sample/{id}` - Update sample
- `DELETE /api/sample/{id}` - Delete sample

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License.

## 🙏 Acknowledgments

- Clean Architecture by Robert C. Martin
- Microsoft .NET Documentation
- Entity Framework Core Documentation

---

**Built with ❤️ using .NET 10**
