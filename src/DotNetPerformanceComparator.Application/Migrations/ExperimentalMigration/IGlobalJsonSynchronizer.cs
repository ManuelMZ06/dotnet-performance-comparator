namespace DotNetPerformanceComparator.Application.Migrations.ExperimentalMigration;

/// <summary>Qué ocurrió con el <c>global.json</c> de la copia experimental.</summary>
public enum GlobalJsonSyncOutcome
{
    Created,
    Updated,
    Failed
}

/// <param name="Outcome">Creado, actualizado o fallido.</param>
/// <param name="Error">Causa comprensible si no pudo sincronizarse (p. ej. JSON mal formado).</param>
public sealed record GlobalJsonSyncResult(GlobalJsonSyncOutcome Outcome, string? Error = null);

/// <summary>
/// Sincroniza el <c>global.json</c> de la raíz del workspace experimental con el SDK .NET 10 ya seleccionado
/// por CU-02 (BUG-03). Se crea si no existe y se actualiza la sección <c>sdk</c> si existe, preservando las
/// demás propiedades. Solo actúa sobre la COPIA; nunca sobre el original.
/// </summary>
public interface IGlobalJsonSynchronizer
{
    GlobalJsonSyncResult Synchronize(string copyWorkspaceRoot, string sdkVersion);
}
