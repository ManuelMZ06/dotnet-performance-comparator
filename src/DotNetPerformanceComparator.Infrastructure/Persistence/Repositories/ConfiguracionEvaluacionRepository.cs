using DotNetPerformanceComparator.Application.Evaluations.Configuration;
using DotNetPerformanceComparator.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DotNetPerformanceComparator.Infrastructure.Persistence.Repositories;

/// <summary>
/// Usa un <see cref="AppDbContext"/> de vida corta por operación (adecuado para Blazor Server).
/// </summary>
public sealed class ConfiguracionEvaluacionRepository : IConfiguracionEvaluacionRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public ConfiguracionEvaluacionRepository(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task AddAsync(ConfiguracionEvaluacion configuracion, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        // Agregar la raíz añade también sus pruebas, métricas y parámetros en un solo SaveChanges.
        context.ConfiguracionesEvaluacion.Add(configuracion);
        await context.SaveChangesAsync(cancellationToken);
    }
}
