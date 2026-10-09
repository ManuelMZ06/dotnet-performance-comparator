namespace DotNetPerformanceComparator.Domain.Entities;

public class ConfiguracionEvaluacion
{
    public int Id { get; set; }
    public int ProyectoId { get; set; }
    public int MigracionId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int NumeroEjecuciones { get; set; }
    public DateTime FechaCreacion { get; set; }
    public string Estado { get; set; } = string.Empty;

    public ProyectoDotNet Proyecto { get; set; } = null!;
    public Migracion Migracion { get; set; } = null!;
    public ICollection<ConfiguracionPrueba> ConfiguracionesPrueba { get; set; } = new List<ConfiguracionPrueba>();
    public ICollection<ParametroEvaluacion> Parametros { get; set; } = new List<ParametroEvaluacion>();
    public ICollection<ConfiguracionMetrica> ConfiguracionesMetrica { get; set; } = new List<ConfiguracionMetrica>();
    public ICollection<Ejecucion> Ejecuciones { get; set; } = new List<Ejecucion>();
}
