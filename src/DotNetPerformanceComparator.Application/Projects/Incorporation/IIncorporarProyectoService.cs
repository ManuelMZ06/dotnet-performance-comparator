namespace DotNetPerformanceComparator.Application.Projects.Incorporation;

/// <summary>
/// CU-01 — Incorporar aplicación .NET 8 (RF-01, RF-14).
/// </summary>
public interface IIncorporarProyectoService
{
    /// <summary>Inspecciona el proyecto indicado y evalúa si puede incorporarse.</summary>
    Task<ProjectInspectionResult> InspeccionarAsync(string? rutaProyecto, CancellationToken cancellationToken = default);

    /// <summary>Vuelve a inspeccionar el proyecto y, solo si es válido, lo registra.</summary>
    Task<IncorporarProyectoResult> RegistrarAsync(IncorporarProyectoRequest request, CancellationToken cancellationToken = default);
}
