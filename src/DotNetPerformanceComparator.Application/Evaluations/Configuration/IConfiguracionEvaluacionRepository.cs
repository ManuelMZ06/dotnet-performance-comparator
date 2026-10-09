using DotNetPerformanceComparator.Domain.Entities;

namespace DotNetPerformanceComparator.Application.Evaluations.Configuration;

/// <summary>
/// Persistencia de <see cref="ConfiguracionEvaluacion"/> requerida por CU-03.
/// </summary>
public interface IConfiguracionEvaluacionRepository
{
    /// <summary>
    /// Guarda la configuración junto con sus pruebas, métricas y parámetros en una sola operación.
    /// </summary>
    Task AddAsync(ConfiguracionEvaluacion configuracion, CancellationToken cancellationToken = default);
}
