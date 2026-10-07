namespace DotNetPerformanceComparator.Domain.Entities;

public class Ejecucion
{
    public int Id { get; set; }
    public int ConfiguracionId { get; set; }
    public string VersionDotNet { get; set; } = string.Empty;
    public int NumeroEjecucion { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public string Estado { get; set; } = string.Empty;

    public ConfiguracionEvaluacion Configuracion { get; set; } = null!;
    public ICollection<CondicionEjecucion> Condiciones { get; set; } = new List<CondicionEjecucion>();
    public ICollection<ResultadoMetrica> Resultados { get; set; } = new List<ResultadoMetrica>();
}
