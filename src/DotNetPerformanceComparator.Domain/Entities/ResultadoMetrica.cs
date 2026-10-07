namespace DotNetPerformanceComparator.Domain.Entities;

public class ResultadoMetrica
{
    public int Id { get; set; }
    public int EjecucionId { get; set; }
    public int MetricaId { get; set; }
    public decimal Valor { get; set; }

    public Ejecucion Ejecucion { get; set; } = null!;
    public Metrica Metrica { get; set; } = null!;
}
