namespace DotNetPerformanceComparator.Application.Migrations.ExperimentalMigration;

/// <summary>
/// CU-02 — Migrar aplicación experimentalmente a .NET 10 (RF-02, RF-14).
/// </summary>
public interface IMigrarProyectoService
{
    /// <summary>Proyectos registrados (CU-01) disponibles como base.</summary>
    Task<IReadOnlyList<ProyectoResumen>> ObtenerProyectosAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Valida, copia, actualiza el framework de la copia, restaura y compila con el SDK .NET 10.
    /// El proyecto original nunca se modifica.
    /// </summary>
    Task<MigrarProyectoResult> MigrarAsync(
        MigrarProyectoRequest request,
        IProgress<MigrationProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
