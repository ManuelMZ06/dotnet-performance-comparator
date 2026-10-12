namespace DotNetPerformanceComparator.Domain.Entities;

/// <summary>
/// Valor de una métrica variable por presión (CPU, RAM, Tiempo de respuesta, Latencia, Throughput)
/// para un <see cref="NivelEstres"/>. El tamaño de aplicación NO se guarda por nivel (no depende de los
/// usuarios); permanece a nivel de <see cref="Ejecucion"/> en <see cref="ResultadoMetrica"/>.
/// </summary>
public class ResultadoMetricaNivelEstres
{
    public int Id { get; set; }
    public int NivelEstresId { get; set; }
    public int MetricaId { get; set; }
    public decimal Valor { get; set; }

    public NivelEstres NivelEstres { get; set; } = null!;
    public Metrica Metrica { get; set; } = null!;
}
