namespace DotNetPerformanceComparator.Application.Migrations.ExperimentalMigration;

/// <param name="Updated">Se reemplazó al menos una entrada <c>net8.0</c>.</param>
/// <param name="TargetFrameworks">Frameworks declarados después del cambio.</param>
/// <param name="ErrorMessage">Causa comprensible si no pudo actualizarse.</param>
public sealed record TargetFrameworkUpdateResult(bool Updated, IReadOnlyList<string> TargetFrameworks, string? ErrorMessage = null);

/// <summary>
/// Operaciones sobre el XML del archivo de proyecto.
/// </summary>
public interface IProjectFileEditor
{
    /// <summary>
    /// Devuelve los <c>ProjectReference</c> que apuntan fuera de <paramref name="rootDirectory"/>
    /// (o que no pueden resolverse con seguridad). Solo lectura.
    /// </summary>
    IReadOnlyList<string> FindExternalProjectReferences(string csprojPath, string rootDirectory);

    /// <summary>
    /// Reemplaza <c>net8.0</c> por <c>net10.0</c> en <c>TargetFramework</c>/<c>TargetFrameworks</c>
    /// sin cambiar otros elementos. Debe usarse únicamente sobre la copia experimental.
    /// </summary>
    TargetFrameworkUpdateResult UpdateTargetFramework(string csprojPath);
}
