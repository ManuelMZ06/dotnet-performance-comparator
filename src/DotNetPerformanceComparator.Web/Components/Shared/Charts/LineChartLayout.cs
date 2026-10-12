namespace DotNetPerformanceComparator.Web.Components.Shared.Charts;

/// <summary>Un punto de la serie temporal (X = muestra sucesiva; Y = valor, p. ej. CPU %).</summary>
public sealed record LineChartPoint(double X, double Y);

public sealed record LineChartGeometry(
    double Width,
    double Height,
    double PlotLeft,
    double PlotRight,
    double PlotTop,
    double PlotBottom,
    double MinY,
    double MaxY,
    IReadOnlyList<(double X, double Y)> Puntos,
    string Polyline,
    bool HasData);

/// <summary>
/// Geometría SVG pura y testeable del <see cref="LineChart"/> (sin librerías externas). El eje X son
/// muestras sucesivas (0,1,2,...) del monitor; el eje Y arranca en 0 (CPU %). Maneja lista vacía, un solo
/// punto, valores iguales y evita divisiones por cero.
/// </summary>
public static class LineChartLayout
{
    public const double Width = 640;
    public const double Height = 200;
    public const double PadLeft = 44;
    public const double PadRight = 16;
    public const double PadTop = 16;
    public const double PadBottom = 28;

    public static double PlotWidth => Width - PadLeft - PadRight;
    public static double PlotHeight => Height - PadTop - PadBottom;

    public static LineChartGeometry Calculate(IReadOnlyList<LineChartPoint> puntos)
    {
        var left = PadLeft;
        var right = Width - PadRight;
        var top = PadTop;
        var bottom = Height - PadBottom;

        if (puntos.Count == 0)
        {
            return new LineChartGeometry(Width, Height, left, right, top, bottom, 0, 1, [], string.Empty, HasData: false);
        }

        var maxY = puntos.Max(p => p.Y);
        var minY = 0.0; // CPU % nunca es negativo; el eje arranca en 0.
        // Evita división por cero cuando todos los valores son iguales (o 0).
        var rangoY = maxY - minY;
        if (rangoY <= 0)
        {
            rangoY = maxY > 0 ? maxY : 1.0;
        }

        var maxX = puntos[^1].X;
        var minX = puntos[0].X;
        var rangoX = maxX - minX;

        var proyectados = new List<(double X, double Y)>(puntos.Count);
        foreach (var p in puntos)
        {
            // Un único punto (o todos con la misma X): se ubica a la izquierda del área de trazado.
            var px = rangoX <= 0 ? left : left + (p.X - minX) / rangoX * PlotWidth;
            var py = bottom - (p.Y - minY) / rangoY * PlotHeight;
            proyectados.Add((px, py));
        }

        var polyline = string.Join(' ', proyectados.Select(p =>
            string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{p.X:0.##},{p.Y:0.##}")));

        return new LineChartGeometry(Width, Height, left, right, top, bottom, minY, maxY, proyectados, polyline, HasData: true);
    }
}
