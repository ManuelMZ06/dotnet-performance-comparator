using DotNetPerformanceComparator.Application.Migrations.ExperimentalMigration;
using DotNetPerformanceComparator.Application.Projects.Incorporation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DotNetPerformanceComparator.Application;

/// <summary>
/// Registro de los servicios de la capa de Aplicación / Orquestación.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);

        // CU-01
        services.AddScoped<IIncorporarProyectoService, IncorporarProyectoService>();

        // CU-02
        services.AddScoped<IMigrarProyectoService, MigrarProyectoService>();

        return services;
    }
}
