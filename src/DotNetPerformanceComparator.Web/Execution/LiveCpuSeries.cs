using DotNetPerformanceComparator.Web.Components.Shared.Charts;

namespace DotNetPerformanceComparator.Web.Execution;

/// <summary>
/// Acumula los puntos de CPU en vivo de la <c>Ejecucion</c> actual para el <c>LineChart</c> (C-07.5).
/// Se reinicia cuando cambia la versión o el número de ejecución (nueva <c>Ejecucion</c>); NO se reinicia
/// por cambio de nivel de estrés o de ronda (siguen siendo la misma <c>Ejecucion</c>). Serie transitoria:
/// nunca se persiste ni recalcula el valor oficial. No es thread-safe por sí misma; el llamador sincroniza.
/// </summary>
public sealed class LiveCpuSeries
{
    private readonly List<LineChartPoint> _puntos = [];
    private string? _version;
    private int? _numeroEjecucion;

    public void Observar(string? version, int? numeroEjecucion, decimal cpu)
    {
        if (version != _version || numeroEjecucion != _numeroEjecucion)
        {
            _version = version;
            _numeroEjecucion = numeroEjecucion;
            _puntos.Clear();
        }

        _puntos.Add(new LineChartPoint(_puntos.Count, (double)cpu));
    }

    public IReadOnlyList<LineChartPoint> Snapshot() => _puntos.ToArray();

    public void Reiniciar()
    {
        _puntos.Clear();
        _version = null;
        _numeroEjecucion = null;
    }
}
