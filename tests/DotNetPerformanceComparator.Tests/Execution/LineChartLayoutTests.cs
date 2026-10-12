using DotNetPerformanceComparator.Web.Components.Shared.Charts;

namespace DotNetPerformanceComparator.Tests.Execution;

public class LineChartLayoutTests
{
    private static LineChartPoint P(double x, double y) => new(x, y);

    [Fact]
    public void ListaVacia_NoTieneDatos()
    {
        var g = LineChartLayout.Calculate([]);
        Assert.False(g.HasData);
        Assert.Empty(g.Puntos);
        Assert.Equal(string.Empty, g.Polyline);
    }

    [Fact]
    public void UnPunto_SeUbicaSinError()
    {
        var g = LineChartLayout.Calculate([P(0, 0.5)]);
        Assert.True(g.HasData);
        var (x, y) = Assert.Single(g.Puntos);
        Assert.Equal(LineChartLayout.PadLeft, x); // único punto → a la izquierda del área
        Assert.InRange(y, g.PlotTop, g.PlotBottom);
    }

    [Fact]
    public void VariosPuntos_GeneranPolylineYCoordenadasDentroDelArea()
    {
        var g = LineChartLayout.Calculate([P(0, 10), P(1, 20), P(2, 40), P(3, 100)]);
        Assert.True(g.HasData);
        Assert.Equal(4, g.Puntos.Count);
        Assert.False(string.IsNullOrWhiteSpace(g.Polyline));
        Assert.All(g.Puntos, pt =>
        {
            Assert.InRange(pt.X, g.PlotLeft, g.PlotRight);
            Assert.InRange(pt.Y, g.PlotTop, g.PlotBottom);
        });
        // Mayor Y ⇒ menor coordenada vertical (más arriba).
        Assert.True(g.Puntos[3].Y < g.Puntos[0].Y);
    }

    [Fact]
    public void TodosLosValoresIguales_SinDivisionPorCero()
    {
        var g = LineChartLayout.Calculate([P(0, 5), P(1, 5), P(2, 5)]);
        Assert.All(g.Puntos, pt => Assert.True(double.IsFinite(pt.Y)));
        Assert.All(g.Puntos, pt => Assert.InRange(pt.Y, g.PlotTop, g.PlotBottom));
    }

    [Fact]
    public void TodosCero_SinDivisionPorCero()
    {
        var g = LineChartLayout.Calculate([P(0, 0), P(1, 0)]);
        Assert.All(g.Puntos, pt => Assert.True(double.IsFinite(pt.Y)));
        // Con todos en 0, la línea queda en la base.
        Assert.All(g.Puntos, pt => Assert.Equal(g.PlotBottom, pt.Y, 3));
    }

    [Fact]
    public void Cpu100_QuedaEnLaParteSuperior()
    {
        var g = LineChartLayout.Calculate([P(0, 0), P(1, 100)]);
        Assert.Equal(g.PlotBottom, g.Puntos[0].Y, 3); // 0 % en la base
        Assert.Equal(g.PlotTop, g.Puntos[1].Y, 3);    // 100 % (máximo) arriba
    }

    [Fact]
    public void DimensionesLogicas()
    {
        var g = LineChartLayout.Calculate([P(0, 1)]);
        Assert.Equal(LineChartLayout.Width, g.Width);
        Assert.Equal(LineChartLayout.Height, g.Height);
    }
}
