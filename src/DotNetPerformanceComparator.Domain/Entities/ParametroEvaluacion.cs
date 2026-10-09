namespace DotNetPerformanceComparator.Domain.Entities;

/// <summary>
/// Parámetro técnico de un tipo de prueba dentro de una evaluación. Pertenece a una
/// <see cref="ConfiguracionPrueba"/> concreta mediante la clave compuesta (ConfiguracionId, TipoPruebaId),
/// de modo que cada parámetro queda asociado inequívocamente a su tipo de prueba.
/// </summary>
public class ParametroEvaluacion
{
    public int Id { get; set; }
    public int ConfiguracionId { get; set; }
    public int TipoPruebaId { get; set; }
    public string Clave { get; set; } = string.Empty;
    public string Valor { get; set; } = string.Empty;

    public ConfiguracionPrueba ConfiguracionPrueba { get; set; } = null!;
}
