// Application/Services/GetUserByIdService.cs
// Recupera un usuario por ID. Si no existe, lanza NotFoundException (HTTP 404).
using Microsoft.Extensions.Logging;
using UsersApi.Application.DTOs;
using UsersApi.Application.Exceptions;
using UsersApi.Domain.Interfaces;

namespace UsersApi.Application.Services;

public class GetUserByIdService
{
    private readonly IUserRepository _repository;
    private readonly ILogger<GetUserByIdService> _logger;

    public GetUserByIdService(IUserRepository repository, ILogger<GetUserByIdService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<UserResponse> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Buscando usuario con ID {UserId}", id);

        var user = await _repository.GetByIdAsync(id, cancellationToken);

        if (user is null)
        {
            _logger.LogWarning("Usuario con ID {UserId} no encontrado.", id);
            throw new NotFoundException($"Usuario con ID {id} no encontrado.");
        }

        return UserResponse.FromEntity(user);
    }
}
