namespace DotNetPerformanceComparator.Web.Components.Shared.Charts;

public enum BarChartSeries
{
    Net8,
    Net10
}

public sealed record BarChartBar(BarChartSeries Series, decimal Value, double X, double Y, double Width, double Height);

public sealed record BarChartGroupLayout(string Category, string Unit, double CenterX, IReadOnlyList<BarChartBar> Bars);

public sealed record BarChartGeometry(double Width, double Height, double BaselineY, IReadOnlyList<BarChartGroupLayout> Groups);

/// <summary>
/// Calcula la geometría SVG de <see cref="BarChart"/>.
/// Por defecto (<c>sharedScale = false</c>) cada grupo se escala respecto a su propio valor máximo, porque
/// las categorías pueden ser métricas con unidades distintas (%, MB, ms...) y no son comparables entre sí.
/// Con <c>sharedScale = true</c> todos los grupos comparten un mismo máximo (mismo máximo absoluto de los
/// valores de todos los grupos): se usa cuando las categorías son la misma métrica a distintas cargas
/// (Estrés por nivel), donde las alturas SÍ deben compararse entre grupos.
/// </summary>
public static class BarChartLayout
{
    public const double Height = 220;
    public const double TopPadding = 24;
    public const double BottomPadding = 36;
    public const double BarWidth = 40;
    public const double BarGap = 8;
    public const double GroupGap = 48;
    public const double SidePadding = 24;

    public static double PlotHeight => Height - TopPadding - BottomPadding;

    public static BarChartGeometry Calculate(IReadOnlyList<BarChartGroup> groups, bool sharedScale = false)
    {
        var baseline = Height - BottomPadding;
        var groupWidth = BarWidth * 2 + BarGap;
        var width = groups.Count == 0
            ? SidePadding * 2
            : SidePadding * 2 + groups.Count * groupWidth + (groups.Count - 1) * GroupGap;

        // Escala compartida: máximo absoluto entre todos los grupos (misma unidad). Sin grupos → 0.
        var sharedMax = sharedScale && groups.Count > 0
            ? groups.Max(g => Math.Max(Math.Max(g.Net8Value, g.Net10Value), 0m))
            : 0m;

        var layouts = new List<BarChartGroupLayout>(groups.Count);
        for (var i = 0; i < groups.Count; i++)
        {
            var group = groups[i];
            var x = SidePadding + i * (groupWidth + GroupGap);
            var max = sharedScale ? sharedMax : Math.Max(Math.Max(group.Net8Value, group.Net10Value), 0m);

            layouts.Add(new BarChartGroupLayout(
                group.Category,
                group.Unit,
                x + groupWidth / 2,
                [
                    CreateBar(BarChartSeries.Net8, group.Net8Value, x, max, baseline),
                    CreateBar(BarChartSeries.Net10, group.Net10Value, x + BarWidth + BarGap, max, baseline)
                ]));
        }

        return new BarChartGeometry(width, Height, baseline, layouts);
    }

    private static BarChartBar CreateBar(BarChartSeries series, decimal value, double x, decimal max, double baseline)
    {
        var height = max <= 0m || value <= 0m
            ? 0
            : (double)(value / max) * PlotHeight;

        return new BarChartBar(series, value, x, baseline - height, BarWidth, height);
    }
}
