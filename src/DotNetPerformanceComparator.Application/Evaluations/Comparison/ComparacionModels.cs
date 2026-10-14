namespace DotNetPerformanceComparator.Application.Evaluations.Comparison;

/// <summary>
/// Modelos de lectura transitorios de CU-07 (Comparar .NET 8 vs .NET 10). No son entidades EF ni se
/// persisten (ver D-50): la comparación se deriva de forma determinista del detalle ya consultado en CU-05.
/// .NET 8 es la referencia/base; .NET 10 es la versión experimental.
/// </summary>

/// <summary>
/// Sentido de "mejor" de una métrica. No se persiste ni se infiere del texto; se mapea por
/// <c>MetricaId</c> estable en <see cref="ComparacionResultadosService"/>.
/// </summary>
public enum OrientacionMetrica
{
    /// <summary>Un valor menor es preferible (CPU, RAM, Tiempo de respuesta, Latencia, Tamaño).</summary>
    MenorEsMejor,

    /// <summary>Un valor mayor es preferible (Throughput).</summary>
    MayorEsMejor
}

/// <summary>Juicio cualitativo de la comparación de una métrica, separado del signo matemático.</summary>
public enum InterpretacionComparacion
{
    /// <summary>.NET 10 es mejor que .NET 8 según la orientación de la métrica.</summary>
    Mejora,

    /// <summary>.NET 10 es peor que .NET 8 según la orientación de la métrica.</summary>
    Empeora,

    /// <summary>Ambos promedios son exactamente iguales (igualdad exacta sobre <c>decimal</c>).</summary>
    Igual,

    /// <summary>No existe ningún par experimental válido para comparar esta métrica.</summary>
    NoComparable
}

/// <summary>
/// Comparación de una métrica entre versiones. <see cref="DiferenciaAbsoluta"/> y
/// <see cref="DiferenciaPorcentual"/> son el dato matemático firmado; <see cref="Interpretacion"/> es el
/// juicio según <see cref="Orientacion"/>. Los promedios y diferencias se calculan sin redondear (el
/// redondeo es responsabilidad de la presentación).
/// </summary>
public sealed record ComparacionMetrica(
    int MetricaId,
    string Nombre,
    string Unidad,
    OrientacionMetrica Orientacion,
    decimal? PromedioNet8,
    decimal? PromedioNet10,
    decimal? DiferenciaAbsoluta,
    decimal? DiferenciaPorcentual,
    InterpretacionComparacion Interpretacion,
    int ParesUtilizados,
    string? Nota = null);

/// <summary>
/// Comparación de resumen de una evaluación (Carga/Concurrencia, o el resumen del nivel máximo en Estrés).
/// <see cref="EsComparable"/> es true si al menos una métrica tiene una comparación válida (comparación
/// parcial permitida); false solo si ninguna métrica puede compararse.
/// </summary>
public sealed record ComparacionResumen(
    int ConfiguracionId,
    string Nombre,
    string TipoPrueba,
    bool EsComparable,
    IReadOnlyList<ComparacionMetrica> Metricas,
    string? NotaNoComparable = null);

/// <summary>
/// Comparación por carga equivalente de Estrés. El emparejamiento es por
/// (<c>NumeroEjecucion</c> + <c>UsuariosObjetivo</c> + <c>MetricaId</c>), nunca por número de nivel. Solo
/// niveles Completado en ambas versiones de la misma repetición forman par normal. Tamaño no participa.
/// </summary>
public sealed record ComparacionNivelEstres(
    int UsuariosObjetivo,
    bool NivelComparable,
    IReadOnlyList<ComparacionMetrica> Metricas,
    string? NotaNivel = null);

/// <summary>
/// Contexto de saturación observado para una versión en este experimento concreto (no una capacidad
/// universal). Derivado de los <c>NivelEstres</c> persistidos; no es una métrica ni se persiste.
/// </summary>
/// <param name="MaxUsuariosCompletados">
/// Máximo nivel (UsuariosObjetivo) <b>Completado observado en al menos una repetición</b>. No implica que
/// la versión completara ese nivel en todas las repeticiones; es el mayor nivel que llegó a completarse.
/// </param>
/// <param name="Saturo">true si la versión saturó en al menos una repetición.</param>
/// <param name="NivelSaturacionMinObservado">
/// Menor UsuariosObjetivo donde se observó saturación entre las repeticiones (null si no saturó).
/// </param>
/// <param name="NivelSaturacionMaxObservado">
/// Mayor UsuariosObjetivo donde se observó saturación entre las repeticiones (null si no saturó). Si
/// coincide con el mínimo, la saturación fue consistente; si difiere, la saturación varió entre
/// repeticiones (no se colapsa en un solo número arbitrario).
/// </param>
public sealed record ContextoSaturacionVersion(
    int? MaxUsuariosCompletados,
    bool Saturo,
    int? NivelSaturacionMinObservado,
    int? NivelSaturacionMaxObservado);

/// <summary>Contexto de saturación de ambas versiones (información del escenario, no métrica).</summary>
public sealed record ContextoSaturacion(
    ContextoSaturacionVersion Net8,
    ContextoSaturacionVersion Net10);

/// <summary>
/// Comparación completa de una evaluación de Estrés: resumen (nivel máximo), comparación por nivel de
/// carga equivalente y contexto de saturación. Modelo separado del resumen normal para no mezclar
/// semánticas distintas.
/// </summary>
public sealed record ComparacionEstres(
    ComparacionResumen Resumen,
    IReadOnlyList<ComparacionNivelEstres> Niveles,
    ContextoSaturacion Contexto);
