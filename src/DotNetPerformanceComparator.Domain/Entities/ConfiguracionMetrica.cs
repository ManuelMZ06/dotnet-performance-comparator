namespace DotNetPerformanceComparator.Domain.Entities;

/// <summary>
/// Unión entre <see cref="ConfiguracionEvaluacion"/> y <see cref="Metrica"/>.
/// </summary>
public class ConfiguracionMetrica
{
    public int ConfiguracionId { get; set; }
    public int MetricaId { get; set; }

    public ConfiguracionEvaluacion Configuracion { get; set; } = null!;
    public Metrica Metrica { get; set; } = null!;
}
