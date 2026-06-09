// Infrastructure/DependencyInjection.cs
// Centraliza el registro de servicios de Infrastructure en el contenedor DI.
// Soporta dos proveedores de base de datos intercambiables mediante configuración:
//
// Database__Provider=InMemory  → EF Core InMemory (desarrollo/demo, sin dependencia de SQL Server)
// Database__Provider=SqlServer → SQL Server (producción)
//
// Para migrar a PostgreSQL en el futuro solo se necesita:
// 1. Agregar el paquete Npgsql.EntityFrameworkCore.PostgreSQL
// 2. Agregar un nuevo case "PostgreSql" aquí con .UseNpgsql(connectionString)
// Las capas Domain y Application no requieren cambios.
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
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Leer el proveedor desde configuración. Si no se especifica, usar InMemory.
        // En .env: Database__Provider=InMemory o Database__Provider=SqlServer
        var provider = configuration["Database:Provider"] ?? "InMemory";

        // El DbContext se registra una sola vez, pero con el provider que corresponda.
        // Esto permite cambiar de BD sin tocar código de Application ni Domain.
        services.AddDbContext<AppDbContext>(options =>
        {
            switch (provider.ToLowerInvariant())
            {
                case "sqlserver":
                    var connectionString = configuration.GetConnectionString("DefaultConnection");
                    options.UseSqlServer(connectionString);
                    break;

                case "postgresql":
                    // Ejemplo para futura migración a PostgreSQL:
                    // var pgConnectionString = configuration.GetConnectionString("DefaultConnection");
                    // options.UseNpgsql(pgConnectionString);
                    // break;
                    throw new NotSupportedException(
                        "PostgreSQL no está implementado. Agregar paquete Npgsql.EntityFrameworkCore.PostgreSQL y habilitar este case.");

                case "inmemory":
                default:
                    // InMemory: ideal para desarrollo y pruebas. Los datos se pierden al reiniciar.
                    // Nombre único para evitar conflictos entre tests en paralelo.
                    options.UseInMemoryDatabase("UsersDb");
                    break;
            }
        });

        services.AddScoped<IUserRepository, UserRepository>();

        return services;
    }
}
