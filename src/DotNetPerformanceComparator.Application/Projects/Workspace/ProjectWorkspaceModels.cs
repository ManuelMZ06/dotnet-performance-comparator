namespace DotNetPerformanceComparator.Application.Projects.Workspace;

/// <summary>
/// Información transitoria del workspace de un proyecto: la raíz segura que debe copiarse, el proyecto de
/// entrada (ejecutable ASP.NET Core) y el grafo de <c>ProjectReference</c> locales resueltos. No se persiste;
/// se recalcula desde la ruta del .csproj cuando se necesita (BUG-01, ver D-52).
/// </summary>
/// <param name="WorkspaceRoot">Directorio raíz (contiene el entry y todos los proyectos referenciados).</param>
/// <param name="EntryProjectFullPath">Ruta absoluta del .csproj de entrada seleccionado por el Usuario.</param>
/// <param name="EntryProjectRelativePath">Ruta del entry relativa a <see cref="WorkspaceRoot"/>.</param>
/// <param name="ProjectFiles">Todos los .csproj del grafo (entry + referenciados), rutas absolutas.</param>
/// <param name="ReferencedProjectFiles">Los .csproj referenciados (sin el entry).</param>
public sealed record ProjectWorkspaceInfo(
    string WorkspaceRoot,
    string EntryProjectFullPath,
    string EntryProjectRelativePath,
    IReadOnlyList<string> ProjectFiles,
    IReadOnlyList<string> ReferencedProjectFiles);

/// <summary>Motivo por el que no pudo resolverse el workspace de forma segura.</summary>
public enum WorkspaceResolutionError
{
    None,
    EntryProjectNotFound,
    ReferenceNotFound,
    UnresolvableReference,
    NoSafeRoot
}

/// <summary>Resultado de resolver el workspace de un proyecto.</summary>
public sealed record WorkspaceResolution(ProjectWorkspaceInfo? Info, WorkspaceResolutionError Error, string? Message)
{
    public bool Success => Info is not null && Error == WorkspaceResolutionError.None;

    public static WorkspaceResolution Ok(ProjectWorkspaceInfo info) => new(info, WorkspaceResolutionError.None, null);

    public static WorkspaceResolution Fail(WorkspaceResolutionError error, string message) => new(null, error, message);
}

/// <summary>
/// Resuelve, a partir del .csproj de entrada, el grafo de <c>ProjectReference</c> locales y la raíz segura del
/// workspace que debe copiarse para migrar/preparar/ejecutar. Solo lectura; no modifica el proyecto original.
/// </summary>
public interface IProjectWorkspaceResolver
{
    WorkspaceResolution Resolve(string entryCsprojPath);
}
