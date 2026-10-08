using DotNetPerformanceComparator.Application.Projects.Incorporation;
using DotNetPerformanceComparator.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DotNetPerformanceComparator.Infrastructure.Persistence.Repositories;

/// <summary>
/// Usa un <see cref="AppDbContext"/> de vida corta por operación (adecuado para Blazor Server).
/// </summary>
public sealed class ProyectoDotNetRepository : IProyectoDotNetRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public ProyectoDotNetRepository(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task AddAsync(ProyectoDotNet proyecto, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        context.ProyectosDotNet.Add(proyecto);
        await context.SaveChangesAsync(cancellationToken);
    }
}
