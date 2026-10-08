namespace DotNetPerformanceComparator.Application.Projects.Incorporation;

/// <summary>
/// Resultado de la lectura del archivo de proyecto.
/// </summary>
public enum ProjectFileStatus
{
    RutaVacia,
    RutaInvalida,
    ExtensionInvalida,
    NoExiste,
    EsEnlaceSimbolico,
    AccesoDenegado,
    NoLegible,
    ContenidoInvalido,
    Leido
}

/// <summary>
/// Hechos obtenidos al inspeccionar un <c>.csproj</c>, sin interpretar.
/// Las reglas de compatibilidad se aplican en <see cref="IncorporarProyectoService"/>.
/// </summary>
/// <param name="FileStatus">Resultado de localizar y leer el archivo.</param>
/// <param name="FullPath">Ruta absoluta normalizada del archivo, si la ruta era válida.</param>
/// <param name="ProjectName">Nombre del archivo sin extensión, si la ruta era válida.</param>
/// <param name="Sdks">SDKs MSBuild declarados (sin versión).</param>
/// <param name="TargetFrameworks">Valores de <c>TargetFramework</c>/<c>TargetFrameworks</c> declarados.</param>
/// <param name="KeyFolders">Carpetas relevantes existentes en la raíz del proyecto.</param>
/// <param name="DetectedFiles">Archivos relevantes existentes en la raíz del proyecto.</param>
public sealed record ProjectInspectionData(
    ProjectFileStatus FileStatus,
    string? FullPath,
    string? ProjectName,
    IReadOnlyList<string> Sdks,
    IReadOnlyList<string> TargetFrameworks,
    IReadOnlyList<string> KeyFolders,
    IReadOnlyList<string> DetectedFiles)
{
    public static ProjectInspectionData Failed(ProjectFileStatus status, string? fullPath = null, string? projectName = null) =>
        new(status, fullPath, projectName, [], [], [], []);
}
