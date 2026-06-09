// Domain/Entities/User.cs
// Representa la entidad de negocio User siguiendo el patrón Rich Domain Model.
// Propiedades con private set para encapsulación y control de cambios desde métodos de dominio si se requirieran.
namespace UsersApi.Domain.Entities;

public class User
{
    // Se declaran con null! para suprimir CS8618: el factory method Create() garantiza
    // que las propiedades se inicializan antes de que el objeto sea usado.
    // Alternativamente EF Core las inicializa por reflexión al materializar desde la BD.
    public Guid Id { get; private set; }
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }

    // Constructor requerido por EF Core para materializar objetos desde la base de datos.
    // Es private para forzar el uso del factory method Create() y mantener la integridad del dominio.
    private User() { }

    // Factory method estático: centraliza la lógica de creación, asegurando que toda instancia
    // cumpla las reglas de negocio (Id autogenerado, CreatedAt = UtcNow).
    public static User Create(string firstName, string lastName, string email)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            CreatedAt = DateTime.UtcNow
        };
    }
}
