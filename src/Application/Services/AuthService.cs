// Application/Services/AuthService.cs
// Servicio de autenticación: valida credenciales y genera tokens JWT.
//
// En una aplicación real, las credenciales se validarían contra la base de datos
// usando ASP.NET Core Identity o un proveedor externo (Azure AD, Auth0, etc.).
// Aquí se usa una validación simplificada para la prueba técnica.
//
// La configuración JWT (secret, issuer, audience, expiración) se lee desde
// IConfiguration, que toma los valores de appsettings.json o variables de entorno.
// Esto permite diferentes configuraciones por ambiente sin recompilar.
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using UsersApi.Application.DTOs;
using UsersApi.Application.Exceptions;

namespace UsersApi.Application.Services;

public class AuthService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IConfiguration configuration, ILogger<AuthService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public LoginResponse Execute(LoginRequest request)
    {
        // En producción, aquí validarías contra base de datos o Identity Provider.
        // Para la prueba técnica se usan credenciales fijas.
        if (request.Username != "admin" || request.Password != "admin123")
        {
            _logger.LogWarning("Intento de login fallido para usuario {Username}", request.Username);
            throw new UnauthorizedAccessException("Credenciales inválidas.");
        }

        // Leer configuración JWT desde appsettings / variables de entorno.
        // En .env: Jwt__SecretKey, Jwt__Issuer, Jwt__Audience, Jwt__ExpirationMinutes
        var secretKey = _configuration["Jwt:SecretKey"]
            ?? throw new InvalidOperationException("JWT SecretKey no configurada.");
        var issuer = _configuration["Jwt:Issuer"] ?? "UsersApi";
        var audience = _configuration["Jwt:Audience"] ?? "UsersApi";
        var expirationMinutes = int.TryParse(_configuration["Jwt:ExpirationMinutes"], out var minutes) ? minutes : 60;

        var expiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes);

        // Claims: información que viaja codificada dentro del token.
        // - sub: identificador del usuario (estándar JWT)
        // - jti: identificador único del token (permite revocación si se implementa blacklist)
        // - iat: timestamp de emisión
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, request.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
        };

        // Clave simétrica: en producción usar RSA (asymmetric) o Azure Key Vault.
        // HMAC-SHA256 es adecuado para APIs internas o pruebas.
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt,
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        _logger.LogInformation("Token JWT generado para usuario {Username}, expira {ExpiresAt}",
            request.Username, expiresAt);

        return new LoginResponse(tokenString, expiresAt);
    }
}
