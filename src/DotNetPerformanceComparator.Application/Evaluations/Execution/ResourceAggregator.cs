namespace DotNetPerformanceComparator.Application.Evaluations.Execution;

/// <summary>
/// Una muestra de recursos del proceso objetivo tomada durante la fase estable.
/// </summary>
/// <param name="CpuTotalMs">Tiempo total de CPU del proceso acumulado (<c>Process.TotalProcessorTime</c>) en ms.</param>
/// <param name="WorkingSetBytes">Memoria de trabajo (<c>Process.WorkingSet64</c>) en bytes.</param>
/// <param name="WallClockMs">Marca de reloj monótona de la muestra, en ms.</param>
public sealed record ResourceSample(double CpuTotalMs, long WorkingSetBytes, double WallClockMs);

/// <param name="CpuPorcentaje">Promedio de % de CPU del proceso durante la ventana (null si no hay intervalos válidos).</param>
/// <param name="MemoriaMb">Promedio de memoria de trabajo en MB (null si no hay muestras).</param>
public sealed record ResourceAggregates(decimal? CpuPorcentaje, int MuestrasCpu, decimal? MemoriaMb, int MuestrasRam);

/// <summary>
/// Calcula CPU y RAM promedio del proceso objetivo a partir de las muestras de la fase estable.
/// CPU por intervalo = ΔCPUms / (ΔWallMs · nºProcesadores) · 100.
/// </summary>
public static class ResourceAggregator
{
    private const long BytesPorMb = 1024 * 1024;

    /// <summary>
    /// Máximo físico de CPU (100 %) más una pequeña tolerancia por la granularidad del contador de CPU
    /// frente al reloj. Dentro de la tolerancia se ajusta a 100; por encima se considera muestra inválida.
    /// </summary>
    private const double MaximoCpuConTolerancia = 105.0;

    public static ResourceAggregates Aggregate(IReadOnlyList<ResourceSample> muestras, int processorCount)
    {
        if (processorCount <= 0)
        {
            processorCount = 1;
        }

        var cpuIntervalos = new List<double>();
        for (var i = 1; i < muestras.Count; i++)
        {
            var deltaWall = muestras[i].WallClockMs - muestras[i - 1].WallClockMs;
            var deltaCpu = muestras[i].CpuTotalMs - muestras[i - 1].CpuTotalMs;
            if (deltaWall <= 0 || deltaCpu < 0)
            {
                continue; // intervalo anómalo (reloj no avanzó o CPU retrocedió)
            }

            var porcentaje = deltaCpu / (deltaWall * processorCount) * 100.0;
            if (porcentaje > MaximoCpuConTolerancia)
            {
                // Exceso grande sobre el 100 % físico: se descarta en lugar de ocultarlo con un clamp.
                continue;
            }

            // Pequeña desviación sobre 100 % (granularidad del contador): se ajusta a 100.
            cpuIntervalos.Add(Math.Min(porcentaje, 100.0));
        }

        var ramValidas = muestras.Where(m => m.WorkingSetBytes > 0).Select(m => (double)m.WorkingSetBytes).ToList();

        decimal? cpu = cpuIntervalos.Count > 0 ? (decimal)cpuIntervalos.Average() : null;
        decimal? ram = ramValidas.Count > 0 ? (decimal)(ramValidas.Average() / BytesPorMb) : null;

        return new ResourceAggregates(cpu, cpuIntervalos.Count, ram, ramValidas.Count);
    }
}
