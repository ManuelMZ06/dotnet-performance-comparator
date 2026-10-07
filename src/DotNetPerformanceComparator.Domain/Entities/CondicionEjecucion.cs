namespace DotNetPerformanceComparator.Domain.Entities;

public class CondicionEjecucion
{
    public int Id { get; set; }
    public int EjecucionId { get; set; }
    public string Clave { get; set; } = string.Empty;
    public string Valor { get; set; } = string.Empty;

    public Ejecucion Ejecucion { get; set; } = null!;
}
