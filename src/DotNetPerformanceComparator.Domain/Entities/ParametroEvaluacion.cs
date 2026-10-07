namespace DotNetPerformanceComparator.Domain.Entities;

public class ParametroEvaluacion
{
    public int Id { get; set; }
    public int ConfiguracionId { get; set; }
    public string Clave { get; set; } = string.Empty;
    public string Valor { get; set; } = string.Empty;

    public ConfiguracionEvaluacion Configuracion { get; set; } = null!;
}
