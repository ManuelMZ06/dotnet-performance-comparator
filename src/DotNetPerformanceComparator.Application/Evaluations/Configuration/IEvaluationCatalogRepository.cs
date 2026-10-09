using DotNetPerformanceComparator.Domain.Entities;

namespace DotNetPerformanceComparator.Application.Evaluations.Configuration;

/// <summary>
/// Consultas de catálogo y de verificación necesarias para CU-03 (solo lectura).
/// </summary>
public interface IEvaluationCatalogRepository
{
    Task<IReadOnlyList<ProyectoDotNet>> GetProyectosAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Migracion>> GetMigracionesExitosasAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TipoPrueba>> GetTiposPruebaAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Metrica>> GetMetricasAsync(CancellationToken cancellationToken = default);

    /// <summary>Migración por Id, o null si no existe (para verificar al guardar).</summary>
    Task<Migracion?> GetMigracionByIdAsync(int id, CancellationToken cancellationToken = default);
}
