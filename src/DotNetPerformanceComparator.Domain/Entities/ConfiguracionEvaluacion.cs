namespace DotNetPerformanceComparator.Domain.Entities;

public class ConfiguracionEvaluacion
{
    public int Id { get; set; }
    public int ProyectoId { get; set; }
    public int MigracionId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int NumeroEjecuciones { get; set; }

    /// <summary>Ruta HTTP relativa que CU-04 solicitará (p. ej. <c>/api/productos</c>).</summary>
    public string RutaObjetivo { get; set; } = string.Empty;

    /// <summary>Notas sobre la evaluación completa; no es un parámetro de prueba.</summary>
    public string? Observaciones { get; set; }

    public DateTime FechaCreacion { get; set; }
    public string Estado { get; set; } = string.Empty;

    public ProyectoDotNet Proyecto { get; set; } = null!;
    public Migracion Migracion { get; set; } = null!;
    public ICollection<ConfiguracionPrueba> ConfiguracionesPrueba { get; set; } = new List<ConfiguracionPrueba>();
    public ICollection<ConfiguracionMetrica> ConfiguracionesMetrica { get; set; } = new List<ConfiguracionMetrica>();
    public ICollection<Ejecucion> Ejecuciones { get; set; } = new List<Ejecucion>();
}
