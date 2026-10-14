using DotNetPerformanceComparator.Application.Evaluations.Comparison;
using DotNetPerformanceComparator.Application.Evaluations.Configuration;
using DotNetPerformanceComparator.Application.Evaluations.Execution;
using DotNetPerformanceComparator.Application.Evaluations.Results;
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

        // CU-03
        services.AddScoped<IConfigurarEvaluacionService, ConfigurarEvaluacionService>();

        // CU-04 (C-07.1 / C-07.2 / C-07.3 / C-07.4)
        services.AddSingleton<IEvaluationExecutionGate, EvaluationExecutionGate>();
        services.AddScoped<IStressExecutionRunner, StressExecutionRunner>();
        services.AddScoped<IConcurrencyExecutionRunner, ConcurrencyExecutionRunner>();
        services.AddScoped<IEjecutarEvaluacionService, EjecutarEvaluacionService>();
        services.AddScoped<IExecutionStatusQueryService, ExecutionStatusQueryService>();

        // CU-05
        services.AddScoped<IConsultaResultadosService, ConsultaResultadosService>();

        // CU-07 (motor comparativo puro/determinista; sin estado, sin dependencias)
        services.AddSingleton<IComparacionResultadosService, ComparacionResultadosService>();

        return services;
    }
}
