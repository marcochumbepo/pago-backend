// tests/UnitTests/Services/GetUserByIdServiceTests.cs
// Pruebas unitarias para GetUserByIdService.
// Escenarios probados:
// 1. Usuario existe -> retorna UserResponse
// 2. Usuario no existe -> lanza NotFoundException
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
        // Arrange: simulamos que el repositorio encuentra un usuario.
        var userId = Guid.NewGuid();
        var existingUser = User.Create("María", "García", "maria@email.com");

        // Para que el ID coincida, usamos reflexión o recreamos la entidad.
        // Alternativa más limpia: mockear GetByIdAsync para que retorne la entidad esperada.
        _repositoryMock
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _service.ExecuteAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.FirstName.Should().Be("María");
        result.LastName.Should().Be("García");
        result.Email.Should().Be("maria@email.com");
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange: el repositorio retorna null (usuario no encontrado).
        var userId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act & Assert
        var act = async () => await _service.ExecuteAsync(userId);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"Usuario con ID {userId} no encontrado.");
    }
}
