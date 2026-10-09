using DotNetPerformanceComparator.Application.Evaluations.Configuration;
using DotNetPerformanceComparator.Domain.Constants;
using DotNetPerformanceComparator.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DotNetPerformanceComparator.Infrastructure.Persistence.Repositories;

/// <summary>
/// Consultas de solo lectura (sin seguimiento) para CU-03, con contextos de vida corta.
/// </summary>
public sealed class EvaluationCatalogRepository : IEvaluationCatalogRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public EvaluationCatalogRepository(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<ProyectoDotNet>> GetProyectosAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.ProyectosDotNet.AsNoTracking()
            .OrderBy(p => p.Nombre).ThenBy(p => p.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Migracion>> GetMigracionesExitosasAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Migraciones.AsNoTracking()
            .Where(m => m.Estado == EstadosMigracion.Exitosa)
            .OrderBy(m => m.ProyectoId).ThenBy(m => m.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TipoPrueba>> GetTiposPruebaAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.TiposPrueba.AsNoTracking().OrderBy(t => t.Id).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Metrica>> GetMetricasAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Metricas.AsNoTracking().OrderBy(m => m.Id).ToListAsync(cancellationToken);
    }

    public async Task<Migracion?> GetMigracionByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Migraciones.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }
}
