namespace DotNetPerformanceComparator.Application.Evaluations.Results;

/// <summary>
/// Modelos de lectura de CU-05 (Consultar resultados). Son de solo lectura y no exponen entidades EF ni
/// datos internos/transitorios; representan únicamente lo persistido. No recalculan ni promedian nada.
/// </summary>

/// <summary>Elemento del listado de evaluaciones disponibles.</summary>
public sealed record EvaluacionResumenConsulta(
    int Id,
    string Nombre,
    string Proyecto,
    string TipoPrueba,
    string Estado,
    int NumeroEjecuciones,
    DateTime FechaCreacion,
    bool TieneResultados);

/// <summary>Una métrica seleccionada en la configuración (Id estable del catálogo, nombre y unidad).</summary>
public sealed record MetricaSeleccionadaConsulta(int MetricaId, string Nombre, string Unidad);

/// <summary>
/// Valor persistido de una métrica (resumen de ejecución o nivel de estrés). Incluye <c>MetricaId</c> como
/// identificador estable para que CU-06 filtre por métrica sin depender del texto de presentación.
/// </summary>
public sealed record ResultadoMetricaConsulta(int MetricaId, string Metrica, string Unidad, decimal Valor);

/// <summary>Condición de ejecución registrada (contexto reproducible: TipoPrueba, SDK, parámetros, etc.).</summary>
public sealed record CondicionConsulta(string Clave, string Valor);

/// <summary>Un nivel de una ejecución de Estrés con sus resultados por nivel realmente persistidos.</summary>
public sealed record NivelEstresConsulta(
    int NumeroNivel,
    int UsuariosObjetivo,
    string Estado,
    int SolicitudesExitosas,
    int SolicitudesFallidas,
    string? Detalle,
    IReadOnlyList<ResultadoMetricaConsulta> Resultados);

/// <summary>Una ejecución (versión + repetición) con su contexto y resultados persistidos.</summary>
public sealed record EjecucionConsulta(
    string VersionDotNet,
    int NumeroEjecucion,
    string Estado,
    DateTime FechaInicio,
    DateTime? FechaFin,
    IReadOnlyList<CondicionConsulta> Condiciones,
    IReadOnlyList<ResultadoMetricaConsulta> Resultados,
    IReadOnlyList<NivelEstresConsulta> Niveles);

/// <summary>
/// Parámetros del experimento según el tipo de prueba (solo lectura; una evaluación = un `TipoPrueba`,
/// ver D-34). Representación fuertemente tipada de los <c>ParametroEvaluacion</c> persistidos. Cada valor es
/// <c>int?</c>: <c>null</c> indica ausencia o dato no interpretable (una configuración histórica/inconsistente);
/// NUNCA se sustituye por 0, que solo aparece cuando 0 es el valor realmente persistido.
/// </summary>
public abstract record ParametrosPruebaConsulta;

public sealed record ParametrosCargaConsulta(
    int? UsuariosVirtuales,
    int? DuracionSegundos,
    int? WarmUpSegundos,
    int? RampUpSegundos,
    int? IntervaloSolicitudesMs) : ParametrosPruebaConsulta;

public sealed record ParametrosEstresConsulta(
    int? UsuariosIniciales,
    int? IncrementoUsuarios,
    int? UsuariosMaximos,
    int? DuracionNivelSegundos,
    int? WarmUpSegundos,
    int? IntervaloSolicitudesMs) : ParametrosPruebaConsulta;

public sealed record ParametrosConcurrenciaConsulta(
    int? UsuariosConcurrentes,
    int? Rondas,
    int? WarmUpSegundos,
    int? IntervaloRondasMs) : ParametrosPruebaConsulta;

/// <summary>Detalle completo de una evaluación seleccionada.</summary>
public sealed record EvaluacionDetalleConsulta(
    int Id,
    string Nombre,
    string Proyecto,
    string? Migracion,
    string TipoPrueba,
    string Estado,
    int NumeroEjecuciones,
    string RutaObjetivo,
    string? Observaciones,
    DateTime FechaCreacion,
    IReadOnlyList<MetricaSeleccionadaConsulta> MetricasSeleccionadas,
    IReadOnlyList<EjecucionConsulta> Ejecuciones,
    ParametrosPruebaConsulta? Parametros = null)
{
    /// <summary>true si existe al menos un resultado persistido (resumen de ejecución o de nivel de estrés).</summary>
    public bool TieneResultados =>
        Ejecuciones.Any(e => e.Resultados.Count > 0 || e.Niveles.Any(n => n.Resultados.Count > 0));
}

/// <summary>
/// Lectura de evaluaciones y sus resultados persistidos (CU-05). Contrato en Application; implementación EF
/// en Infrastructure. Solo lectura: sin editar, borrar, ejecutar, cancelar, filtrar ni comparar.
/// </summary>
public interface IConsultaResultadosRepository
{
    /// <summary>Evaluaciones disponibles, de la más reciente a la más antigua (lista vacía si no hay).</summary>
    Task<IReadOnlyList<EvaluacionResumenConsulta>> ListarAsync(CancellationToken cancellationToken = default);

    /// <summary>Detalle de una evaluación por Id; <c>null</c> si no existe.</summary>
    Task<EvaluacionDetalleConsulta?> ObtenerDetalleAsync(int configuracionId, CancellationToken cancellationToken = default);
}
