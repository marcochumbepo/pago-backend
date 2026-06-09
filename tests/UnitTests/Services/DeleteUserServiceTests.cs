// tests/UnitTests/Services/DeleteUserServiceTests.cs
// Pruebas unitarias para DeleteUserService.
// Escenarios probados:
// 1. Eliminar usuario existente -> éxito
// 2. Eliminar usuario inexistente -> NotFoundException
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
        // Arrange: simulamos que el repositorio encuentra un usuario.
        var userId = Guid.NewGuid();
        var existingUser = User.Create("Carlos", "Ruiz", "carlos@email.com");

        _repositoryMock
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        // Act: ejecutamos la eliminación.
        await _service.ExecuteAsync(userId);

        // Assert: verificamos que se llamó a DeleteAsync y SaveChangesAsync una vez cada uno.
        _repositoryMock.Verify(r => r.DeleteAsync(existingUser, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange: el repositorio retorna null (usuario no encontrado).
        var userId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act & Assert: debe lanzar NotFoundException.
        var act = async () => await _service.ExecuteAsync(userId);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"Usuario con ID {userId} no encontrado.");
    }
}
