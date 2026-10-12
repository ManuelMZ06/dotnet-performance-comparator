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

    public async Task<int> CrearEjecucionEnProcesoAsync(Ejecucion ejecucion, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        context.Ejecuciones.Add(ejecucion);
        await context.SaveChangesAsync(cancellationToken);
        return ejecucion.Id;
    }

    public async Task<int> AgregarNivelEnProcesoAsync(NivelEstres nivel, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        context.NivelesEstres.Add(nivel);
        await context.SaveChangesAsync(cancellationToken);
        return nivel.Id;
    }

    public async Task CompletarNivelAsync(
        int nivelId, string estado, DateTime fechaFin, long solicitudesExitosas, long solicitudesFallidas,
        string? detalle, IReadOnlyList<(int MetricaId, decimal Valor)> resultados, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var nivel = await context.NivelesEstres.FirstOrDefaultAsync(n => n.Id == nivelId, cancellationToken)
            ?? throw new InvalidOperationException($"No existe el nivel de estrés {nivelId}.");

        nivel.Estado = estado;
        nivel.FechaFin = fechaFin;
        nivel.SolicitudesExitosas = (int)solicitudesExitosas;
        nivel.SolicitudesFallidas = (int)solicitudesFallidas;
        nivel.Detalle = detalle;

        foreach (var (metricaId, valor) in resultados)
        {
            context.ResultadosMetricaNivelEstres.Add(new ResultadoMetricaNivelEstres
            {
                NivelEstresId = nivelId, MetricaId = metricaId, Valor = valor
            });
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task FinalizarEjecucionAsync(
        int ejecucionId, string estado, DateTime fechaFin,
        IReadOnlyList<(int MetricaId, decimal Valor)>? resumen, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var ejecucion = await context.Ejecuciones.FirstOrDefaultAsync(e => e.Id == ejecucionId, cancellationToken)
            ?? throw new InvalidOperationException($"No existe la ejecución {ejecucionId}.");

        ejecucion.Estado = estado;
        ejecucion.FechaFin = fechaFin;

        if (resumen is not null)
        {
            foreach (var (metricaId, valor) in resumen)
            {
                context.ResultadosMetrica.Add(new ResultadoMetrica
                {
                    EjecucionId = ejecucionId, MetricaId = metricaId, Valor = valor
                });
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
