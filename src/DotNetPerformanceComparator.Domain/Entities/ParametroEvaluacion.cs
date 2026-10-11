namespace DotNetPerformanceComparator.Domain.Entities;

/// <summary>
/// Parámetro técnico de una evaluación. Pertenece a una <see cref="ConfiguracionEvaluacion"/>, cuyo
/// <see cref="ConfiguracionEvaluacion.TipoPrueba"/> determina inequívocamente el tipo del parámetro.
/// </summary>
public class ParametroEvaluacion
{
    public int Id { get; set; }
    public int ConfiguracionId { get; set; }
    public string Clave { get; set; } = string.Empty;
    public string Valor { get; set; } = string.Empty;

    public ConfiguracionEvaluacion Configuracion { get; set; } = null!;
}
