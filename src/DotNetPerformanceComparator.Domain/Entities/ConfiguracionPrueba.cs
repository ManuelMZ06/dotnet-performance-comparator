namespace DotNetPerformanceComparator.Domain.Entities;

/// <summary>
/// Unión entre <see cref="ConfiguracionEvaluacion"/> y <see cref="TipoPrueba"/>.
/// Posee los parámetros técnicos específicos de ese tipo de prueba dentro de la evaluación.
/// </summary>
public class ConfiguracionPrueba
{
    public int ConfiguracionId { get; set; }
    public int TipoPruebaId { get; set; }

    public ConfiguracionEvaluacion Configuracion { get; set; } = null!;
    public TipoPrueba TipoPrueba { get; set; } = null!;
    public ICollection<ParametroEvaluacion> Parametros { get; set; } = new List<ParametroEvaluacion>();
}
