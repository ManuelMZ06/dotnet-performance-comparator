using Microsoft.Extensions.DependencyInjection;

namespace DotNetPerformanceComparator.Infrastructure;

/// <summary>
/// Registro de las implementaciones de Infraestructura (persistencia, herramientas externas, etc.).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Las implementaciones de las abstracciones de Aplicación se registrarán aquí.
        return services;
    }
}
