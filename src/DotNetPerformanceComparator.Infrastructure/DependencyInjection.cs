using DotNetPerformanceComparator.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetPerformanceComparator.Infrastructure;

/// <summary>
/// Registro de las implementaciones de Infraestructura (persistencia, herramientas externas, etc.).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("No se configuró la cadena de conexión 'DefaultConnection'.");
        }

        services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

        return services;
    }
}
