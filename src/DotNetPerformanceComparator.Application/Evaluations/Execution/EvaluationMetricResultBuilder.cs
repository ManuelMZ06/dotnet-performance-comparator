using DotNetPerformanceComparator.Domain.Constants;
using DotNetPerformanceComparator.Domain.Entities;

namespace DotNetPerformanceComparator.Application.Evaluations.Execution;

/// <summary>Valores crudos de una ventana estable con los que se construyen los <c>ResultadoMetrica</c>.</summary>
/// <param name="CpuPorcentaje">CPU promedio (%) del agregador; null si no hubo intervalos válidos.</param>
/// <param name="MemoriaMb">RAM promedio (MB) del agregador; null si no hubo muestras.</param>
/// <param name="TiempoRespuestaMedioMs">Mean de latencia OK del Step "request".</param>
/// <param name="LatenciaP95Ms">P95 de latencia OK del Step "request".</param>
/// <param name="ThroughputRps">RPS de solicitudes OK del Step "request".</param>
/// <param name="PublishedSizeBytes">Tamaño del publish congelado para la versión.</param>
public sealed record MetricInputs(
    decimal? CpuPorcentaje,
    decimal? MemoriaMb,
    double TiempoRespuestaMedioMs,
    double LatenciaP95Ms,
    double ThroughputRps,
    long PublishedSizeBytes);

/// <summary>
/// Construye los <c>ResultadoMetrica</c> para las métricas seleccionadas, mapeando cada nombre del
/// catálogo a su valor y validando que sea finito y semánticamente válido. Clase pura (sin NBomber ni
/// infraestructura) para fijar y probar la semántica de C-07.1 (CPU/RAM/Tiempo de respuesta) y C-07.2
/// (Latencia P95, Throughput RPS, Tamaño). Si una métrica seleccionada no puede obtenerse de forma válida,
/// <see cref="Build"/> devuelve su nombre en <c>Faltante</c> y ningún resultado (la ejecución será Fallida,
/// sin resultados parciales).
/// </summary>
public static class EvaluationMetricResultBuilder
{
    private const long BytesPorMb = 1024L * 1024L;

    public static (List<(int MetricaId, decimal Valor)> Resultados, string? Faltante) Build(
        IReadOnlyList<Metrica> metricasSeleccionadas, MetricInputs inputs)
    {
        var resultados = new List<(int, decimal)>();
        foreach (var metrica in metricasSeleccionadas)
        {
            var valor = ValorDe(metrica.Nombre, inputs);
            if (valor is null)
            {
                return ([], metrica.Nombre);
            }

            resultados.Add((metrica.Id, valor.Value));
        }

        return (resultados, null);
    }

    /// <summary>
    /// Construye solo los resultados válidos de las métricas seleccionadas, omitiendo (sin fallar) las que
    /// no tengan un valor válido. Se usa para persistir las métricas parciales confiables (p. ej. CPU/RAM)
    /// de un nivel de Estrés saturado por 0 solicitudes exitosas, sin inventar Mean/P95/RPS.
    /// </summary>
    public static List<(int MetricaId, decimal Valor)> BuildBestEffort(
        IReadOnlyList<Metrica> metricasSeleccionadas, MetricInputs inputs)
    {
        var resultados = new List<(int, decimal)>();
        foreach (var metrica in metricasSeleccionadas)
        {
            if (ValorDe(metrica.Nombre, inputs) is { } valor)
            {
                resultados.Add((metrica.Id, valor));
            }
        }

        return resultados;
    }

    private static decimal? ValorDe(string nombre, MetricInputs i) => nombre switch
    {
        NombresMetrica.Cpu => i.CpuPorcentaje,
        NombresMetrica.MemoriaRam => i.MemoriaMb,
        NombresMetrica.TiempoRespuesta => DesdeDouble(i.TiempoRespuestaMedioMs, requiereEstrictamentePositivo: false),
        NombresMetrica.Latencia => DesdeDouble(i.LatenciaP95Ms, requiereEstrictamentePositivo: false),
        NombresMetrica.Throughput => DesdeDouble(i.ThroughputRps, requiereEstrictamentePositivo: true),
        NombresMetrica.TamanoAplicacion => DesdeTamano(i.PublishedSizeBytes),
        _ => null
    };

    /// <summary>Convierte un valor HTTP (ms o req/s) a decimal si es finito y válido; null si es NaN/Infinity o fuera de rango.</summary>
    private static decimal? DesdeDouble(double valor, bool requiereEstrictamentePositivo)
    {
        if (!double.IsFinite(valor))
        {
            return null; // NaN / Infinity: dato inexistente, no se inventa 0/-1.
        }

        if (requiereEstrictamentePositivo ? valor <= 0 : valor < 0)
        {
            return null;
        }

        if (valor > (double)decimal.MaxValue)
        {
            return null; // estadística incoherente fuera del rango decimal.
        }

        return (decimal)valor;
    }

    /// <summary>Tamaño del publish a MB; null si es 0 o negativo (un publish válido siempre produce archivos).</summary>
    private static decimal? DesdeTamano(long bytes) => bytes > 0 ? (decimal)bytes / BytesPorMb : null;
}
