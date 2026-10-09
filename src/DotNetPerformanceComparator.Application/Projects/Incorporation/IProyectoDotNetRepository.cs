using DotNetPerformanceComparator.Domain.Entities;

namespace DotNetPerformanceComparator.Application.Projects.Incorporation;

/// <summary>
/// Persistencia de <see cref="ProyectoDotNet"/> requerida por CU-01 (registro) y CU-02 (consulta).
/// </summary>
public interface IProyectoDotNetRepository
{
    Task AddAsync(ProyectoDotNet proyecto, CancellationToken cancellationToken = default);

    /// <summary>Proyectos registrados ordenados por nombre (sin seguimiento de cambios).</summary>
    Task<IReadOnlyList<ProyectoDotNet>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<ProyectoDotNet?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}
