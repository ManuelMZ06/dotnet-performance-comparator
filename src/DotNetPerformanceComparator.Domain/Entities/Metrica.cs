namespace DotNetPerformanceComparator.Domain.Entities;

/// <summary>
/// Catálogo de métricas con su unidad de almacenamiento/presentación.
/// </summary>
public class Metrica
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Unidad { get; set; } = string.Empty;
    public string? Descripcion { get; set; }

    public ICollection<ConfiguracionMetrica> ConfiguracionesMetrica { get; set; } = new List<ConfiguracionMetrica>();
    public ICollection<ResultadoMetrica> Resultados { get; set; } = new List<ResultadoMetrica>();
}
