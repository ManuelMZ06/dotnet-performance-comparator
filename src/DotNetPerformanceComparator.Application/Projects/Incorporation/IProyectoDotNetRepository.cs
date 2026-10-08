using DotNetPerformanceComparator.Domain.Entities;

namespace DotNetPerformanceComparator.Application.Projects.Incorporation;

/// <summary>
/// Persistencia de <see cref="ProyectoDotNet"/> requerida por CU-01.
/// </summary>
public interface IProyectoDotNetRepository
{
    Task AddAsync(ProyectoDotNet proyecto, CancellationToken cancellationToken = default);
}
