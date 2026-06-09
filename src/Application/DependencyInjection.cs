// Application/DependencyInjection.cs
// Centraliza el registro de servicios de Application en el contenedor DI.
using Microsoft.Extensions.DependencyInjection;
using UsersApi.Application.Services;
using UsersApi.Application.Validators;

namespace UsersApi.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Registro de servicios como scoped: ciclo de vida alineado con el request HTTP.
        services.AddScoped<CreateUserService>();
        services.AddScoped<GetUsersService>();
        services.AddScoped<GetUserByIdService>();
        services.AddScoped<DeleteUserService>();
        services.AddScoped<AuthService>();

        // Registro de validadores FluentValidation como scoped.
        services.AddScoped<CreateUserRequestValidator>();

        return services;
    }
}
