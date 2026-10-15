namespace DotNetPerformanceComparator.Application.Evaluations.Comparison;

/// <summary>Conteos descriptivos por interpretación y su texto. No declara ganador, score ni significancia.</summary>
public sealed record ConclusionDescriptiva(int Total, int Mejoras, int Empeoramientos, int Iguales, int NoComparables, string Texto);

/// <summary>
/// Construye la conclusión descriptiva de una comparación (CU-07). Lógica textual pura y única, reutilizada
/// por M-07 (Web) y por el reporte PDF (Infrastructure) para evitar divergencia. Describe conteos, nunca un
/// ganador global, porcentaje de superioridad ni significancia estadística.
/// </summary>
public static class ConclusionComparativa
{
    public static ConclusionDescriptiva Describir(IReadOnlyList<ComparacionMetrica> metricas)
    {
        var mejoras = metricas.Count(m => m.Interpretacion == InterpretacionComparacion.Mejora);
        var empeora = metricas.Count(m => m.Interpretacion == InterpretacionComparacion.Empeora);
        var iguales = metricas.Count(m => m.Interpretacion == InterpretacionComparacion.Igual);
        var noComp = metricas.Count(m => m.Interpretacion == InterpretacionComparacion.NoComparable);
        var total = metricas.Count;

        string texto;
        if (total == 0)
        {
            texto = "No hay métricas configuradas para comparar.";
        }
        else if (mejoras + empeora + iguales == 0)
        {
            texto = "No hay métricas comparables en el experimento.";
        }
        else
        {
            texto = $"En las métricas comparables del experimento, .NET 10 presentó mejora en {mejoras}, " +
                    $"empeoramiento en {empeora} e igualdad en {iguales}.";
        }

        if (noComp > 0)
        {
            texto += $" {noComp} no se pudo comparar.";
        }

        return new ConclusionDescriptiva(total, mejoras, empeora, iguales, noComp, texto);
    }
}
