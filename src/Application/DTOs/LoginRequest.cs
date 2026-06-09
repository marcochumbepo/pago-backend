// Application/DTOs/LoginRequest.cs
// DTO de entrada para el endpoint POST /api/auth/login.
// Contiene las credenciales mínimas necesarias para autenticar a un usuario.
namespace UsersApi.Application.DTOs;

public record LoginRequest(string Username, string Password);
