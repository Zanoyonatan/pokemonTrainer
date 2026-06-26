# Step 01 Instructions — Backend Foundation, Authentication, JWT and Pokémon Data Model

## Purpose

Implement the first backend foundation phase for the PokéTrainer AI project.

This step must create a clean ASP.NET Core Web API foundation, configure SQL Server LocalDB with Entity Framework Core, add ASP.NET Core Identity, implement JWT-based authentication, and define the initial Pokémon data model.

Work incrementally. Do not skip validation points. After each checkpoint, stop and wait for approval before continuing.

---

## Target Result

At the end of this step, the backend should include:

- ASP.NET Core Web API running on .NET 10 LTS.
- Controllers-based API.
- Swagger enabled in Development only.
- A working `HealthController`.
- SQL Server LocalDB connection.
- Entity Framework Core configured.
- `ApplicationDbContext`.
- ASP.NET Core Identity configured.
- JWT Bearer Authentication configured.
- Register endpoint.
- Login endpoint that returns a JWT.
- Protected `me` endpoint.
- Pokémon relational model.
- Many-to-many relationship between Pokémon and Pokémon Types.
- EF Core migrations applied successfully.

---

## Expected Project Structure

Use the following structure inside the backend project:

```text
pokemonTrainer/
│
├── Controllers/
│   ├── AuthController.cs
│   └── HealthController.cs
│
├── Data/
│   └── ApplicationDbContext.cs
│
├── DTOs/
│   └── Auth/
│       ├── AuthResponse.cs
│       ├── LoginRequest.cs
│       ├── RegisterRequest.cs
│       └── UserInfoResponse.cs
│
├── Infrastructure/
│
├── Models/
│   ├── ApplicationUser.cs
│   ├── Pokemon.cs
│   ├── PokemonType.cs
│   └── PokemonPokemonType.cs
│
├── Services/
│   └── JwtTokenService.cs
│
├── Migrations/
│
├── appsettings.json
├── appsettings.Development.json
└── Program.cs
```

---

# Phase 1 — Create the ASP.NET Core Web API Project

## Goal

Create the backend project foundation.

## Instructions

Create an ASP.NET Core Web API project with the following settings:

```text
Framework: .NET 10 LTS
Authentication: None
Configure for HTTPS: Enabled
Enable OpenAPI support: Enabled
Use Controllers: Enabled
Enable Container Support: Disabled
Aspire orchestration: Disabled
```

Use a clear project name such as:

```text
pokemonTrainer
```

or:

```text
PokemonTrainerAI.Api
```

Do not use the combined Angular + ASP.NET Core template at this stage.

The Angular application should be created separately later.

## Cleanup

Delete the default template files:

```text
WeatherForecast.cs
Controllers/WeatherForecastController.cs
```

## Create Folders

Create these folders:

```text
Controllers
Data
DTOs
Infrastructure
Models
Services
```

## Validation

Run the API and confirm that it starts successfully.

Expected console output should show that the application is listening on HTTP/HTTPS ports.

Stop here and wait for approval.

---

# Phase 2 — Configure Swagger for Development

## Goal

Enable Swagger for local development only.

## Program.cs Requirements

Use Swagger in Development only:

```csharp
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
```

Do not keep both `AddOpenApi()` and `AddSwaggerGen()` unless there is a specific reason.

For this project, prefer the classic Swagger setup:

```csharp
builder.Services.AddSwaggerGen();
app.UseSwagger();
app.UseSwaggerUI();
```

## Validation

Run the project and open:

```text
/swagger
```

Confirm that Swagger loads successfully.

Stop here and wait for approval.

---

# Phase 3 — Add HealthController

## Goal

Create a simple endpoint that confirms the API is running.

## File Location

```text
Controllers/HealthController.cs
```

## Code

```csharp
using Microsoft.AspNetCore.Mvc;

namespace pokemonTrainer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            Status = "Healthy",
            Service = "Pokemon Trainer API",
            Timestamp = DateTime.UtcNow
        });
    }
}
```

## Explanation

The route:

```csharp
[Route("api/[controller]")]
```

maps `HealthController` to:

```http
/api/health
```

The action:

```csharp
[HttpGet]
```

maps the method to an HTTP GET request.

The line:

```csharp
app.MapControllers();
```

must exist in `Program.cs`; otherwise controller routes will not be mapped.

## Validation

Run the project and test:

```http
GET /api/health
```

Expected result:

```json
{
  "status": "Healthy",
  "service": "Pokemon Trainer API",
  "timestamp": "..."
}
```

Stop here and wait for approval.

---

# Phase 4 — Add Entity Framework Core and SQL Server LocalDB

## Goal

Configure EF Core with SQL Server LocalDB.

## Required NuGet Packages

Install:

```text
Microsoft.EntityFrameworkCore.SqlServer
Microsoft.EntityFrameworkCore.Tools
Microsoft.EntityFrameworkCore.Design
```

## Create ApplicationDbContext

File location:

```text
Data/ApplicationDbContext.cs
```

Initial code:

```csharp
using Microsoft.EntityFrameworkCore;

namespace pokemonTrainer.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
}
```

## appsettings.json

Add the connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=PokemonTrainerDb;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

Make sure the JSON file has one root object only.

## Register DbContext in Program.cs

Add:

```csharp
using Microsoft.EntityFrameworkCore;
using pokemonTrainer.Data;
```

Register the context:

```csharp
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));
```

## Validation

Build the solution.

Expected result:

```text
Build succeeded.
```

Stop here and wait for approval.

---

# Phase 5 — Create the Initial Migration

## Goal

Verify EF Core migrations and LocalDB connectivity.

## Commands

Run in Package Manager Console:

```powershell
Add-Migration InitialCreate
```

Then:

```powershell
Update-Database
```

## Expected Result

The database should be created:

```text
PokemonTrainerDb
```

At this stage it may contain only:

```text
__EFMigrationsHistory
```

This is valid.

## Validation

Confirm that `Update-Database` ends with:

```text
Done.
```

Stop here and wait for approval.

---

# Phase 6 — Add ASP.NET Core Identity

## Goal

Add user management with ASP.NET Core Identity.

## Required NuGet Package

Install:

```text
Microsoft.AspNetCore.Identity.EntityFrameworkCore
```

## Create ApplicationUser

File location:

```text
Models/ApplicationUser.cs
```

Code:

```csharp
using Microsoft.AspNetCore.Identity;

namespace pokemonTrainer.Models;

public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginAt { get; set; }
}
```

## Update ApplicationDbContext

Replace the initial `DbContext` inheritance with `IdentityDbContext<ApplicationUser>`.

File:

```text
Data/ApplicationDbContext.cs
```

Code:

```csharp
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using pokemonTrainer.Models;

namespace pokemonTrainer.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
}
```

## Register Identity in Program.cs

Add:

```csharp
using pokemonTrainer.Models;
```

Register Identity:

```csharp
builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;

        options.Password.RequiredLength = 6;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>();
```

Use `AddIdentityCore` because this is a Web API project and does not need the built-in Razor UI.

## Validation

Build the solution.

Stop here and wait for approval.

---

# Phase 7 — Add Identity Migration

## Goal

Create Identity tables in SQL Server.

## Commands

Run:

```powershell
Add-Migration AddIdentityTables
```

Then:

```powershell
Update-Database
```

## Expected Tables

The database should contain:

```text
AspNetUsers
AspNetRoles
AspNetUserRoles
AspNetUserClaims
AspNetUserLogins
AspNetUserTokens
AspNetRoleClaims
__EFMigrationsHistory
```

## Validation

Confirm that `Update-Database` ends with:

```text
Done.
```

Confirm that `AspNetUsers` exists in the database.

Stop here and wait for approval.

---

# Phase 8 — Add JWT Settings

## Goal

Add JWT configuration.

## appsettings.json

Add:

```json
"Jwt": {
  "Key": "THIS_IS_A_DEMO_SECRET_KEY_FOR_POKEMON_TRAINER_AI_123456",
  "Issuer": "PokemonTrainerAI",
  "Audience": "PokemonTrainerAIUsers",
  "ExpiresInMinutes": 120
}
```

The full file should remain valid JSON.

Example structure:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=PokemonTrainerDb;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "Jwt": {
    "Key": "THIS_IS_A_DEMO_SECRET_KEY_FOR_POKEMON_TRAINER_AI_123456",
    "Issuer": "PokemonTrainerAI",
    "Audience": "PokemonTrainerAIUsers",
    "ExpiresInMinutes": 120
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

## Security Note

This is acceptable for local development.

Before production, move the JWT key to:

- User Secrets
- Environment variables
- A secure secret store

Stop here and wait for approval.

---

# Phase 9 — Add Authentication DTOs

## Goal

Create request and response models for authentication.

## Create Folder

```text
DTOs/Auth
```

## RegisterRequest.cs

File:

```text
DTOs/Auth/RegisterRequest.cs
```

Code:

```csharp
using System.ComponentModel.DataAnnotations;

namespace pokemonTrainer.DTOs.Auth;

public class RegisterRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string DisplayName { get; set; } = string.Empty;
}
```

## LoginRequest.cs

File:

```text
DTOs/Auth/LoginRequest.cs
```

Code:

```csharp
using System.ComponentModel.DataAnnotations;

namespace pokemonTrainer.DTOs.Auth;

public class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
```

## UserInfoResponse.cs

File:

```text
DTOs/Auth/UserInfoResponse.cs
```

Code:

```csharp
namespace pokemonTrainer.DTOs.Auth;

public class UserInfoResponse
{
    public string Id { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;
}
```

## AuthResponse.cs

File:

```text
DTOs/Auth/AuthResponse.cs
```

Code:

```csharp
namespace pokemonTrainer.DTOs.Auth;

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public UserInfoResponse User { get; set; } = new();
}
```

## Validation

Build the solution.

Stop here and wait for approval.

---

# Phase 10 — Add JwtTokenService

## Goal

Create a dedicated service responsible for generating JWT tokens.

## File Location

```text
Services/JwtTokenService.cs
```

## Code

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using pokemonTrainer.Models;

namespace pokemonTrainer.Services;

public class JwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public (string Token, DateTime ExpiresAt) CreateToken(ApplicationUser user)
    {
        var jwtSection = _configuration.GetSection("Jwt");

        var key = jwtSection["Key"];
        var issuer = jwtSection["Issuer"];
        var audience = jwtSection["Audience"];
        var expiresInMinutes = int.Parse(jwtSection["ExpiresInMinutes"] ?? "120");

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException("JWT key is missing.");
        }

        var expiresAt = DateTime.UtcNow.AddMinutes(expiresInMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Name, user.DisplayName),
            new("displayName", user.DisplayName)
        };

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
```

## Explanation

This service separates JWT creation from the controller.

The JWT should contain only safe identity claims.

JWT is signed, not encrypted. The client can read the claims, but cannot modify them without invalidating the signature.

## Register Service

In `Program.cs`:

```csharp
builder.Services.AddScoped<JwtTokenService>();
```

## Validation

Build the solution.

Stop here and wait for approval.

---

# Phase 11 — Configure JWT Bearer Authentication

## Goal

Configure the backend to validate JWT tokens sent by clients.

## Required NuGet Package

Install:

```text
Microsoft.AspNetCore.Authentication.JwtBearer
```

## Program.cs Usings

Add:

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
```

## Add Authentication Configuration

Add after Identity registration:

```csharp
var jwtSection = builder.Configuration.GetSection("Jwt");

var jwtKey = jwtSection["Key"]
             ?? throw new InvalidOperationException("JWT Key is missing.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],

            IssuerSigningKey =
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtKey))
        };
    });
```

## Configure Middleware Order

Use this order:

```csharp
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
```

`UseAuthentication()` must come before `UseAuthorization()`.

## Validation

Build and run the API.

Stop here and wait for approval.

---

# Phase 12 — Add AuthController

## Goal

Add endpoints for registration, login, and current user info.

## File Location

```text
Controllers/AuthController.cs
```

## Code

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using pokemonTrainer.DTOs.Auth;
using pokemonTrainer.Models;
using pokemonTrainer.Services;
using System.Security.Claims;

namespace pokemonTrainer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtTokenService _jwtTokenService;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        JwtTokenService jwtTokenService)
    {
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var existingUser =
            await _userManager.FindByEmailAsync(request.Email);

        if (existingUser != null)
        {
            return BadRequest("Email already exists.");
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName
        };

        var result =
            await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok();
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user =
            await _userManager.FindByEmailAsync(request.Email);

        if (user == null)
        {
            return Unauthorized("Invalid email or password.");
        }

        var passwordValid =
            await _userManager.CheckPasswordAsync(user, request.Password);

        if (!passwordValid)
        {
            return Unauthorized("Invalid email or password.");
        }

        user.LastLoginAt = DateTime.UtcNow;

        var tokenResult =
            _jwtTokenService.CreateToken(user);

        return Ok(new AuthResponse
        {
            Token = tokenResult.Token,
            ExpiresAt = tokenResult.ExpiresAt,
            User = new UserInfoResponse
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                DisplayName = user.DisplayName
            }
        });
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        return Ok(new
        {
            UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
            Email = User.FindFirstValue(ClaimTypes.Email),
            Name = User.FindFirstValue(ClaimTypes.Name)
        });
    }
}
```

## Note

`LastLoginAt` is assigned in the current implementation. To persist it, add this after assigning it:

```csharp
await _userManager.UpdateAsync(user);
```

If this is added, confirm that it does not introduce unwanted side effects.

## Validation

Run the API and test:

### Register

```http
POST /api/auth/register
```

Request:

```json
{
  "email": "trainer@test.com",
  "password": "Test123",
  "displayName": "Ash Trainer"
}
```

Expected response:

```http
200 OK
```

### Login

```http
POST /api/auth/login
```

Request:

```json
{
  "email": "trainer@test.com",
  "password": "Test123"
}
```

Expected response:

```json
{
  "token": "...",
  "expiresAt": "...",
  "user": {
    "id": "...",
    "email": "trainer@test.com",
    "displayName": "Ash Trainer"
  }
}
```

Stop here and wait for approval.

---

# Phase 13 — Add Pokémon Data Models

## Goal

Add the first relational Pokémon model.

Use a normalized model for Pokémon and Pokémon Types.

## Create Pokemon.cs

File:

```text
Models/Pokemon.cs
```

Code:

```csharp
namespace pokemonTrainer.Models;

public class Pokemon
{
    public int Id { get; set; }

    public int PokeApiId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    public int Height { get; set; }

    public int Weight { get; set; }

    public int? BaseExperience { get; set; }

    public string? StatsJson { get; set; }

    public string? AbilitiesJson { get; set; }

    public bool IsLegendary { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<PokemonPokemonType> PokemonTypes { get; set; } = new List<PokemonPokemonType>();
}
```

## Create PokemonType.cs

File:

```text
Models/PokemonType.cs
```

Code:

```csharp
namespace pokemonTrainer.Models;

public class PokemonType
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public ICollection<PokemonPokemonType> PokemonTypes { get; set; } = new List<PokemonPokemonType>();
}
```

## Create PokemonPokemonType.cs

File:

```text
Models/PokemonPokemonType.cs
```

Code:

```csharp
namespace pokemonTrainer.Models;

public class PokemonPokemonType
{
    public int PokemonId { get; set; }

    public Pokemon Pokemon { get; set; } = null!;

    public int PokemonTypeId { get; set; }

    public PokemonType PokemonType { get; set; } = null!;
}
```

## Explanation

Use a many-to-many relationship because one Pokémon can have multiple types and one type can belong to many Pokémon.

Examples:

```text
Pikachu -> Electric
Charizard -> Fire + Flying
Bulbasaur -> Grass + Poison
```

Stop here and wait for approval.

---

# Phase 14 — Update ApplicationDbContext for Pokémon

## Goal

Register Pokémon entities and configure their relationships.

## File

```text
Data/ApplicationDbContext.cs
```

## Updated Code

```csharp
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using pokemonTrainer.Models;

namespace pokemonTrainer.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Pokemon> Pokemons { get; set; }

    public DbSet<PokemonType> PokemonTypes { get; set; }

    public DbSet<PokemonPokemonType> PokemonPokemonTypes { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Pokemon>()
            .HasIndex(p => p.PokeApiId)
            .IsUnique();

        builder.Entity<Pokemon>()
            .HasIndex(p => p.Name);

        builder.Entity<PokemonType>()
            .HasIndex(t => t.Name)
            .IsUnique();

        builder.Entity<PokemonPokemonType>()
            .HasKey(pt => new { pt.PokemonId, pt.PokemonTypeId });

        builder.Entity<PokemonPokemonType>()
            .HasOne(pt => pt.Pokemon)
            .WithMany(p => p.PokemonTypes)
            .HasForeignKey(pt => pt.PokemonId);

        builder.Entity<PokemonPokemonType>()
            .HasOne(pt => pt.PokemonType)
            .WithMany(t => t.PokemonTypes)
            .HasForeignKey(pt => pt.PokemonTypeId);
    }
}
```

## Explanation

The indexes improve lookup and prevent duplicate imported data.

`PokeApiId` is unique to avoid importing the same Pokémon more than once.

The composite key on `PokemonPokemonType` prevents duplicate type links.

## Validation

Build the solution.

Stop here and wait for approval.

---

# Phase 15 — Add Pokémon Migration

## Goal

Create Pokémon tables in SQL Server.

## Commands

Run:

```powershell
Add-Migration AddPokemonTables
```

Then:

```powershell
Update-Database
```

## Expected Tables

```text
Pokemons
PokemonTypes
PokemonPokemonTypes
```

## Validation

Confirm that `Update-Database` ends with:

```text
Done.
```

Confirm that the tables exist in the database.

Stop here and wait for approval.

---

# Final Step Summary Format

After completing all phases in this file, provide the following summary:

```text
Step summary:
- Goal: Backend foundation, Identity authentication, JWT setup, and Pokémon data model.
- Files created:
  - Controllers/HealthController.cs
  - Controllers/AuthController.cs
  - Data/ApplicationDbContext.cs
  - DTOs/Auth/RegisterRequest.cs
  - DTOs/Auth/LoginRequest.cs
  - DTOs/Auth/AuthResponse.cs
  - DTOs/Auth/UserInfoResponse.cs
  - Models/ApplicationUser.cs
  - Models/Pokemon.cs
  - Models/PokemonType.cs
  - Models/PokemonPokemonType.cs
  - Services/JwtTokenService.cs
- Files modified:
  - Program.cs
  - appsettings.json
- Database changes:
  - Identity tables
  - Pokémon tables
- How to test:
  - GET /api/health
  - POST /api/auth/register
  - POST /api/auth/login
  - Confirm JWT is returned
  - Confirm database tables exist
- Next recommended step:
  - Implement PokeApiClient and PokemonSyncService.

Waiting for approval before continuing.
```
