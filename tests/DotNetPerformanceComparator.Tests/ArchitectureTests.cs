using System.Reflection;
using DotNetPerformanceComparator.Application;
using DotNetPerformanceComparator.Infrastructure;
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
            .AddInfrastructure();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });

        Assert.NotNull(provider);
    }
}
