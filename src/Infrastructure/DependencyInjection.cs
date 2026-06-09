// Infrastructure/DependencyInjection.cs
// Centraliza el registro de servicios de Infrastructure en el contenedor DI.
// Sigue el patrón de extensión IServiceCollection para mantener Program.cs limpio.
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
        // Registro del DbContext con SQL Server.
        // La connection string se lee desde appsettings.json o variables de entorno,
        // permitiendo diferentes configuraciones por ambiente (dev, prod).
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Registro del repositorio como scoped: una instancia por request HTTP,
        // alineado con el ciclo de vida del DbContext que también es scoped.
        services.AddScoped<IUserRepository, UserRepository>();

        return services;
    }
}
