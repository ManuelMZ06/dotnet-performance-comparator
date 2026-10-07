namespace DotNetPerformanceComparator.Domain.Entities;

public class Migracion
{
    public int Id { get; set; }
    public int ProyectoId { get; set; }
    public string VersionOrigen { get; set; } = string.Empty;
    public string VersionDestino { get; set; } = string.Empty;
    public string? RutaMigrada { get; set; }
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public string? Detalle { get; set; }

    public ProyectoDotNet Proyecto { get; set; } = null!;
}
