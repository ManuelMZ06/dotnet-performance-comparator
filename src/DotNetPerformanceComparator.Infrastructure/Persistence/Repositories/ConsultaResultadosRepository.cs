using DotNetPerformanceComparator.Application.Evaluations.Results;
using DotNetPerformanceComparator.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DotNetPerformanceComparator.Infrastructure.Persistence.Repositories;

/// <summary>
/// Lectura de CU-05 con contextos de vida corta y <c>AsNoTracking</c>. El listado se resuelve por proyección
/// (sin cargar grafos ni N+1); el detalle carga el grafo de una sola configuración y lo mapea a modelos de
/// lectura. Solo lee lo persistido: no recalcula métricas, no promedia niveles y no inventa resúmenes.
/// </summary>
public sealed class ConsultaResultadosRepository : IConsultaResultadosRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public ConsultaResultadosRepository(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<EvaluacionResumenConsulta>> ListarAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        // Orden determinista: más recientes primero (FechaCreacion desc, Id desc como desempate).
        // TieneResultados se evalúa como EXISTS en SQL, sin materializar ejecuciones/resultados.
        return await context.ConfiguracionesEvaluacion
            .AsNoTracking()
            .OrderByDescending(c => c.FechaCreacion)
            .ThenByDescending(c => c.Id)
            .Select(c => new EvaluacionResumenConsulta(
                c.Id,
                c.Nombre,
                c.Proyecto.Nombre,
                c.TipoPrueba.Nombre,
                c.Estado,
                c.NumeroEjecuciones,
                c.FechaCreacion,
                c.Ejecuciones.Any(e => e.Resultados.Any())
                    || c.Ejecuciones.Any(e => e.Niveles.Any(n => n.Resultados.Any()))))
            .ToListAsync(cancellationToken);
    }

    public async Task<EvaluacionDetalleConsulta?> ObtenerDetalleAsync(int configuracionId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var config = await context.ConfiguracionesEvaluacion
            .AsNoTracking()
            .AsSplitQuery()
            .Include(c => c.Proyecto)
            .Include(c => c.Migracion)
            .Include(c => c.TipoPrueba)
            .Include(c => c.ConfiguracionesMetrica).ThenInclude(cm => cm.Metrica)
            .Include(c => c.Ejecuciones).ThenInclude(e => e.Resultados).ThenInclude(r => r.Metrica)
            .Include(c => c.Ejecuciones).ThenInclude(e => e.Condiciones)
            .Include(c => c.Ejecuciones).ThenInclude(e => e.Niveles).ThenInclude(n => n.Resultados).ThenInclude(r => r.Metrica)
            .FirstOrDefaultAsync(c => c.Id == configuracionId, cancellationToken);

        if (config is null)
        {
            return null;
        }

        var metricas = config.ConfiguracionesMetrica
            .Select(cm => new MetricaSeleccionadaConsulta(cm.MetricaId, cm.Metrica.Nombre, cm.Metrica.Unidad))
            .ToList();

        var ejecuciones = config.Ejecuciones
            .OrderBy(e => e.Id)
            .Select(MapEjecucion)
            .ToList();

        var migracion = config.Migracion is { } m
            ? $"{m.VersionOrigen} → {m.VersionDestino} ({m.Estado})"
            : null;

        return new EvaluacionDetalleConsulta(
            config.Id, config.Nombre, config.Proyecto.Nombre, migracion, config.TipoPrueba.Nombre,
            config.Estado, config.NumeroEjecuciones, config.RutaObjetivo, config.Observaciones, config.FechaCreacion,
            metricas, ejecuciones);
    }

    private static EjecucionConsulta MapEjecucion(Ejecucion e) => new(
        e.VersionDotNet,
        e.NumeroEjecucion,
        e.Estado,
        e.FechaInicio,
        e.FechaFin,
        e.Condiciones.Select(c => new CondicionConsulta(c.Clave, c.Valor)).ToList(),
        e.Resultados.Select(MapResultado).ToList(),
        e.Niveles.OrderBy(n => n.NumeroNivel).Select(MapNivel).ToList());

    private static NivelEstresConsulta MapNivel(NivelEstres n) => new(
        n.NumeroNivel,
        n.UsuariosObjetivo,
        n.Estado,
        n.SolicitudesExitosas,
        n.SolicitudesFallidas,
        n.Detalle,
        n.Resultados.Select(MapResultado).ToList());

    private static ResultadoMetricaConsulta MapResultado(ResultadoMetrica r) =>
        new(r.MetricaId, r.Metrica.Nombre, r.Metrica.Unidad, r.Valor);

    private static ResultadoMetricaConsulta MapResultado(ResultadoMetricaNivelEstres r) =>
        new(r.MetricaId, r.Metrica.Nombre, r.Metrica.Unidad, r.Valor);
}
