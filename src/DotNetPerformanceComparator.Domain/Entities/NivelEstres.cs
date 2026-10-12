namespace DotNetPerformanceComparator.Domain.Entities;

/// <summary>
/// Un escalón de una prueba de Estrés (C-07.3): una cantidad fija de usuarios concurrentes medida
/// durante una ventana, dentro de una misma <see cref="Ejecucion"/> (mismo proceso objetivo).
/// </summary>
public class NivelEstres
{
    public int Id { get; set; }
    public int EjecucionId { get; set; }
    public int NumeroNivel { get; set; }
    public int UsuariosObjetivo { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public string Estado { get; set; } = string.Empty;
    public int SolicitudesExitosas { get; set; }
    public int SolicitudesFallidas { get; set; }
    public string? Detalle { get; set; }

    public Ejecucion Ejecucion { get; set; } = null!;
    public ICollection<ResultadoMetricaNivelEstres> Resultados { get; set; } = new List<ResultadoMetricaNivelEstres>();
}
