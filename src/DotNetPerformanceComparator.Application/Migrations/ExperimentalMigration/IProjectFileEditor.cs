namespace DotNetPerformanceComparator.Application.Migrations.ExperimentalMigration;

/// <param name="Updated">Se reemplazó al menos una entrada <c>net8.0</c>.</param>
/// <param name="TargetFrameworks">Frameworks declarados después del cambio.</param>
/// <param name="ErrorMessage">Causa comprensible si no pudo actualizarse.</param>
public sealed record TargetFrameworkUpdateResult(bool Updated, IReadOnlyList<string> TargetFrameworks, string? ErrorMessage = null);

/// <summary>
/// Análisis del framework objetivo de un proyecto, previo a decidir si debe migrarse (BUG-02).
/// </summary>
/// <param name="Resoluble">
/// true si el <c>TargetFramework</c>/<c>TargetFrameworks</c> pudo determinarse de forma segura (literal,
/// sin expresiones MSBuild ni herencia). false exige detener la migración del grafo (no adivinar).
/// </param>
/// <param name="RequiresChange">true si contiene el token exacto <c>net8.0</c> (debe migrarse a net10.0).</param>
/// <param name="FrameworksOriginales">TFM declarados actualmente.</param>
/// <param name="FrameworksDestino">TFM tras aplicar net8.0→net10.0 (igual a los originales si no cambia).</param>
/// <param name="Motivo">Causa de no ser resoluble (expresión, herencia/no SDK-style, lectura, etc.).</param>
public sealed record TargetFrameworkAnalysis(
    bool Resoluble,
    bool RequiresChange,
    IReadOnlyList<string> FrameworksOriginales,
    IReadOnlyList<string> FrameworksDestino,
    string? Motivo = null);

/// <summary>
/// Operaciones sobre el XML del archivo de proyecto.
/// </summary>
public interface IProjectFileEditor
{
    /// <summary>
    /// Analiza el <c>TargetFramework</c>/<c>TargetFrameworks</c> de un proyecto sin modificarlo: clasifica si
    /// targetea net8.0 (migrable), otro TFM/netstandard (se conserva) o si no puede determinarse con seguridad
    /// (expresión MSBuild, herencia, no SDK-style). Solo lectura.
    /// </summary>
    TargetFrameworkAnalysis AnalyzeTargetFramework(string csprojPath);

    /// <summary>
    /// Reemplaza <c>net8.0</c> por <c>net10.0</c> en <c>TargetFramework</c>/<c>TargetFrameworks</c>
    /// sin cambiar otros elementos. Debe usarse únicamente sobre la copia experimental.
    /// </summary>
    TargetFrameworkUpdateResult UpdateTargetFramework(string csprojPath);
}
