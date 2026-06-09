// tests/UnitTests/Services/AuthServiceTests.cs
// Pruebas unitarias para AuthService.
// Usa Mock de IConfiguration para simular las claves JWT necesarias.
// Escenarios probados:
// 1. Login válido -> retorna token JWT
// 2. Contraseña inválida -> UnauthorizedAccessException
// 3. Usuario inválido -> UnauthorizedAccessException
// 4. SecretKey no configurada -> InvalidOperationException
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using UsersApi.Application.DTOs;
using UsersApi.Application.Services;
using Xunit;

namespace UsersApi.UnitTests.Services;

public class AuthServiceTests
{
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        // Arrange común: configuramos IConfiguration para retornar valores JWT válidos.
        _configurationMock = new Mock<IConfiguration>();

        // Simulamos la sección Jwt del appsettings / .env.
        // IConfiguration["Jwt:SecretKey"] busca en la clave "Jwt:SecretKey".
        _configurationMock.Setup(c => c["Jwt:SecretKey"])
            .Returns("EstaEsUnaClaveSecretaSuperSeguraDe32Chars!!");
        _configurationMock.Setup(c => c["Jwt:Issuer"]).Returns("UsersApi");
        _configurationMock.Setup(c => c["Jwt:Audience"]).Returns("UsersApi");
        _configurationMock.Setup(c => c["Jwt:ExpirationMinutes"]).Returns("60");

        _loggerMock = new Mock<ILogger<AuthService>>();
        _service = new AuthService(_configurationMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void Execute_WithValidCredentials_ShouldReturnTokenAndExpiration()
    {
        // Arrange: credenciales correctas.
        var request = new LoginRequest("admin", "admin123");

        // Act: ejecutamos el login.
        var result = _service.Execute(request);

        // Assert: el token no puede ser nulo o vacío, y la expiración debe ser futura.
        result.Token.Should().NotBeNullOrEmpty();
        result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
        // El token JWT tiene 3 partes separadas por punto (header.payload.signature).
        result.Token.Split('.').Should().HaveCount(3);
    }

    [Fact]
    public void Execute_WithInvalidPassword_ShouldThrowUnauthorizedAccess()
    {
        // Arrange: contraseña incorrecta.
        var request = new LoginRequest("admin", "wrongpassword");

        // Act: ejecutamos el login.
        var act = () => _service.Execute(request);

        // Assert: debe lanzar UnauthorizedAccessException.
        act.Should().Throw<UnauthorizedAccessException>()
            .WithMessage("Credenciales inválidas.");
    }

    [Fact]
    public void Execute_WithInvalidUsername_ShouldThrowUnauthorizedAccess()
    {
        // Arrange: usuario incorrecto.
        var request = new LoginRequest("invitado", "admin123");

        // Act
        var act = () => _service.Execute(request);

        // Assert: debe lanzar UnauthorizedAccessException.
        act.Should().Throw<UnauthorizedAccessException>()
            .WithMessage("Credenciales inválidas.");
    }

    [Fact]
    public void Execute_WhenSecretKeyNotConfigured_ShouldThrowInvalidOperationException()
    {
        // Arrange: creamos un servicio SIN secret key configurada.
        var configMock = new Mock<IConfiguration>();
        // No configuramos Jwt:SecretKey -> retorna null.
        var service = new AuthService(configMock.Object, _loggerMock.Object);
        var request = new LoginRequest("admin", "admin123");

        // Act & Assert: debe lanzar InvalidOperationException.
        var act = () => service.Execute(request);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("JWT SecretKey no configurada.");
    }
}
