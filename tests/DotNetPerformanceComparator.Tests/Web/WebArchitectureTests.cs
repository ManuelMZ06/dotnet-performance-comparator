using System.Reflection;
using DotNetPerformanceComparator.Infrastructure.Persistence;
using Microsoft.AspNetCore.Components;

namespace DotNetPerformanceComparator.Tests.Web;

public class WebArchitectureTests
{
    [Fact]
    public void ComponentesBlazor_NoInyectanTiposDeInfrastructure()
    {
        var infrastructure = typeof(AppDbContext).Assembly;
        var web = typeof(DotNetPerformanceComparator.Web.Components.App).Assembly;

        var violaciones = web.GetTypes()
            .Where(t => typeof(ComponentBase).IsAssignableFrom(t))
            .SelectMany(t => t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(p => p.GetCustomAttribute<InjectAttribute>() is not null && p.PropertyType.Assembly == infrastructure)
                .Select(p => $"{t.Name}.{p.Name}"))
            .ToList();

        Assert.Empty(violaciones);
    }
}
