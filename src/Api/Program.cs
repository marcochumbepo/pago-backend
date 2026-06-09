// Api/Program.cs
// Punto de entrada de la aplicación.
// Configura el pipeline de ASP.NET Core: servicios, middleware y endpoints.
// 
// Orden de los middlewares:
// 1. GlobalExceptionMiddleware (primero para capturar errores de todo el pipeline)
// 2. Authentication (valida el token JWT en requests protegidos)
// 3. Authorization (verifica permisos/roles si aplica)
// 4. Swagger (solo en desarrollo)
// 5. Routing + Controllers
// 6. Health checks
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using UsersApi.Api.Middleware;
using UsersApi.Application;
using UsersApi.Infrastructure;
using UsersApi.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// ==================== SERVICES ====================

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// JWT Authentication: configura cómo se validan los tokens entrantes.
// La clave secreta se lee de configuración (appsettings.json o variable de entorno).
// En producción, usar RSA asimétrico o Azure Key Vault para la clave.
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"]
    ?? throw new InvalidOperationException("JWT SecretKey no configurada. Agregar Jwt__SecretKey en .env.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "UsersApi";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "UsersApi";

builder.Services.AddAuthentication(options =>
{
    // JwtBearer: el esquema por defecto. El cliente envía "Authorization: Bearer <token>".
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        // Validar que el token fue emitido por nosotros (issuer) y para nosotros (audience).
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true, // Rechaza tokens expirados.
        ValidateIssuerSigningKey = true, // Verifica la firma HMAC/RSA del token.
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
        // ClockSkew: margen de tolerancia para la expiración (default 5 min).
        // Lo reducimos a 0 para que el token expire exactamente a la hora indicada.
        ClockSkew = TimeSpan.Zero
    };
});

// Swagger con soporte para JWT Bearer token.
// Agrega el botón "Authorize" en Swagger UI donde se puede pegar el token.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Users API", Version = "v1" });

    // Definición de seguridad para Swagger: permite ingresar el token JWT
    // en la UI y lo adjunta automáticamente a todas las requests.
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Ingresar el token JWT: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    // Requerir el token por defecto en todos los endpoints,
    // excepto los que tengan [AllowAnonymous].
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddHealthChecks();

var app = builder.Build();

// Asegurar que la base de datos y tablas existan al iniciar.
// Solo aplica cuando se usa SQL Server. Con InMemory la BD se crea automáticamente.
// En producción usar Migrate() con migraciones versionadas en lugar de EnsureCreated().
var dbProvider = builder.Configuration["Database:Provider"] ?? "InMemory";
if (dbProvider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<UsersApi.Infrastructure.Data.AppDbContext>();
    dbContext.Database.EnsureCreated();
}

// ==================== MIDDLEWARE PIPELINE ====================

// GlobalExceptionMiddleware primero: captura cualquier excepción del pipeline.
app.UseGlobalExceptionMiddleware();

// Authentication debe ir ANTES de Authorization y antes de los endpoints.
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();
