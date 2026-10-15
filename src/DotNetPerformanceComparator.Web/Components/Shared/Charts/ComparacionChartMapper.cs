using DotNetPerformanceComparator.Application.Evaluations.Comparison;

namespace DotNetPerformanceComparator.Web.Components.Shared.Charts;

/// <summary>
/// Mapeo puro de la comparación de CU-07 a modelos visuales de barras (CU-08). No calcula la comparación,
/// no consulta datos y no contiene estado de página: solo transforma <see cref="ComparacionMetrica"/> en
/// <see cref="BarChartGroup"/>. Compartido por M-07 (small multiples) y M-01 (resumen reciente).
/// </summary>
public static class ComparacionChartMapper
{
    /// <summary>Una métrica es graficable solo si es comparable y tiene ambos promedios (nunca null→0).</summary>
    public static bool EsGraficable(ComparacionMetrica m) =>
        m.Interpretacion != InterpretacionComparacion.NoComparable &&
        m.PromedioNet8 is not null &&
        m.PromedioNet10 is not null;

    /// <summary>
    /// Un <see cref="BarChartGroup"/> por métrica graficable (small multiple = un gráfico por métrica).
    /// Las métricas NoComparable se omiten; un cero real sí se incluye (barra 0 con etiqueta "0").
    /// </summary>
    public static IReadOnlyList<BarChartGroup> ResumenAGrupos(IEnumerable<ComparacionMetrica> metricas) =>
        metricas
            .Where(EsGraficable)
            .Select(m => new BarChartGroup(m.Nombre, m.PromedioNet8!.Value, m.PromedioNet10!.Value, m.Unidad))
            .ToList();
}
