# Guía de Desarrollo — Users API desde Cero

Esta guía explica cómo construir el proyecto paso a paso, decisión por decisión.
Está pensada para entender el **por qué** de cada capa, cada patrón y cada línea de código.

No cubre despliegue ni infraestructura (eso está en [GUIDE.md](GUIDE.md)).

---

## Índice

1. [Arquitectura y decisiones de diseño](#1-arquitectura-y-decisiones-de-diseño)
2. [Crear la solución y los proyectos](#2-crear-la-solución-y-los-proyectos)
3. [Capa Domain](#3-capa-domain)
4. [Capa Application](#4-capa-application)
5. [Capa Infrastructure](#5-capa-infrastructure)
6. [Capa Api](#6-capa-api)
7. [Autenticación JWT](#7-autenticación-jwt)
8. [Pruebas unitarias](#8-pruebas-unitarias)
9. [Variables de entorno y configuración](#9-variables-de-entorno-y-configuración)
10. [Docker](#10-docker)
11. [CI/CD con GitHub Actions](#11-cicd-con-github-actions)
12. [Resumen: qué hace cada archivo](#12-resumen-qué-hace-cada-archivo)

---

## 1. Arquitectura y decisiones de diseño

### Clean Architecture con 4 capas

```
src/
├── Api/              ← Controllers, Middleware, Program.cs
├── Application/      ← Servicios, DTOs, Validaciones
├── Domain/           ← Entidades, Interfaces
└── Infrastructure/   ← EF Core, Repositorios
```

**Regla de dependencias:** las capas internas no conocen a las externas.

```
Api → Application → Domain
Api → Infrastructure → Domain
Infrastructure → Application → Domain
```

`Domain` no referencia ningún otro proyecto. Es el núcleo.

### SOLID aplicado

| Principio | Cómo se aplica |
|---|---|
| **S**ingle Responsibility | Cada servicio tiene un solo caso de uso. El controller solo recibe requests y delega. |
| **O**pen/Closed | Abierto a extensión (nuevos providers de BD) sin modificar lo existente. |
| **L**iskov Substitution | `IUserRepository` permite cualquier implementación (EF Core, InMemory, Mock). |
| **I**nterface Segregation | `IUserRepository` solo expone los métodos que el dominio necesita. |
| **D**ependency Inversion | Las capas superiores dependen de `IUserRepository` (abstracción), no de `UserRepository` (concreción). |

### Por qué NO usamos MediatR, CQRS, Unit of Work

- Una sola entidad no justifica CQRS.
- Servicios directos son más simples de entender, testear y debugear.
- EF Core ya actúa como Unit of Work con `SaveChangesAsync()`.

---

## 2. Crear la solución y los proyectos

```bash
# 1. Crear solución
dotnet new sln -n UsersApi

# 2. Crear proyectos
dotnet new classlib -n UsersApi.Domain      -o src/Domain
dotnet new classlib -n UsersApi.Application  -o src/Application
dotnet new classlib -n UsersApi.Infrastructure -o src/Infrastructure
dotnet new webapi    -n UsersApi             -o src/Api

# 3. Crear proyecto de tests
dotnet new xunit     -n UsersApi.UnitTests   -o tests/UnitTests

# 4. Agregar proyectos a la solución
dotnet sln add src/Domain/UsersApi.Domain.csproj
dotnet sln add src/Application/UsersApi.Application.csproj
dotnet sln add src/Infrastructure/UsersApi.Infrastructure.csproj
dotnet sln add src/Api/UsersApi.csproj
dotnet sln add tests/UnitTests/UsersApi.UnitTests.csproj

# 5. Establecer referencias entre proyectos
dotnet add src/Application/UsersApi.Application.csproj reference src/Domain/UsersApi.Domain.csproj
dotnet add src/Infrastructure/UsersApi.Infrastructure.csproj reference src/Domain/UsersApi.Domain.csproj
dotnet add src/Infrastructure/UsersApi.Infrastructure.csproj reference src/Application/UsersApi.Application.csproj
dotnet add src/Api/UsersApi.csproj reference src/Application/UsersApi.Application.csproj
dotnet add src/Api/UsersApi.csproj reference src/Infrastructure/UsersApi.Infrastructure.csproj
dotnet add tests/UnitTests/UsersApi.UnitTests.csproj reference src/Application/UsersApi.Application.csproj
```

### Paquetes NuGet necesarios

```bash
# Application
dotnet add src/Application/UsersApi.Application.csproj package FluentValidation
dotnet add src/Application/UsersApi.Application.csproj package Microsoft.Extensions.DependencyInjection.Abstractions
dotnet add src/Application/UsersApi.Application.csproj package Microsoft.Extensions.Logging.Abstractions
dotnet add src/Application/UsersApi.Application.csproj package Microsoft.Extensions.Configuration.Abstractions
dotnet add src/Application/UsersApi.Application.csproj package Microsoft.IdentityModel.Tokens
dotnet add src/Application/UsersApi.Application.csproj package System.IdentityModel.Tokens.Jwt

# Infrastructure
dotnet add src/Infrastructure/UsersApi.Infrastructure.csproj package Microsoft.EntityFrameworkCore.SqlServer
dotnet add src/Infrastructure/UsersApi.Infrastructure.csproj package Microsoft.EntityFrameworkCore.InMemory
dotnet add src/Infrastructure/UsersApi.Infrastructure.csproj package Microsoft.EntityFrameworkCore.Tools

# Api
dotnet add src/Api/UsersApi.csproj package Microsoft.EntityFrameworkCore.Design
dotnet add src/Api/UsersApi.csproj package Swashbuckle.AspNetCore
dotnet add src/Api/UsersApi.csproj package Microsoft.AspNetCore.Authentication.JwtBearer

# Tests
dotnet add tests/UnitTests/UsersApi.UnitTests.csproj package FluentAssertions
dotnet add tests/UnitTests/UsersApi.UnitTests.csproj package Moq
dotnet add tests/UnitTests/UsersApi.UnitTests.csproj package coverlet.collector
```

### Limpiar el template de webapi

Eliminar del `src/Api/` los archivos generados por el template que no usamos:
- `WeatherForecast.cs`
- `Controllers/WeatherForecastController.cs`

---

## 3. Capa Domain

El Domain es el corazón. No depende de nada externo. Define **qué es** el negocio.

### 3.1 Entidad User

**Archivo:** `src/Domain/Entities/User.cs`

```csharp
namespace UsersApi.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }

    private User() { }

    public static User Create(string firstName, string lastName, string email)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            CreatedAt = DateTime.UtcNow
        };
    }
}
```

**Decisiones de diseño:**

| Decisión | Por qué |
|---|---|
| `private set` | Las propiedades no se modifican desde fuera. Encapsulación. |
| `private User() { }` | Constructor vacío privado. Solo EF Core lo usa para materializar objetos. El código de negocio usa `User.Create()`. |
| `= null!` | Suprime el warning CS8618. Las propiedades se inicializan en `Create()` o por EF Core. |
| `User.Create()` | Factory method. Centraliza la creación, asigna `Guid.NewGuid()` y `DateTime.UtcNow`. |
| Sin Data Annotations | La capa de dominio no debe depender de EF Core. La configuración de BD va en Infrastructure. |

### 3.2 Interfaz del repositorio

**Archivo:** `src/Domain/Interfaces/IUserRepository.cs`

```csharp
using UsersApi.Domain.Entities;

namespace UsersApi.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    Task DeleteAsync(User user, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

**Decisiones de diseño:**

| Decisión | Por qué |
|---|---|
| En Domain, no en Infrastructure | Dependency Inversion: Application depende de esta interfaz, no de la implementación. |
| `CancellationToken` | Permite cancelar operaciones de BD si el cliente cierra la conexión. |
| `SaveChangesAsync` separado | Permite operaciones transaccionales (múltiples Add/Delete antes de guardar). |

---

## 4. Capa Application

Define **cómo se ejecutan** los casos de uso. Orquesta Domain + Infrastructure.

### 4.1 DTOs

Los DTOs separan el contrato de la API del modelo de dominio.

**`src/Application/DTOs/CreateUserRequest.cs`**
```csharp
namespace UsersApi.Application.DTOs;

public record CreateUserRequest(string FirstName, string LastName, string Email);
```

**`src/Application/DTOs/UserResponse.cs`**
```csharp
using UsersApi.Domain.Entities;

namespace UsersApi.Application.DTOs;

public record UserResponse(Guid Id, string FirstName, string LastName, string Email, DateTime CreatedAt)
{
    public static UserResponse FromEntity(User user) =>
        new(user.Id, user.FirstName, user.LastName, user.Email, user.CreatedAt);
}
```

| Decisión | Por qué |
|---|---|
| `record` en vez de `class` | Inmutables por defecto, ideales para DTOs. `record` genera `ToString()`, `Equals()`, deconstruction. |
| `FromEntity()` estático | Lógica de mapeo en un solo lugar. Si la entidad cambia, solo se toca este método. |

### 4.2 Excepciones de dominio

**`src/Application/Exceptions/NotFoundException.cs`**
```csharp
namespace UsersApi.Application.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}
```

**`src/Application/Exceptions/ValidationException.cs`**
```csharp
namespace UsersApi.Application.Exceptions;

public class ValidationException : Exception
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(string message, IDictionary<string, string[]> errors)
        : base(message)
    {
        Errors = errors;
    }
}
```

| Decisión | Por qué |
|---|---|
| Excepciones propias | El middleware puede diferenciar `NotFoundException` (404) de `ValidationException` (400). |
| `Errors` como diccionario | Permite retornar errores por campo: `{ "Email": ["El email no es válido."] }`. |

### 4.3 Validación con FluentValidation

**`src/Application/Validators/CreateUserRequestValidator.cs`**
```csharp
using FluentValidation;
using UsersApi.Application.DTOs;

namespace UsersApi.Application.Validators;

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().WithMessage("El nombre es obligatorio.");
        RuleFor(x => x.LastName).NotEmpty().WithMessage("El apellido es obligatorio.");
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es obligatorio.")
            .EmailAddress().WithMessage("El formato del email no es válido.");
    }
}
```

| Decisión | Por qué |
|---|---|
| FluentValidation | Reglas declarativas, testeo unitario simple sin depender de HTTP. |
| Validación en Application | Las reglas de negocio se validan antes de llegar a Infrastructure. |

### 4.4 Servicios de caso de uso

Cada servicio es un caso de uso único (Single Responsibility).

**`src/Application/Services/CreateUserService.cs`**

```csharp
public class CreateUserService
{
    private readonly IUserRepository _repository;
    private readonly CreateUserRequestValidator _validator;
    private readonly ILogger<CreateUserService> _logger;

    public CreateUserService(IUserRepository repository,
        CreateUserRequestValidator validator, ILogger<CreateUserService> logger)
    {
        _repository = repository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<UserResponse> ExecuteAsync(CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new Exceptions.ValidationException("Error de validación.", errors);
        }

        var user = User.Create(request.FirstName, request.LastName, request.Email);
        await _repository.AddAsync(user, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Usuario creado: {UserId}", user.Id);
        return UserResponse.FromEntity(user);
    }
}
```

El patrón se repite para `GetUsersService`, `GetUserByIdService`, `DeleteUserService`.

| Decisión | Por qué |
|---|---|
| `ILogger<T>` en cada servicio | Trazabilidad granular. En producción se envía a Application Insights. |
| Validación antes de persistir | Fail fast: si los datos son inválidos, no tocamos la BD. |
| `User.Create()` factory | La lógica de creación de entidad vive en el dominio, no en el servicio. |

### 4.5 DependencyInjection de Application

**`src/Application/DependencyInjection.cs`**

```csharp
using Microsoft.Extensions.DependencyInjection;
using UsersApi.Application.Services;
using UsersApi.Application.Validators;

namespace UsersApi.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateUserService>();
        services.AddScoped<GetUsersService>();
        services.AddScoped<GetUserByIdService>();
        services.AddScoped<DeleteUserService>();
        services.AddScoped<AuthService>();
        services.AddScoped<CreateUserRequestValidator>();
        return services;
    }
}
```

| Decisión | Por qué |
|---|---|
| `AddScoped` | Una instancia por request HTTP. El ciclo de vida coincide con `DbContext`. |
| Método de extensión | `Program.cs` solo llama a `builder.Services.AddApplication()`. Limpio y modular. |

---

## 5. Capa Infrastructure

Implementa las interfaces definidas en Domain. Es la **única capa que conoce EF Core**.

### 5.1 DbContext

**`src/Infrastructure/Data/AppDbContext.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using UsersApi.Domain.Entities;

namespace UsersApi.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(u => u.LastName).IsRequired().HasMaxLength(100);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(256);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.CreatedAt).IsRequired();
        });
    }
}
```

| Decisión | Por qué |
|---|---|
| Fluent API, no Data Annotations | La entidad `User` no tiene dependencia de EF Core. |
| `HasIndex(u => u.Email).IsUnique()` | Índice único a nivel BD. Segundo nivel de defensa después de la validación. |
| `Set<User>()` en vez de field | Evita null dereference. Método recomendado por Microsoft. |

### 5.2 Implementación del repositorio

**`src/Infrastructure/Repositories/UserRepository.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using UsersApi.Domain.Entities;
using UsersApi.Domain.Interfaces;
using UsersApi.Infrastructure.Data;

namespace UsersApi.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context) => _context = context;

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<IEnumerable<User>> GetAllAsync(CancellationToken ct = default)
        => await _context.Users.AsNoTracking()
            .OrderByDescending(u => u.CreatedAt).ToListAsync(ct);

    public async Task AddAsync(User user, CancellationToken ct = default)
        => await _context.Users.AddAsync(user, ct);

    public Task DeleteAsync(User user, CancellationToken ct = default)
    {
        _context.Users.Remove(user);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);
}
```

| Decisión | Por qué |
|---|---|
| `AsNoTracking()` en queries | Mejora rendimiento. Solo se necesita tracking en operaciones de escritura. |
| `OrderByDescending` en GetAll | Los usuarios más recientes primero. Mejor UX. |

### 5.3 Proveedor de BD intercambiable

**`src/Infrastructure/DependencyInjection.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UsersApi.Domain.Interfaces;
using UsersApi.Infrastructure.Data;
using UsersApi.Infrastructure.Repositories;

namespace UsersApi.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["Database:Provider"] ?? "InMemory";

        services.AddDbContext<AppDbContext>(options =>
        {
            switch (provider.ToLowerInvariant())
            {
                case "sqlserver":
                    options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
                    break;
                case "inmemory":
                default:
                    options.UseInMemoryDatabase("UsersDb");
                    break;
            }
        });

        services.AddScoped<IUserRepository, UserRepository>();
        return services;
    }
}
```

| Decisión | Por qué |
|---|---|
| Switch por variable de entorno | Cambiar de BD sin tocar código. Solo se cambia `Database__Provider` en `.env`. |
| InMemory como default | Desarrollo rápido sin dependencia de SQL Server. |
| Preparado para PostgreSQL | Solo agregar un `case "postgresql"` con `options.UseNpgsql(...)`. |

---

## 6. Capa Api

Es la capa más externa. Solo recibe requests HTTP, delega a Application y retorna respuestas.

### 6.1 Controllers

**`src/Api/Controllers/UsersController.cs`**

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<UserResponse>> Create(
        [FromBody] CreateUserRequest request, CancellationToken ct)
    {
        var user = await _createUserService.ExecuteAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserResponse>>> GetAll(CancellationToken ct)
        => Ok(await _getUsersService.ExecuteAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserResponse>> GetById(Guid id, CancellationToken ct)
        => Ok(await _getUserByIdService.ExecuteAsync(id, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _deleteUserService.ExecuteAsync(id, ct);
        return NoContent();
    }
}
```

| Decisión | Por qué |
|---|---|
| `[Authorize]` a nivel clase | Todos los endpoints requieren JWT. No se repite en cada método. |
| `[ApiController]` | Binding automático, validación de modelo, respuestas ProblemDetails. |
| `CreatedAtAction` | Retorna 201 + `Location` header con URL del recurso. RESTful. |
| `CancellationToken` en cada acción | Si el cliente cancela, ASP.NET propaga la cancelación hasta la BD. |

### 6.2 Middleware global de errores

**`src/Api/Middleware/GlobalExceptionMiddleware.cs`**

```csharp
public class GlobalExceptionMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await _next(context); }
        catch (ValidationException ex)     { /* 400 */ }
        catch (UnauthorizedAccessException ex) { /* 401 */ }
        catch (NotFoundException ex)       { /* 404 */ }
        catch (Exception ex)               { /* 500, oculta stack trace */ }
    }
}
```

| Decisión | Por qué |
|---|---|
| Un solo middleware | Consistencia: todas las excepciones producen el mismo formato JSON de error. |
| Stack trace oculto en 500 | Seguridad: no exponer detalles internos en producción. |
| Primer middleware del pipeline | Captura excepciones de cualquier capa inferior. |

### 6.3 Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// JWT config
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { /* validación de token */ });

// Swagger con botón Authorize
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    options.AddSecurityRequirement(/* requerir Bearer en todos los endpoints */);
});

builder.Services.AddHealthChecks();

var app = builder.Build();

// BD auto-create solo para SQL Server
if (app.Configuration["Database:Provider"] == "SqlServer")
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.UseGlobalExceptionMiddleware();
app.UseAuthentication();
app.UseAuthorization();
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.MapHealthChecks("/health");
app.MapControllers();
app.Run();
```

| Decisión | Por qué |
|---|---|
| Orden de middlewares | Exception → Auth → Swagger → Controllers. El orden importa. |
| `EnsureCreated()` solo para SQL Server | InMemory se crea sola. No forzar EnsureCreated en todos los providers. |
| Health check separado | Los orquestadores (Docker, Azure) lo usan para saber si la app está viva. |

---

## 7. Autenticación JWT

### 7.1 AuthService

**`src/Application/Services/AuthService.cs`**

```csharp
public class AuthService
{
    public LoginResponse Execute(LoginRequest request)
    {
        // Validar credenciales (hardcoded para demo)
        if (request.Username != "admin" || request.Password != "admin123")
            throw new UnauthorizedAccessException("Credenciales inválidas.");

        // Leer config JWT
        var secretKey = _configuration["Jwt:SecretKey"]
            ?? throw new InvalidOperationException("JWT SecretKey no configurada.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[] { /* sub, jti, iat */ };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return new LoginResponse(tokenHandler.WriteToken(token), tokenDescriptor.Expires.Value);
    }
}
```

| Decisión | Por qué |
|---|---|
| `SymmetricSecurityKey` (HMAC-SHA256) | Simple para demo. En producción usar RSA asimétrico o Azure Key Vault. |
| Secret en `IConfiguration` | Se inyecta desde `.env` o Azure env vars. No hardcodeado. |
| Claims: `sub`, `jti`, `iat` | Estándar JWT. `jti` permite implementar token blacklist en el futuro. |

### 7.2 AuthController

```csharp
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]  // Login no requiere token
public class AuthController : ControllerBase
{
    [HttpPost("login")]
    public ActionResult<LoginResponse> Login([FromBody] LoginRequest request)
    {
        var response = _authService.Execute(request);
        return Ok(response);
    }
}
```

### 7.3 Flujo completo

```
1. POST /api/auth/login  → { "username": "admin", "password": "admin123" }
2. Respuesta: { "token": "eyJ...", "expiresAt": "..." }
3. Copiar token
4. GET /api/users → Header: Authorization: Bearer eyJ...
5. ASP.NET valida firma, expiración, issuer, audience
6. Si es válido → ejecuta el controller
7. Si no → 401 Unauthorized
```

---

## 8. Pruebas unitarias

### 8.1 Estructura

```
tests/UnitTests/Services/
├── CreateUserServiceTests.cs
├── GetUserByIdServiceTests.cs
├── GetUsersServiceTests.cs
├── DeleteUserServiceTests.cs
└── AuthServiceTests.cs
```

### 8.2 Herramientas

| Herramienta | Uso |
|---|---|
| **xUnit** | Framework de testing. Atributo `[Fact]` para tests sin parámetros. |
| **Moq** | Crea mocks de interfaces (`Mock<IUserRepository>`). |
| **FluentAssertions** | Aserciones legibles: `result.Should().NotBeNull()`, `act.Should().ThrowAsync<T>()`. |

### 8.3 Implementación de todos los tests

#### CreateUserServiceTests.cs

```csharp
// 3 tests: éxito, email inválido, nombre vacío
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using UsersApi.Application.DTOs;
using UsersApi.Application.Exceptions;
using UsersApi.Application.Services;
using UsersApi.Application.Validators;
using UsersApi.Domain.Entities;
using UsersApi.Domain.Interfaces;
using Xunit;

namespace UsersApi.UnitTests.Services;

public class CreateUserServiceTests
{
    private readonly Mock<IUserRepository> _repositoryMock;
    private readonly CreateUserRequestValidator _validator;
    private readonly Mock<ILogger<CreateUserService>> _loggerMock;
    private readonly CreateUserService _service;

    public CreateUserServiceTests()
    {
        _repositoryMock = new Mock<IUserRepository>();
        _validator = new CreateUserRequestValidator();
        _loggerMock = new Mock<ILogger<CreateUserService>>();
        _service = new CreateUserService(_repositoryMock.Object, _validator, _loggerMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidRequest_ShouldCreateUserAndReturnResponse()
    {
        var request = new CreateUserRequest("Juan", "Pérez", "juan.perez@email.com");

        var result = await _service.ExecuteAsync(request);

        result.Should().NotBeNull();
        result.FirstName.Should().Be("Juan");
        result.LastName.Should().Be("Pérez");
        result.Email.Should().Be("juan.perez@email.com");
        result.Id.Should().NotBeEmpty();
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidEmail_ShouldThrowValidationException()
    {
        var request = new CreateUserRequest("Juan", "Pérez", "email-invalido");

        var act = async () => await _service.ExecuteAsync(request);

        var exception = await act.Should().ThrowAsync<ValidationException>();
        exception.And.Errors.Should().ContainKey("Email");
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyFirstName_ShouldThrowValidationException()
    {
        var request = new CreateUserRequest("", "Pérez", "juan@email.com");

        var act = async () => await _service.ExecuteAsync(request);

        var exception = await act.Should().ThrowAsync<ValidationException>();
        exception.And.Errors.Should().ContainKey("FirstName");
    }
}
```

#### GetUserByIdServiceTests.cs

```csharp
// 2 tests: usuario existe, usuario no existe
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using UsersApi.Application.Exceptions;
using UsersApi.Application.Services;
using UsersApi.Domain.Entities;
using UsersApi.Domain.Interfaces;
using Xunit;

namespace UsersApi.UnitTests.Services;

public class GetUserByIdServiceTests
{
    private readonly Mock<IUserRepository> _repositoryMock;
    private readonly Mock<ILogger<GetUserByIdService>> _loggerMock;
    private readonly GetUserByIdService _service;

    public GetUserByIdServiceTests()
    {
        _repositoryMock = new Mock<IUserRepository>();
        _loggerMock = new Mock<ILogger<GetUserByIdService>>();
        _service = new GetUserByIdService(_repositoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserExists_ShouldReturnUserResponse()
    {
        var userId = Guid.NewGuid();
        var existingUser = User.Create("María", "García", "maria@email.com");
        _repositoryMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        var result = await _service.ExecuteAsync(userId);

        result.Should().NotBeNull();
        result.FirstName.Should().Be("María");
        result.LastName.Should().Be("García");
        result.Email.Should().Be("maria@email.com");
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserDoesNotExist_ShouldThrowNotFoundException()
    {
        var userId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var act = async () => await _service.ExecuteAsync(userId);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"Usuario con ID {userId} no encontrado.");
    }
}
```

#### GetUsersServiceTests.cs

```csharp
// 2 tests: con usuarios, lista vacía
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using UsersApi.Application.Services;
using UsersApi.Domain.Entities;
using UsersApi.Domain.Interfaces;
using Xunit;

namespace UsersApi.UnitTests.Services;

public class GetUsersServiceTests
{
    private readonly Mock<IUserRepository> _repositoryMock;
    private readonly Mock<ILogger<GetUsersService>> _loggerMock;
    private readonly GetUsersService _service;

    public GetUsersServiceTests()
    {
        _repositoryMock = new Mock<IUserRepository>();
        _loggerMock = new Mock<ILogger<GetUsersService>>();
        _service = new GetUsersService(_repositoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUsersExist_ShouldReturnAllUsers()
    {
        var users = new List<User>
        {
            User.Create("Ana", "García", "ana@email.com"),
            User.Create("Luis", "Mendoza", "luis@email.com")
        };
        _repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        var result = await _service.ExecuteAsync();
        var resultList = result.ToList();

        resultList.Should().HaveCount(2);
        resultList[0].FirstName.Should().Be("Ana");
        resultList[1].Email.Should().Be("luis@email.com");
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoUsersExist_ShouldReturnEmptyList()
    {
        _repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User>());

        var result = await _service.ExecuteAsync();

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }
}
```

#### DeleteUserServiceTests.cs

```csharp
// 2 tests: elimina existente, lanza 404 si no existe
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using UsersApi.Application.Exceptions;
using UsersApi.Application.Services;
using UsersApi.Domain.Entities;
using UsersApi.Domain.Interfaces;
using Xunit;

namespace UsersApi.UnitTests.Services;

public class DeleteUserServiceTests
{
    private readonly Mock<IUserRepository> _repositoryMock;
    private readonly Mock<ILogger<DeleteUserService>> _loggerMock;
    private readonly DeleteUserService _service;

    public DeleteUserServiceTests()
    {
        _repositoryMock = new Mock<IUserRepository>();
        _loggerMock = new Mock<ILogger<DeleteUserService>>();
        _service = new DeleteUserService(_repositoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserExists_ShouldDeleteAndSave()
    {
        var userId = Guid.NewGuid();
        var existingUser = User.Create("Carlos", "Ruiz", "carlos@email.com");
        _repositoryMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        await _service.ExecuteAsync(userId);

        _repositoryMock.Verify(r => r.DeleteAsync(existingUser, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserDoesNotExist_ShouldThrowNotFoundException()
    {
        var userId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var act = async () => await _service.ExecuteAsync(userId);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"Usuario con ID {userId} no encontrado.");
    }
}
```

#### AuthServiceTests.cs

```csharp
// 4 tests: login válido, password mal, usuario mal, sin secret key
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using UsersApi.Application.DTOs;
using UsersApi.Application.Services;
using Xunit;

namespace UsersApi.UnitTests.Services;

public class AuthServiceTests
{
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        _configurationMock = new Mock<IConfiguration>();
        _configurationMock.Setup(c => c["Jwt:SecretKey"])
            .Returns("EstaEsUnaClaveSecretaSuperSeguraDe32Chars!!");
        _configurationMock.Setup(c => c["Jwt:Issuer"]).Returns("UsersApi");
        _configurationMock.Setup(c => c["Jwt:Audience"]).Returns("UsersApi");
        _configurationMock.Setup(c => c["Jwt:ExpirationMinutes"]).Returns("60");

        _loggerMock = new Mock<ILogger<AuthService>>();
        _service = new AuthService(_configurationMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void Execute_WithValidCredentials_ShouldReturnTokenAndExpiration()
    {
        var request = new LoginRequest("admin", "admin123");

        var result = _service.Execute(request);

        result.Token.Should().NotBeNullOrEmpty();
        result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
        // Un JWT tiene 3 partes: header.payload.signature
        result.Token.Split('.').Should().HaveCount(3);
    }

    [Fact]
    public void Execute_WithInvalidPassword_ShouldThrowUnauthorizedAccess()
    {
        var request = new LoginRequest("admin", "wrongpassword");

        var act = () => _service.Execute(request);

        act.Should().Throw<UnauthorizedAccessException>()
            .WithMessage("Credenciales inválidas.");
    }

    [Fact]
    public void Execute_WithInvalidUsername_ShouldThrowUnauthorizedAccess()
    {
        var request = new LoginRequest("invitado", "admin123");

        var act = () => _service.Execute(request);

        act.Should().Throw<UnauthorizedAccessException>()
            .WithMessage("Credenciales inválidas.");
    }

    [Fact]
    public void Execute_WhenSecretKeyNotConfigured_ShouldThrowInvalidOperationException()
    {
        var configMock = new Mock<IConfiguration>();
        var service = new AuthService(configMock.Object, _loggerMock.Object);
        var request = new LoginRequest("admin", "admin123");

        var act = () => service.Execute(request);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("JWT SecretKey no configurada.");
    }
}
```

| Decisión | Por qué |
|---|---|
| Validator real, no mock | Las reglas de FluentValidation son lógica de negocio que queremos probar. |
| Mock de `IUserRepository` | Aislamos el servicio de la BD. Tests rápidos y deterministas. |
| Mock de `ILogger<T>` | No necesitamos logs reales en tests. |

### 8.4 Total de tests: 13

| Clase | Tests | Escenarios |
|---|---|---|
| `CreateUserService` | 3 | éxito, email inválido, nombre vacío |
| `GetUserByIdService` | 2 | existe, no existe |
| `GetUsersService` | 2 | con datos, lista vacía |
| `DeleteUserService` | 2 | existe (elimina), no existe (404) |
| `AuthService` | 4 | login válido, password mal, usuario mal, sin secret key |

### 8.5 Ejecutar tests

```bash
dotnet test UsersApi.sln --configuration Release

# Con Docker
docker run --rm -v "${PWD}:/src" -w /src mcr.microsoft.com/dotnet/sdk:8.0 dotnet test
```

---

### 8.6 Pruebas de integración

Las pruebas de integración verifican que múltiples componentes reales funcionen juntos
correctamente, sin mocks. A diferencia de las unitarias, aquí se prueba la interacción
real entre capas.

**Qué probarían:**

| Prueba | Capas involucradas | Qué verifica |
|---|---|---|
| `POST /api/users` → BD real | Api → Application → Infrastructure → InMemory DB | Que el controller, servicio, validador, repositorio y DbContext funcionan integrados |
| `GET /api/users/{id}` con datos reales | Api → Infrastructure → InMemory DB | Que el flujo completo de lectura funciona |
| `DELETE /api/users/{id}` → verificar que desaparece | Api → Infrastructure → InMemory DB | Que la eliminación persiste en BD |
| Login → token → endpoint protegido | Api → AuthService → JWT middleware | Que el middleware JWT valida tokens emitidos por AuthService |

**Cómo se implementarían:**

1. **Proyecto de tests separado:** `tests/IntegrationTests/UsersApi.IntegrationTests.csproj` con referencia a `src/Api`
2. **`WebApplicationFactory<Program>`:** levanta la aplicación real en memoria, sin red. Permite hacer requests HTTP reales con `HttpClient`.
3. **BD InMemory real (no mockeada):** se configura `Database__Provider=InMemory` para que los tests usen la BD en memoria sin depender de SQL Server.
4. **`CustomWebApplicationFactory`:** hereda de `WebApplicationFactory` y sobrescribe servicios si es necesario (ej. sembrar datos de prueba).

```csharp
// Ejemplo conceptual de CustomWebApplicationFactory
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Users.Add(User.Create("Test", "User", "test@test.com"));
            db.SaveChanges();
        });
    }
}

// Ejemplo conceptual de test de integración
public class UsersControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public UsersControllerIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetUsers_ReturnsOkWithUsers()
    {
        var response = await _client.GetAsync("/api/users");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

**Ventajas:**
- Detecta errores de configuración (DI mal registrado, middlewares en orden incorrecto)
- Prueba el pipeline real de ASP.NET Core
- No requiere contenedores Docker ni red

**Cuándo usarlas:**
Después de tener cobertura unitaria sólida. Las de integración confirman que las piezas
encajan. No reemplazan las unitarias, las complementan.

---

### 8.7 Pruebas end-to-end (E2E)

Las pruebas E2E validan el sistema completo desde la perspectiva del usuario,
incluyendo infraestructura real (contenedores, BD, red).

**Qué probarían:**

| Prueba | Entorno | Qué verifica |
|---|---|---|
| Login → crear usuario → consultar → eliminar | Docker Compose con todos los servicios | Flujo de negocio completo sin mocks |
| Token expirado → 401 | Docker Compose | Que el middleware JWT rechaza tokens vencidos |
| Request sin token → 401 | Docker Compose | Que los endpoints protegidos requieren autenticación |
| Email inválido → 400 con errores por campo | Docker Compose | Que FluentValidation responde correctamente |

**Cómo se implementarían:**

1. **Proyecto separado:** `tests/E2ETests/UsersApi.E2ETests.csproj`
2. **Docker Compose para tests:** un `docker-compose.e2e.yml` que levanta la API con BD InMemory (sin SQL Server, para velocidad). La BD se reinicia entre tests.
3. **`HttpClient` contra `http://localhost:8080`:** requests reales a la API corriendo en contenedor.
4. **xUnit + FluentAssertions:** mismo stack que las unitarias y de integración.

**Flujo típico de un test E2E:**

```
1. docker compose -f docker-compose.e2e.yml up -d   ← levantar entorno
2. Esperar health check (GET /health → 200)
3. Ejecutar test: POST /api/auth/login → obtener token
4. Ejecutar test: POST /api/users → crear usuario
5. Ejecutar test: GET /api/users/{id} → verificar creación
6. Ejecutar test: DELETE /api/users/{id} → verificar eliminación
7. docker compose -f docker-compose.e2e.yml down    ← limpiar
```

**Ventajas:**
- Máxima confianza: prueba exactamente lo que corre en producción
- Detecta errores de Docker, red, puertos, variables de entorno
- Puede integrarse en CI/CD como última etapa antes del deploy

**Desventajas:**
- Lentas (levantar contenedores lleva segundos)
- Frágiles (dependen de puertos disponibles, estado del sistema)
- Más difíciles de debugear

**Cuándo usarlas:**
Solo para flujos críticos (login, CRUD principal). 2-4 tests E2E son suficientes.
No buscar 100% de cobertura con E2E. Las unitarias y de integración cubren el resto.

---

### 8.8 Pirámide de testing recomendada

```
        /\
       /E2E\          2-4 tests      ← Lentas, frágiles, máxima confianza
      /------\
     /Integra-\        8-12 tests     ← Velocidad media, detectan errores de ensamble
    /----------\
   / Unitarias  \      13+ tests      ← Rápidas, aíslan lógica, fáciles de mantener
  /--------------\
```

**Regla práctica:** si un bug se puede detectar con una prueba unitaria, hacelo ahí.
Solo subí a integración si necesitás probar la interacción entre capas.
Solo subí a E2E si es un flujo de negocio que no puede fallar.

---

## 9. Variables de entorno y configuración

### 9.1 Archivo `.env`

```
Database__Provider=InMemory
ConnectionStrings__DefaultConnection=Server=sqlserver,1433;Database=UsersDb;User Id=sa;Password=...;TrustServerCertificate=True;
MSSQL_SA_PASSWORD=YourStrong!Passw0rd
ASPNETCORE_ENVIRONMENT=Development
Jwt__SecretKey=clave-minimo-32-caracteres
Jwt__Issuer=UsersApi
Jwt__Audience=UsersApi
Jwt__ExpirationMinutes=60
```

### 9.2 Cómo se resuelven

En .NET, `__` (doble guion bajo) equivale a `:` en la jerarquía de configuración.

```
Jwt__SecretKey     → configuration["Jwt:SecretKey"]
Jwt__Issuer        → configuration["Jwt:Issuer"]
```

### 9.3 Orden de precedencia

1. Variables de entorno (`.env` cargado por docker-compose o Azure)
2. `appsettings.Development.json`
3. `appsettings.json`

| Decisión | Por qué |
|---|---|
| `.env` como fuente única local | No hay duplicación. Docker y desarrollo local leen del mismo archivo. |
| `appsettings.json` sin secretos | Solo estructura y defaults seguros. Se versiona. |
| `.env` en `.gitignore` | Las credenciales no se versionan. |

---

## 10. Docker

### 10.1 Dockerfile (multi-stage)

```dockerfile
# Stage 1: BUILD
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/*/*.csproj", ...]
RUN dotnet restore
COPY . .
RUN dotnet publish "src/Api/UsersApi.csproj" -c Release -o /app/publish

# Stage 2: RUNTIME
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
RUN adduser --disabled-password appuser && chown -R appuser /app
USER appuser
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "UsersApi.dll"]
```

| Decisión | Por qué |
|---|---|
| Multi-stage | Imagen final solo tiene runtime (~120 MB). SDK ocupa ~700 MB. |
| Usuario no-root | Si un atacante compromete la app, no tiene acceso root al contenedor. |
| `COPY *.csproj` primero | Docker cachea las capas. Si no cambia el csproj, no reinstala paquetes NuGet. |

### 10.2 docker-compose.yml

```yaml
services:
  api:
    build: .
    ports: ["8080:8080"]
    env_file: .env

  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    profiles: [sql]
    environment:
      - ACCEPT_EULA=Y
      - MSSQL_SA_PASSWORD=${MSSQL_SA_PASSWORD}
    healthcheck:
      test: ["CMD-SHELL", "/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P \"$$MSSQL_SA_PASSWORD\" -C -Q \"SELECT 1\""]
```

| Decisión | Por qué |
|---|---|
| `profiles: [sql]` | SQL Server solo se levanta si se pide explícitamente. Por defecto InMemory. |
| `env_file: .env` | Una sola fuente de variables. Sin duplicación. |
| `$$MSSQL_SA_PASSWORD` | El doble `$$` escapa la variable para que el shell del contenedor la expanda. |
| `-C` en sqlcmd | Requerido por SQL Server 2022+ para TrustServerCertificate. |

---

## 11. CI/CD con GitHub Actions

### 11.1 Workflow

**Archivo:** `.github/workflows/ci-cd.yml`

```yaml
on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main, develop]

jobs:
  ci:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: "8.0.x" }
      - run: dotnet restore UsersApi.sln
      - run: dotnet build UsersApi.sln -c Release --no-restore
      - run: dotnet test UsersApi.sln -c Release --no-build

  docker:
    needs: ci
    if: github.event_name == 'push' && github.ref == 'refs/heads/main'
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: azure/login@v2
        with: { creds: "${{ secrets.AZURE_CREDENTIALS }}" }
      - run: az acr login --name ${{ secrets.ACR_NAME }}
      - run: docker build -t ${{ secrets.ACR_NAME }}.azurecr.io/users-api:${{ github.sha }} .
      - run: docker push ${{ secrets.ACR_NAME }}.azurecr.io/users-api:${{ github.sha }}
      - run: |
          az containerapp update --name users-api \
            --resource-group ${{ secrets.AZURE_RESOURCE_GROUP }} \
            --image ${{ secrets.ACR_NAME }}.azurecr.io/users-api:${{ github.sha }} \
            --set-env-vars "Database__Provider=InMemory" ...
```

| Decisión | Por qué |
|---|---|
| `github.ref == 'refs/heads/main'` | Docker + deploy solo en push a main. Develop solo CI. |
| `needs: ci` | Si los tests fallan, no se construye Docker. |
| `azure/login@v2` con `creds` | Usa Service Principal con client secret. |
| `github.sha` como tag | Trazabilidad: cada imagen se vincula al commit exacto. |

---

## 12. Resumen: qué hace cada archivo

| Archivo | Capa | Responsabilidad |
|---|---|---|
| `User.cs` | Domain | Entidad con factory method y encapsulación |
| `IUserRepository.cs` | Domain | Contrato de persistencia |
| `CreateUserRequest.cs` | Application | DTO de entrada |
| `UserResponse.cs` | Application | DTO de salida con mapeo |
| `NotFoundException.cs` | Application | Excepción para recursos no encontrados |
| `ValidationException.cs` | Application | Excepción con errores por campo |
| `CreateUserRequestValidator.cs` | Application | Reglas FluentValidation |
| `*Service.cs` | Application | Casos de uso (orquestan Domain + Infrastructure) |
| `DependencyInjection.cs` | Application | Registro de servicios en el contenedor DI |
| `AppDbContext.cs` | Infrastructure | Contexto EF Core, Fluent API, índices |
| `UserRepository.cs` | Infrastructure | Implementación concreta del repositorio |
| `DependencyInjection.cs` | Infrastructure | Registro del DbContext y repositorio |
| `*Controller.cs` | Api | Endpoints HTTP |
| `GlobalExceptionMiddleware.cs` | Api | Manejo centralizado de errores |
| `Program.cs` | Api | Configuración del pipeline |
| `appsettings.json` | Api | Configuración base sin secretos |
| `Dockerfile` | Raíz | Build multi-stage |
| `docker-compose.yml` | Raíz | Orquestación local |
| `.env` | Raíz | Variables de entorno (única fuente local) |
| `ci-cd.yml` | .github | Pipeline CI/CD |
| `*Tests.cs` | Tests | Pruebas unitarias por servicio |
