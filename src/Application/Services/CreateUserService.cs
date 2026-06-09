// Application/Services/CreateUserService.cs
// Orquesta la creación de un usuario: valida el request, crea la entidad de dominio,
// persiste mediante el repositorio y retorna el DTO de respuesta.
// Depende de IUserRepository (abstracción, no implementación concreta) cumpliendo DIP de SOLID.
using FluentValidation;
using Microsoft.Extensions.Logging;
using UsersApi.Application.DTOs;
using UsersApi.Application.Validators;
using UsersApi.Domain.Entities;
using UsersApi.Domain.Interfaces;

namespace UsersApi.Application.Services;

public class CreateUserService
{
    private readonly IUserRepository _repository;
    private readonly CreateUserRequestValidator _validator;
    // ILogger inyectado para trazabilidad de operaciones y debugging.
    private readonly ILogger<CreateUserService> _logger;

    public CreateUserService(
        IUserRepository repository,
        CreateUserRequestValidator validator,
        ILogger<CreateUserService> logger)
    {
        _repository = repository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<UserResponse> ExecuteAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        // Validación temprana: si el request no cumple las reglas, lanzamos ValidationException
        // que será capturada por el GlobalExceptionMiddleware para retornar 400.
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            _logger.LogWarning("Validación fallida al crear usuario: {@Errors}", errors);
            throw new Exceptions.ValidationException("Error de validación al crear usuario.", errors);
        }

        // Factory method: delegamos la creación al dominio para mantener la lógica de negocio encapsulada.
        var user = User.Create(request.FirstName, request.LastName, request.Email);

        await _repository.AddAsync(user, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Usuario creado exitosamente con ID {UserId}", user.Id);

        return UserResponse.FromEntity(user);
    }
}
