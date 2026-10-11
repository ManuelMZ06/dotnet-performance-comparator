using DotNetPerformanceComparator.Application.Evaluations.Execution;
using DotNetPerformanceComparator.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DotNetPerformanceComparator.Infrastructure.Persistence.Repositories;

/// <summary>
/// Persistencia de CU-04 con contextos de vida corta (<see cref="IDbContextFactory{AppDbContext}"/>).
/// </summary>
public sealed class EvaluationExecutionRepository : IEvaluationExecutionRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public EvaluationExecutionRepository(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<ConfiguracionEvaluacion?> GetConfiguracionAsync(int configuracionId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.ConfiguracionesEvaluacion
            .AsNoTracking()
            .Include(c => c.Proyecto)
            .Include(c => c.Migracion)
            .Include(c => c.TipoPrueba)
            .Include(c => c.Parametros)
            .Include(c => c.ConfiguracionesMetrica).ThenInclude(cm => cm.Metrica)
            .Include(c => c.Ejecuciones)
            .FirstOrDefaultAsync(c => c.Id == configuracionId, cancellationToken);
    }

    public async Task ActualizarEstadoConfiguracionAsync(int configuracionId, string estado, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var config = await context.ConfiguracionesEvaluacion.FirstOrDefaultAsync(c => c.Id == configuracionId, cancellationToken)
            ?? throw new InvalidOperationException($"No existe la configuración {configuracionId}.");

        config.Estado = estado;
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task AgregarEjecucionAsync(Ejecucion ejecucion, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        context.Ejecuciones.Add(ejecucion);
        await context.SaveChangesAsync(cancellationToken);
    }
}
