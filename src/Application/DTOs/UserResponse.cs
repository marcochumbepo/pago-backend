// Application/DTOs/UserResponse.cs
// DTO de salida para todos los endpoints de consulta.
// Mapea la entidad de dominio a un formato seguro para exponer en la API,
// ocultando detalles internos que no deben salir del dominio.
using UsersApi.Domain.Entities;

namespace UsersApi.Application.DTOs;

public record UserResponse(Guid Id, string FirstName, string LastName, string Email, DateTime CreatedAt)
{
    // Método de mapeo estático: mantiene la lógica de transformación en un solo lugar,
    // facilitando el mantenimiento si la entidad User evoluciona.
    public static UserResponse FromEntity(User user) =>
        new(user.Id, user.FirstName, user.LastName, user.Email, user.CreatedAt);
}
