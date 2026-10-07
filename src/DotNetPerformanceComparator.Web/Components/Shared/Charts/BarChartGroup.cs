namespace DotNetPerformanceComparator.Web.Components.Shared.Charts;

/// <summary>
/// Una categoría del gráfico (normalmente una métrica) con su valor en .NET 8 y .NET 10.
/// </summary>
/// <param name="Category">Nombre mostrado bajo el grupo de barras.</param>
/// <param name="Net8Value">Valor para .NET 8.</param>
/// <param name="Net10Value">Valor para .NET 10.</param>
/// <param name="Unit">Unidad mostrada junto a cada valor (p. ej. "%", "MB").</param>
public sealed record BarChartGroup(string Category, decimal Net8Value, decimal Net10Value, string Unit);
