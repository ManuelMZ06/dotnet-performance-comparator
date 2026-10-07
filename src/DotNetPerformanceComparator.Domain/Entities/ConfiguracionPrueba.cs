namespace DotNetPerformanceComparator.Domain.Entities;

/// <summary>
/// Unión entre <see cref="ConfiguracionEvaluacion"/> y <see cref="TipoPrueba"/>.
/// </summary>
public class ConfiguracionPrueba
{
    public int ConfiguracionId { get; set; }
    public int TipoPruebaId { get; set; }

    public ConfiguracionEvaluacion Configuracion { get; set; } = null!;
    public TipoPrueba TipoPrueba { get; set; } = null!;
}
