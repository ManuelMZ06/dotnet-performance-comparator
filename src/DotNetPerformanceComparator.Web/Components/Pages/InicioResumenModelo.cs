using DotNetPerformanceComparator.Application.Evaluations.Comparison;
using DotNetPerformanceComparator.Application.Evaluations.Results;
using DotNetPerformanceComparator.Web.Components.Shared.Charts;

namespace DotNetPerformanceComparator.Web.Components.Pages;

/// <summary>
/// Estado del panel "Resumen comparativo reciente" de M-01 (CU-08). Muestra la evaluación MÁS RECIENTE con
/// una comparación de RESUMEN válida (al menos una métrica graficable), como small multiples. Reutiliza
/// <see cref="IConsultaResultadosService"/> (datos) e <see cref="IComparacionResultadosService"/> (comparación);
/// Web no calcula ni accede a EF. Solo resumen: nunca niveles de Estrés ni saturación en Inicio.
/// </summary>
public sealed class InicioResumenModelo
{
    private readonly IConsultaResultadosService _consulta;
    private readonly IComparacionResultadosService _comparacion;
    private readonly ILogger _logger;

    public InicioResumenModelo(
        IConsultaResultadosService consulta, IComparacionResultadosService comparacion, ILogger logger)
    {
        _consulta = consulta;
        _comparacion = comparacion;
        _logger = logger;
    }

    public bool Cargando { get; private set; }
    public bool Error { get; private set; }

    /// <summary>La evaluación reciente elegida (para el contexto: nombre, proyecto, tipo, fecha, Id).</summary>
    public EvaluacionResumenConsulta? Evaluacion { get; private set; }

    /// <summary>Small multiples de la comparación de resumen; vacío si no hay nada graficable.</summary>
    public IReadOnlyList<BarChartGroup> Graficos { get; private set; } = [];

    public bool HayResumen => Evaluacion is not null && Graficos.Count > 0;

    public async Task CargarAsync(CancellationToken cancellationToken = default)
    {
        Cargando = true;
        Error = false;
        Evaluacion = null;
        Graficos = [];

        try
        {
            // Listado ya ordenado por FechaCreacion desc, Id desc (coherente con CU-05). Se recorren solo las
            // evaluaciones con resultados y se toma la PRIMERA cuyo resumen tenga alguna métrica graficable.
            // Implicación: puede inspeccionarse el detalle de unas pocas evaluaciones recientes hasta hallar
            // una comparable; en la práctica la más reciente con resultados suele serlo (sin patrón N+1 masivo).
            var evaluaciones = await _consulta.ObtenerEvaluacionesAsync(cancellationToken);
            foreach (var ev in evaluaciones.Where(e => e.TieneResultados))
            {
                var detalle = await _consulta.ObtenerDetalleAsync(ev.Id, cancellationToken);
                if (detalle is null)
                {
                    continue;
                }

                var resumen = _comparacion.CompararResumen(detalle);
                var grupos = ComparacionChartMapper.ResumenAGrupos(resumen.Metricas);
                if (grupos.Count > 0)
                {
                    Evaluacion = ev;
                    Graficos = grupos;
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No fue posible preparar el resumen comparativo reciente de Inicio.");
            Error = true;
        }
        finally
        {
            Cargando = false;
        }
    }
}
