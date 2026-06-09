// Infrastructure/Data/AppDbContext.cs
// Contexto de Entity Framework Core para SQL Server.
// Actúa como Unit of Work y gateway a la base de datos.
// DbSet<User> mapea la entidad a una tabla en la BD.
using Microsoft.EntityFrameworkCore;
using UsersApi.Domain.Entities;

namespace UsersApi.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configuración explícita de la entidad User usando Fluent API.
        // Esto evita depender de Data Annotations en la capa de dominio,
        // manteniéndola limpia de dependencias de infraestructura.
        modelBuilder.Entity<User>(entity =>
        {
            // Clave primaria.
            entity.HasKey(u => u.Id);

            // FirstName: requerido, max 100 caracteres.
            entity.Property(u => u.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            // LastName: requerido, max 100 caracteres.
            entity.Property(u => u.LastName)
                .IsRequired()
                .HasMaxLength(100);

            // Email: requerido, max 256 caracteres (longitud máxima de email según RFC).
            entity.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(256);

            // Índice único en Email para evitar duplicados a nivel de base de datos.
            entity.HasIndex(u => u.Email).IsUnique();

            // CreatedAt: se establece en UTC al crear.
            entity.Property(u => u.CreatedAt)
                .IsRequired();
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Conversión automática de fechas a UTC antes de guardar,
        // asegurando consistencia temporal en entornos distribuidos.
        foreach (var entry in ChangeTracker.Entries<User>())
        {
            if (entry.State == EntityState.Added && entry.Entity.CreatedAt.Kind != DateTimeKind.Utc)
            {
                entry.Entity.GetType().GetProperty(nameof(User.CreatedAt))?
                    .SetValue(entry.Entity, DateTime.SpecifyKind(entry.Entity.CreatedAt, DateTimeKind.Utc));
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
