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

    // ----------------------------------------------------------------- sharedScale (C-11.2, Estrés por nivel)

    [Fact]
    public void SharedScale_DefaultFalse_EscalaPorGrupo()
    {
        // Mismo escenario que Grupo_EscalaRespectoASuPropioMaximo: sin el flag, cada grupo a su máximo.
        var geometry = BarChartLayout.Calculate(
        [
            new BarChartGroup("5 usuarios", 40m, 20m, "%"),
            new BarChartGroup("10 usuarios", 10m, 5m, "%")
        ]);

        // Grupo 1: Net8=40 → altura plena. Grupo 2: Net8=10 → altura plena (escala propia).
        Assert.Equal(BarChartLayout.PlotHeight, geometry.Groups[0].Bars[0].Height, 3);
        Assert.Equal(BarChartLayout.PlotHeight, geometry.Groups[1].Bars[0].Height, 3);
    }

    [Fact]
    public void SharedScale_True_UsaMaximoGlobal()
    {
        var geometry = BarChartLayout.Calculate(
        [
            new BarChartGroup("5 usuarios", 40m, 20m, "%"),
            new BarChartGroup("10 usuarios", 10m, 5m, "%")
        ], sharedScale: true);

        // Máximo global = 40. Grupo 1 Net8=40 → plena; Net10=20 → mitad.
        Assert.Equal(BarChartLayout.PlotHeight, geometry.Groups[0].Bars[0].Height, 3);
        Assert.Equal(BarChartLayout.PlotHeight / 2, geometry.Groups[0].Bars[1].Height, 3);
        // Grupo 2 Net8=10 → 1/4; Net10=5 → 1/8 (proporción conservada contra el grupo grande).
        Assert.Equal(BarChartLayout.PlotHeight / 4, geometry.Groups[1].Bars[0].Height, 3);
        Assert.Equal(BarChartLayout.PlotHeight / 8, geometry.Groups[1].Bars[1].Height, 3);
    }

    [Fact]
    public void SharedScale_TodosCero_NoDividePorCero()
    {
        var geometry = BarChartLayout.Calculate(
        [
            new BarChartGroup("5 usuarios", 0m, 0m, "req/s"),
            new BarChartGroup("10 usuarios", 0m, 0m, "req/s")
        ], sharedScale: true);

        Assert.All(geometry.Groups, g => Assert.All(g.Bars, b => Assert.Equal(0, b.Height)));
    }

    [Fact]
    public void SharedScale_ColeccionVacia_NoFalla()
    {
        var geometry = BarChartLayout.Calculate([], sharedScale: true);
        Assert.Empty(geometry.Groups);
    }
}
