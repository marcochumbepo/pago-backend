// tests/UnitTests/Services/CreateUserServiceTests.cs
// Pruebas unitarias para CreateUserService usando xUnit, Moq y FluentAssertions.
// 
// Moq: simula IUserRepository e ILogger para aislar la unidad bajo prueba.
// FluentAssertions: sintaxis fluida y legible para aserciones.
// 
// Se prueban 2 escenarios (mínimo requerido):
// 1. Creación exitosa
// 2. Email inválido
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using UsersApi.Application.DTOs;
using UsersApi.Application.Exceptions;
using UsersApi.Application.Services;
using UsersApi.Application.Validators;
using UsersApi.Domain.Entities;
using UsersApi.Domain.Interfaces;
using Xunit;

namespace UsersApi.UnitTests.Services;

public class CreateUserServiceTests
{
    private readonly Mock<IUserRepository> _repositoryMock;
    private readonly CreateUserRequestValidator _validator;
    private readonly Mock<ILogger<CreateUserService>> _loggerMock;
    private readonly CreateUserService _service;

    public CreateUserServiceTests()
    {
        // Arrange común: inicializamos los mocks y el servicio bajo prueba.
        // Usamos el validador real (no mock) porque contiene reglas de negocio que queremos probar.
        _repositoryMock = new Mock<IUserRepository>();
        _validator = new CreateUserRequestValidator();
        _loggerMock = new Mock<ILogger<CreateUserService>>();
        _service = new CreateUserService(_repositoryMock.Object, _validator, _loggerMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidRequest_ShouldCreateUserAndReturnResponse()
    {
        // Arrange: preparamos un request válido.
        var request = new CreateUserRequest("Juan", "Pérez", "juan.perez@email.com");

        // Act: ejecutamos el servicio.
        var result = await _service.ExecuteAsync(request);

        // Assert: verificamos el resultado.
        result.Should().NotBeNull();
        result.FirstName.Should().Be("Juan");
        result.LastName.Should().Be("Pérez");
        result.Email.Should().Be("juan.perez@email.com");
        result.Id.Should().NotBeEmpty(); // Guid autogenerado
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1)); // Fecha reciente

        // Verificamos que se llamó al repositorio exactamente una vez.
        _repositoryMock.Verify(r => r.AddAsync(
            It.IsAny<User>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _repositoryMock.Verify(r => r.SaveChangesAsync(
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidEmail_ShouldThrowValidationException()
    {
        // Arrange: email sin formato válido.
        var request = new CreateUserRequest("Juan", "Pérez", "email-invalido");

        // Act & Assert: debe lanzar ValidationException.
        var act = async () => await _service.ExecuteAsync(request);

        var exception = await act.Should().ThrowAsync<ValidationException>();
        exception.And.Errors.Should().ContainKey("Email");
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyFirstName_ShouldThrowValidationException()
    {
        // Arrange: nombre vacío (prueba adicional que demuestra cobertura completa).
        var request = new CreateUserRequest("", "Pérez", "juan@email.com");

        // Act & Assert
        var act = async () => await _service.ExecuteAsync(request);

        var exception = await act.Should().ThrowAsync<ValidationException>();
        exception.And.Errors.Should().ContainKey("FirstName");
    }
}
