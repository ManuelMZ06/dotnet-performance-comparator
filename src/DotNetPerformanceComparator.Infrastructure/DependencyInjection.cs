using DotNetPerformanceComparator.Application.Evaluations.Configuration;
using DotNetPerformanceComparator.Application.Evaluations.Execution;
using DotNetPerformanceComparator.Application.Evaluations.Results;
using DotNetPerformanceComparator.Application.Migrations.ExperimentalMigration;
using DotNetPerformanceComparator.Infrastructure.Execution;
using DotNetPerformanceComparator.Application.Projects.Incorporation;
using DotNetPerformanceComparator.Infrastructure.ExperimentalMigration;
using DotNetPerformanceComparator.Infrastructure.Persistence;
using DotNetPerformanceComparator.Infrastructure.Persistence.Repositories;
using DotNetPerformanceComparator.Infrastructure.Platform.Windows;
using DotNetPerformanceComparator.Infrastructure.Processes;
using DotNetPerformanceComparator.Infrastructure.Projects;
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

        // Fábrica de contextos de vida corta: un DbContext no debe vivir todo un circuito de Blazor Server.
        services.AddDbContextFactory<AppDbContext>(options => options.UseSqlite(connectionString));

        // CU-01
        services.AddSingleton<IProjectInspector, ProjectInspector>();
        services.AddScoped<IProyectoDotNetRepository, ProyectoDotNetRepository>();

        // Selector nativo de .csproj (solo Windows; en otra plataforma devuelve Unsupported).
        services.AddSingleton<IProjectFilePicker, WindowsProjectFilePicker>();

        // CU-02
        services.AddScoped<IMigracionRepository, MigracionRepository>();
        services.AddSingleton<IProjectCopyService, ProjectCopyService>();
        services.AddSingleton<IProjectFileEditor, ProjectFileEditor>();
        services.AddSingleton<IDotNetCommandRunner, DotNetCommandRunner>();
        services.AddSingleton<IDirectoryPicker, WindowsDirectoryPicker>();

        // CU-03
        services.AddScoped<IConfiguracionEvaluacionRepository, ConfiguracionEvaluacionRepository>();
        services.AddScoped<IEvaluationCatalogRepository, EvaluationCatalogRepository>();

        // CU-05
        services.AddScoped<IConsultaResultadosRepository, ConsultaResultadosRepository>();

        // CU-04 (C-07.1)
        services.AddScoped<IEvaluationExecutionRepository, EvaluationExecutionRepository>();
        services.AddSingleton<IExecutionWorkspacePreparer, ExecutionWorkspacePreparer>();
        services.AddSingleton<ITargetAppLauncher, TargetAppLauncher>();
        services.AddSingleton<ILoadRunner, NBomberLoadRunner>();
        services.AddSingleton<IConcurrencyRunner, ConcurrencyNBomberRunner>();

        return services;
    }
}
