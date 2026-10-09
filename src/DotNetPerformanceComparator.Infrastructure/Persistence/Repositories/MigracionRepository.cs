using DotNetPerformanceComparator.Application.Migrations.ExperimentalMigration;
using DotNetPerformanceComparator.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DotNetPerformanceComparator.Infrastructure.Persistence.Repositories;

/// <summary>
/// Usa un <see cref="AppDbContext"/> de vida corta por operación (adecuado para Blazor Server).
/// </summary>
public sealed class MigracionRepository : IMigracionRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public MigracionRepository(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task AddAsync(Migracion migracion, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        context.Migraciones.Add(migracion);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Migracion migracion, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var existente = await context.Migraciones.FirstOrDefaultAsync(m => m.Id == migracion.Id, cancellationToken)
            ?? throw new InvalidOperationException($"No existe la migración {migracion.Id}.");

        existente.Estado = migracion.Estado;
        existente.RutaMigrada = migracion.RutaMigrada;
        existente.FechaFin = migracion.FechaFin;
        existente.Detalle = migracion.Detalle;
        await context.SaveChangesAsync(cancellationToken);
    }
}
