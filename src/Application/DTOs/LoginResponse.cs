// Application/DTOs/LoginResponse.cs
// DTO de salida para el endpoint POST /api/auth/login.
// Retorna el token JWT y su fecha de expiración para que el cliente
// pueda gestionar el refresco del token antes de que expire.
namespace UsersApi.Application.DTOs;

public record LoginResponse(string Token, DateTime ExpiresAt);
