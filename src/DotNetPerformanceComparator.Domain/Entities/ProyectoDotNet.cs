namespace DotNetPerformanceComparator.Domain.Entities;

public class ProyectoDotNet
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string RutaOriginal { get; set; } = string.Empty;
    public string VersionBase { get; set; } = string.Empty;
    public string EstadoValidacion { get; set; } = string.Empty;
    public DateTime FechaRegistro { get; set; }

    public ICollection<Migracion> Migraciones { get; set; } = new List<Migracion>();
    public ICollection<ConfiguracionEvaluacion> ConfiguracionesEvaluacion { get; set; } = new List<ConfiguracionEvaluacion>();
}
