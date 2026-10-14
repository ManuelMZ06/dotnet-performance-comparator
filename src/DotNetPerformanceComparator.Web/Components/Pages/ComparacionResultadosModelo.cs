using System.Globalization;
using DotNetPerformanceComparator.Application.Evaluations.Comparison;
using DotNetPerformanceComparator.Application.Evaluations.Results;
using DotNetPerformanceComparator.Domain.Constants;
using DotNetPerformanceComparator.Web.Components.Shared;

namespace DotNetPerformanceComparator.Web.Components.Pages;

/// <summary>
/// Estado y presentación de M-07 (CU-07, Comparar .NET 8 vs .NET 10). Obtiene el detalle con
/// <see cref="IConsultaResultadosService"/> (CU-05) y delega TODO el cálculo en
/// <see cref="IComparacionResultadosService"/> (no hay aritmética comparativa en Web). Solo decide qué
/// mostrar, aplica el enfoque de métrica recibido de CU-06 y formatea. Testeable sin framework de UI.
/// </summary>
public sealed class ComparacionResultadosModelo
{
    private const int TamanoMetricaId = 6; // Tamaño: valor constante por versión (no "muestras").

    private readonly IConsultaResultadosService _consulta;
    private readonly IComparacionResultadosService _comparacion;
    private readonly ILogger _logger;

    public ComparacionResultadosModelo(
        IConsultaResultadosService consulta, IComparacionResultadosService comparacion, ILogger logger)
    {
        _consulta = consulta;
        _comparacion = comparacion;
        _logger = logger;
    }

    public bool Cargando { get; private set; }
    public bool EvaluacionNoEncontrada { get; private set; }
    public EvaluacionDetalleConsulta? Detalle { get; private set; }

    /// <summary>Comparación de resumen (Carga/Concurrencia, o nivel máximo en Estrés). null hasta cargar.</summary>
    public ComparacionResumen? Resumen { get; private set; }

    /// <summary>Comparación específica de Estrés (resumen + niveles + contexto). null si no es Estrés.</summary>
    public ComparacionEstres? Estres { get; private set; }

    public bool EsEstres => Estres is not null;

    public string? Mensaje { get; private set; }
    public OperationMessageKind MensajeTipo { get; private set; }

    /// <summary>MetricaId del enfoque activo (recibido de CU-06); null = todas las métricas.</summary>
    public int? MetricaFiltroId { get; private set; }

    public bool FiltroActivo => MetricaFiltroId is not null;

    public IReadOnlyList<MetricaSeleccionadaConsulta> MetricasDisponibles => Detalle?.MetricasSeleccionadas ?? [];

    /// <summary>true cuando la evaluación existe pero no tiene ningún resultado persistido.</summary>
    public bool SinResultados => Detalle is { } d && !d.TieneResultados;

    public async Task CargarAsync(int configuracionId, int? metricaId = null, CancellationToken cancellationToken = default)
    {
        Cargando = true;
        EvaluacionNoEncontrada = false;
        Detalle = null;
        Resumen = null;
        Estres = null;
        MetricaFiltroId = null;
        Mensaje = null;

        try
        {
            var detalle = await _consulta.ObtenerDetalleAsync(configuracionId, cancellationToken);
            if (detalle is null)
            {
                EvaluacionNoEncontrada = true;
                return;
            }

            Detalle = detalle;
            if (detalle.TipoPrueba == TiposPruebaConocidos.Estres)
            {
                Estres = _comparacion.CompararEstres(detalle);
                Resumen = Estres.Resumen;
            }
            else
            {
                Resumen = _comparacion.CompararResumen(detalle);
            }

            AplicarFiltroMetrica(metricaId); // valida contra las métricas de la evaluación
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No fue posible calcular la comparación de la evaluación {ConfiguracionId}.", configuracionId);
            MensajeTipo = OperationMessageKind.Error;
            Mensaje = "No fue posible calcular la comparación de la evaluación.";
        }
        finally
        {
            Cargando = false;
        }
    }

    /// <summary>Aplica el enfoque de métrica. Un id que no pertenece a la evaluación se ignora (→ todas).</summary>
    public void AplicarFiltroMetrica(int? metricaId) =>
        MetricaFiltroId = metricaId is { } id && Detalle is { } d && d.MetricasSeleccionadas.Any(m => m.MetricaId == id)
            ? id
            : null;

    public void QuitarFiltroMetrica() => MetricaFiltroId = null;

    /// <summary>Filas visibles del resumen según el enfoque (solo presentación; no recalcula).</summary>
    public IReadOnlyList<ComparacionMetrica> MetricasVisibles(ComparacionResumen resumen) =>
        MetricaFiltroId is { } id ? resumen.Metricas.Where(m => m.MetricaId == id).ToList() : resumen.Metricas;

    /// <summary>Filas visibles de un nivel de estrés según el enfoque.</summary>
    public IReadOnlyList<ComparacionMetrica> MetricasVisibles(ComparacionNivelEstres nivel) =>
        MetricaFiltroId is { } id ? nivel.Metricas.Where(m => m.MetricaId == id).ToList() : nivel.Metricas;

    // ----------------------------------------------------------------- conclusión descriptiva (sin ganador)

    /// <summary>Conteos descriptivos por interpretación del resumen. No declara ganador ni pondera.</summary>
    public sealed record ConclusionComparacion(int Total, int Mejoras, int Empeoramientos, int Iguales, int NoComparables, string Texto);

    public ConclusionComparacion Conclusion()
    {
        var metricas = Resumen?.Metricas ?? [];
        var mejoras = metricas.Count(m => m.Interpretacion == InterpretacionComparacion.Mejora);
        var empeora = metricas.Count(m => m.Interpretacion == InterpretacionComparacion.Empeora);
        var iguales = metricas.Count(m => m.Interpretacion == InterpretacionComparacion.Igual);
        var noComp = metricas.Count(m => m.Interpretacion == InterpretacionComparacion.NoComparable);
        var total = metricas.Count;

        string texto;
        if (total == 0)
        {
            texto = "No hay métricas configuradas para comparar.";
        }
        else if (mejoras + empeora + iguales == 0)
        {
            texto = "No hay métricas comparables en el experimento.";
        }
        else
        {
            texto = $"En las métricas comparables del experimento, .NET 10 presentó mejora en {mejoras}, " +
                    $"empeoramiento en {empeora} e igualdad en {iguales}.";
        }

        if (noComp > 0)
        {
            texto += $" {noComp} no se pudo comparar.";
        }

        return new ConclusionComparacion(total, mejoras, empeora, iguales, noComp, texto);
    }

    // ----------------------------------------------------------------- contexto de saturación (texto)

    /// <summary>Texto descriptivo del contexto de saturación de una versión (no es una métrica ni capacidad).</summary>
    public static string ContextoMaximoTexto(ContextoSaturacionVersion contexto) =>
        contexto.MaxUsuariosCompletados is { } max
            ? $"Máximo nivel completado observado: {max} usuarios"
            : "No se completó ningún nivel.";

    public static string ContextoSaturacionTexto(ContextoSaturacionVersion contexto)
    {
        if (!contexto.Saturo)
        {
            return "No se observó saturación en los niveles ejecutados.";
        }

        var min = contexto.NivelSaturacionMinObservado;
        var max = contexto.NivelSaturacionMaxObservado;
        return min == max
            ? $"Saturación observada: {min} usuarios"
            : $"Saturación observada: entre {min} y {max} usuarios";
    }

    // ----------------------------------------------------------------- formato (presentación)

    private static string Numero(decimal valor) => valor.ToString("0.####", CultureInfo.InvariantCulture);

    public static string Valor(decimal? valor, string unidad)
    {
        if (valor is null)
        {
            return "—";
        }

        return string.IsNullOrEmpty(unidad) ? Numero(valor.Value) : $"{Numero(valor.Value)} {unidad}";
    }

    /// <summary>Diferencia absoluta con signo matemático conservado (nunca invertido) y unidad.</summary>
    public static string Diferencia(ComparacionMetrica metrica)
    {
        if (metrica.DiferenciaAbsoluta is not { } abs)
        {
            return "—";
        }

        var signo = abs > 0 ? "+" : string.Empty; // negativos ya traen "-"
        var numero = $"{signo}{Numero(abs)}";
        return string.IsNullOrEmpty(metrica.Unidad) ? numero : $"{numero} {metrica.Unidad}";
    }

    /// <summary>Variación porcentual con signo; null (base cero) → "No calculable". Nunca ∞/NaN/0 ficticio.</summary>
    public static string Porcentaje(decimal? porcentaje)
    {
        if (porcentaje is not { } pct)
        {
            return "No calculable";
        }

        var signo = pct > 0 ? "+" : string.Empty;
        return $"{signo}{pct.ToString("0.##", CultureInfo.InvariantCulture)} %";
    }

    public static string InterpretacionTexto(InterpretacionComparacion interpretacion) => interpretacion switch
    {
        InterpretacionComparacion.Mejora => "Mejora",
        InterpretacionComparacion.Empeora => "Empeora",
        InterpretacionComparacion.Igual => "Igual",
        _ => "No comparable"
    };

    /// <summary>Clase CSS para el badge de interpretación (refuerzo visual; el texto siempre está presente).</summary>
    public static string InterpretacionClase(InterpretacionComparacion interpretacion) => interpretacion switch
    {
        InterpretacionComparacion.Mejora => "mejora",
        InterpretacionComparacion.Empeora => "empeora",
        InterpretacionComparacion.Igual => "igual",
        _ => "nocomparable"
    };

    /// <summary>Transparencia metodológica de los pares; Tamaño es un valor constante, no varias muestras.</summary>
    public static string ParesTexto(ComparacionMetrica metrica)
    {
        if (metrica.MetricaId == TamanoMetricaId)
        {
            return metrica.Interpretacion == InterpretacionComparacion.NoComparable
                ? "valor constante no comparable"
                : "1 comparación (valor constante por versión)";
        }

        return metrica.ParesUtilizados switch
        {
            0 => "sin pares comparables",
            1 => "1 par comparable",
            var n => $"{n} pares comparables"
        };
    }

    public static string EstadoEjecucionTexto(string estado) => estado switch
    {
        "EnProceso" => "En proceso",
        "Completada" => "Completada",
        "CompletadaConSaturacion" => "Completada con saturación",
        "Fallida" => "Fallida",
        "Cancelada" => "Cancelada",
        _ => estado
    };
}
