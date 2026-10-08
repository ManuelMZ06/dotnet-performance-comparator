namespace DotNetPerformanceComparator.Application.Projects.Incorporation;

/// <summary>
/// Inspecciona, en modo solo lectura, un archivo <c>.csproj</c> y su directorio.
/// No ejecuta, compila ni modifica el proyecto.
/// </summary>
public interface IProjectInspector
{
    Task<ProjectInspectionData> InspectAsync(string? csprojPath, CancellationToken cancellationToken = default);
}
