using DotNetPerformanceComparator.Domain.Entities;

namespace DotNetPerformanceComparator.Application.Migrations.ExperimentalMigration;

/// <summary>
/// Persistencia de <see cref="Migracion"/> requerida por CU-02.
/// </summary>
public interface IMigracionRepository
{
    Task AddAsync(Migracion migracion, CancellationToken cancellationToken = default);

    /// <summary>Actualiza estado, fechas, ruta y detalle de una migración existente.</summary>
    Task UpdateAsync(Migracion migracion, CancellationToken cancellationToken = default);
}
