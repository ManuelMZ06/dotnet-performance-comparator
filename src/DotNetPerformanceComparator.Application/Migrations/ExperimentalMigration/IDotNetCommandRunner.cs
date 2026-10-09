namespace DotNetPerformanceComparator.Application.Migrations.ExperimentalMigration;

/// <param name="Started">El proceso pudo iniciarse.</param>
/// <param name="ExitCode">Código de salida (solo si <paramref name="Started"/>).</param>
/// <param name="Output">Líneas de stdout y stderr en orden de llegada.</param>
public sealed record DotNetCommandResult(bool Started, int ExitCode, IReadOnlyList<string> Output)
{
    public bool Succeeded => Started && ExitCode == 0;
}

/// <summary>
/// Ejecuta el host <c>dotnet</c> sin shell, con argumentos separados (nunca concatenados).
/// </summary>
public interface IDotNetCommandRunner
{
    /// <param name="arguments">Argumentos de <c>dotnet</c>; cada ruta es un argumento independiente.</param>
    /// <param name="sdkVersion">Versión exacta del SDK a utilizar (aislada del <c>global.json</c> de la herramienta); null para no fijarla.</param>
    /// <param name="onOutputLine">Recibe cada línea de salida a medida que se produce.</param>
    Task<DotNetCommandResult> RunAsync(
        IReadOnlyList<string> arguments,
        string? sdkVersion,
        Action<string>? onOutputLine = null,
        CancellationToken cancellationToken = default);
}
