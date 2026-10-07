using Microsoft.Extensions.DependencyInjection;

namespace DotNetPerformanceComparator.Application;

/// <summary>
/// Registro de los servicios de la capa de Aplicación / Orquestación.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Los servicios de casos de uso se registrarán aquí conforme se implementen.
        return services;
    }
}
