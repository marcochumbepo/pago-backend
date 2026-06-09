// Domain/Interfaces/IUserRepository.cs
// Define el contrato de persistencia para la entidad User.
// Se ubica en la capa Domain para cumplir con el Dependency Inversion Principle:
// las capas superiores dependen de esta abstracción, no de la implementación concreta en Infrastructure.
using UsersApi.Domain.Entities;

namespace UsersApi.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    Task DeleteAsync(User user, CancellationToken cancellationToken = default);
    // SaveChangesAsync centraliza el commit de la unidad de trabajo en un solo método,
    // evitando guardados parciales y permitiendo operaciones transaccionales.
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
