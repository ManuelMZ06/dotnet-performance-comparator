using DotNetPerformanceComparator.Web.Components.Shared.Charts;

namespace DotNetPerformanceComparator.Tests.Web;

public class BarChartLayoutTests
{
    [Fact]
    public void ColeccionVacia_NoGeneraGrupos()
    {
        var geometry = BarChartLayout.Calculate([]);

        Assert.Empty(geometry.Groups);
        Assert.True(geometry.Width > 0);
    }

    [Fact]
    public void Grupo_GeneraBarraNet8YNet10EnOrden()
    {
        var geometry = BarChartLayout.Calculate([new BarChartGroup("CPU", 40m, 30m, "%")]);

        var group = Assert.Single(geometry.Groups);
        Assert.Equal(new[] { BarChartSeries.Net8, BarChartSeries.Net10 }, group.Bars.Select(b => b.Series));
        Assert.True(group.Bars[0].X < group.Bars[1].X);
    }

    [Fact]
    public void Grupo_EscalaRespectoASuPropioMaximo()
    {
        var geometry = BarChartLayout.Calculate(
        [
            new BarChartGroup("CPU", 40m, 20m, "%"),
            new BarChartGroup("Memoria RAM", 800m, 400m, "MB")
        ]);

        foreach (var group in geometry.Groups)
        {
            Assert.Equal(BarChartLayout.PlotHeight, group.Bars[0].Height, 3);
            Assert.Equal(BarChartLayout.PlotHeight / 2, group.Bars[1].Height, 3);
            Assert.All(group.Bars, b => Assert.Equal(geometry.BaselineY, b.Y + b.Height, 3));
        }
    }

    [Fact]
    public void ValoresCeroONegativos_GeneranBarrasSinAltura()
    {
        var geometry = BarChartLayout.Calculate([new BarChartGroup("Throughput", 0m, -5m, "req/s")]);

        Assert.All(Assert.Single(geometry.Groups).Bars, b => Assert.Equal(0, b.Height));
    }
}
