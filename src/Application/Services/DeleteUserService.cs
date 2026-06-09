// Application/Services/DeleteUserService.cs
// Elimina un usuario por ID. Si no existe, lanza NotFoundException (HTTP 404).
// La eliminación es física (no soft delete) según los requisitos de la prueba.
using Microsoft.Extensions.Logging;
using UsersApi.Application.Exceptions;
using UsersApi.Domain.Interfaces;

namespace UsersApi.Application.Services;

public class DeleteUserService
{
    private readonly IUserRepository _repository;
    private readonly ILogger<DeleteUserService> _logger;

    public DeleteUserService(IUserRepository repository, ILogger<DeleteUserService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Eliminando usuario con ID {UserId}", id);

        var user = await _repository.GetByIdAsync(id, cancellationToken);

        if (user is null)
        {
            _logger.LogWarning("Usuario con ID {UserId} no encontrado para eliminar.", id);
            throw new NotFoundException($"Usuario con ID {id} no encontrado.");
        }

        await _repository.DeleteAsync(user, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Usuario con ID {UserId} eliminado exitosamente.", id);
    }
}
