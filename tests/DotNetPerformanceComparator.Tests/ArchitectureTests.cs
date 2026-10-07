using System.Reflection;
using DotNetPerformanceComparator.Application;
using DotNetPerformanceComparator.Infrastructure;
using DotNetPerformanceComparator.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetPerformanceComparator.Tests;

public class ArchitectureTests
{
    private const string Prefix = "DotNetPerformanceComparator.";

    private static IEnumerable<string> ProjectReferencesOf(Assembly assembly) =>
        assembly.GetReferencedAssemblies()
            .Select(a => a.Name!)
            .Where(n => n.StartsWith(Prefix));

    [Fact]
    public void Domain_NoDependeDeOtrasCapas()
    {
        var domain = Assembly.Load(Prefix + "Domain");

        Assert.Empty(ProjectReferencesOf(domain));
    }

    [Fact]
    public void Domain_NoDependeDeEntityFrameworkCore()
    {
        var domain = Assembly.Load(Prefix + "Domain");

        Assert.DoesNotContain(domain.GetReferencedAssemblies(), a => a.Name!.StartsWith("Microsoft.EntityFrameworkCore"));
    }

    [Fact]
    public void Application_NoDependeDeInfrastructureNiWeb()
    {
        var references = ProjectReferencesOf(typeof(Application.DependencyInjection).Assembly);

        Assert.DoesNotContain(Prefix + "Infrastructure", references);
        Assert.DoesNotContain(Prefix + "Web", references);
    }

    [Fact]
    public void Infrastructure_NoDependeDeWeb()
    {
        var references = ProjectReferencesOf(typeof(Infrastructure.DependencyInjection).Assembly);

        Assert.DoesNotContain(Prefix + "Web", references);
    }

    [Fact]
    public void RegistroDeCapas_ConstruyeProveedorDeServicios()
    {
        var services = new ServiceCollection()
            .AddApplication()
            .AddInfrastructure("Data Source=:memory:");

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetService<AppDbContext>());
    }

    [Fact]
    public void RegistroDeInfrastructure_SinCadenaDeConexion_Falla()
    {
        Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddInfrastructure(null));
    }
}
