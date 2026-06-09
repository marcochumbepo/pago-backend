// Application/DTOs/CreateUserRequest.cs
// DTO de entrada para el endpoint POST /api/users.
// Separa el contrato de la API de la entidad de dominio,
// permitiendo que el schema de la API evolucione independientemente del modelo de dominio.
namespace UsersApi.Application.DTOs;

public record CreateUserRequest(string FirstName, string LastName, string Email);
