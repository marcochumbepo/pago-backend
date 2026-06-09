// Application/Services/GetUsersService.cs
// Recupera todos los usuarios mediante el repositorio y los mapea a DTOs.
// Servicio simple que demuestra separación de responsabilidades.
using Microsoft.Extensions.Logging;
using UsersApi.Application.DTOs;
using UsersApi.Domain.Interfaces;

namespace UsersApi.Application.Services;

public class GetUsersService
{
    private readonly IUserRepository _repository;
    private readonly ILogger<GetUsersService> _logger;

    public GetUsersService(IUserRepository repository, ILogger<GetUsersService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IEnumerable<UserResponse>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Obteniendo todos los usuarios.");

        var users = await _repository.GetAllAsync(cancellationToken);

        // Mapeo a DTO para no exponer la entidad de dominio directamente.
        return users.Select(UserResponse.FromEntity);
    }
}
