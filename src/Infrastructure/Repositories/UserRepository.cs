// Infrastructure/Repositories/UserRepository.cs
// Implementación concreta de IUserRepository usando Entity Framework Core.
// Esta es la ÚNICA clase que depende directamente de EF Core, cumpliendo con
// el principio de inversión de dependencias: las capas superiores solo conocen la interfaz.
using Microsoft.EntityFrameworkCore;
using UsersApi.Domain.Entities;
using UsersApi.Domain.Interfaces;
using UsersApi.Infrastructure.Data;

namespace UsersApi.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    // El DbContext se inyecta (Dependency Injection), no se instancia manualmente.
    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // AsNoTracking para consultas de solo lectura: mejora el rendimiento
        // al no adjuntar la entidad al ChangeTracker.
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(user, cancellationToken);
    }

    public Task DeleteAsync(User user, CancellationToken cancellationToken = default)
    {
        _context.Users.Remove(user);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
