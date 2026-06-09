// tests/UnitTests/Services/GetUsersServiceTests.cs
// Pruebas unitarias para GetUsersService.
// Escenarios probados:
// 1. Recuperar todos los usuarios -> retorna lista con elementos
// 2. Sin usuarios en BD -> retorna lista vacía
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using UsersApi.Application.Services;
using UsersApi.Domain.Entities;
using UsersApi.Domain.Interfaces;
using Xunit;

namespace UsersApi.UnitTests.Services;

public class GetUsersServiceTests
{
    private readonly Mock<IUserRepository> _repositoryMock;
    private readonly Mock<ILogger<GetUsersService>> _loggerMock;
    private readonly GetUsersService _service;

    public GetUsersServiceTests()
    {
        _repositoryMock = new Mock<IUserRepository>();
        _loggerMock = new Mock<ILogger<GetUsersService>>();
        _service = new GetUsersService(_repositoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUsersExist_ShouldReturnAllUsers()
    {
        // Arrange: el repositorio retorna 2 usuarios.
        var users = new List<User>
        {
            User.Create("Ana", "García", "ana@email.com"),
            User.Create("Luis", "Mendoza", "luis@email.com")
        };

        _repositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        // Act: ejecutamos la consulta.
        var result = await _service.ExecuteAsync();

        // Assert: verificamos que retorna los 2 usuarios mapeados a DTO.
        var resultList = result.ToList();
        resultList.Should().HaveCount(2);
        resultList[0].FirstName.Should().Be("Ana");
        resultList[1].Email.Should().Be("luis@email.com");
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoUsersExist_ShouldReturnEmptyList()
    {
        // Arrange: el repositorio retorna lista vacía.
        _repositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User>());

        // Act
        var result = await _service.ExecuteAsync();

        // Assert: lista vacía, no nula.
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }
}
