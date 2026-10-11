namespace DotNetPerformanceComparator.Domain.Entities;

/// <summary>
/// Catálogo de tipos de prueba (Carga, Estrés, Concurrencia).
/// </summary>
public class TipoPrueba
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }

    public ICollection<ConfiguracionEvaluacion> ConfiguracionesEvaluacion { get; set; } = new List<ConfiguracionEvaluacion>();
}
