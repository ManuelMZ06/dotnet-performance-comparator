using DotNetPerformanceComparator.Application.Evaluations.Results;
using DotNetPerformanceComparator.Domain.Constants;

namespace DotNetPerformanceComparator.Application.Evaluations.Comparison;

/// <summary>
/// Cálculo determinista de la comparación .NET 8 vs .NET 10 (CU-07). Consume únicamente el detalle de
/// lectura de CU-05 (<see cref="EvaluacionDetalleConsulta"/>); no accede a EF, Infrastructure, repositorios,
/// filesystem ni NBomber, y no persiste nada (ver D-50).
/// </summary>
public interface IComparacionResultadosService
{
    /// <summary>
    /// Comparación de resumen (Carga/Concurrencia, o el nivel máximo en Estrés). .NET 8 es la base.
    /// </summary>
    ComparacionResumen CompararResumen(EvaluacionDetalleConsulta detalle);

    /// <summary>Comparación específica de Estrés: resumen + niveles equivalentes + contexto de saturación.</summary>
    ComparacionEstres CompararEstres(EvaluacionDetalleConsulta detalle);
}

/// <inheritdoc />
public sealed class ComparacionResultadosService : IComparacionResultadosService
{
    private const int TamanoMetricaId = 6; // "Tamaño de la aplicación": valor constante por versión.

    /// <summary>Una muestra emparejable: versión, repetición (NumeroEjecucion) y valor persistido.</summary>
    private readonly record struct Muestra(string Version, int Repeticion, decimal Valor);

    // ------------------------------------------------------------------ Orientación (por MetricaId estable)

    /// <summary>
    /// Sentido de "mejor" por <c>MetricaId</c> del catálogo (D-50). No se mapea por texto ni se persiste.
    /// Para Tamaño (6), "menor es mejor" significa menor huella/tamaño, no mejor rendimiento de ejecución.
    /// </summary>
    private static OrientacionMetrica Orientacion(int metricaId) => metricaId switch
    {
        1 => OrientacionMetrica.MenorEsMejor, // CPU
        2 => OrientacionMetrica.MenorEsMejor, // Memoria RAM
        3 => OrientacionMetrica.MenorEsMejor, // Tiempo de respuesta
        4 => OrientacionMetrica.MenorEsMejor, // Latencia (P95)
        5 => OrientacionMetrica.MayorEsMejor, // Throughput
        6 => OrientacionMetrica.MenorEsMejor, // Tamaño de la aplicación (huella)
        _ => OrientacionMetrica.MenorEsMejor  // por defecto conservador (solo catálogo 1..6 en la práctica)
    };

    // ------------------------------------------------------------------ Resumen (Carga / Concurrencia / Estrés)

    public ComparacionResumen CompararResumen(EvaluacionDetalleConsulta detalle)
    {
        // Solo ejecuciones con resumen legítimo: estado Completada (excluye Fallida, Cancelada y, en Estrés,
        // CompletadaConSaturacion, que no tiene resumen). Se respetan los estados finales persistidos.
        var fuentes = detalle.Ejecuciones
            .Where(e => e.Estado == EstadosEjecucion.Completada)
            .ToList();

        var metricas = detalle.MetricasSeleccionadas
            .Select(m => m.MetricaId == TamanoMetricaId
                ? CompararTamano(m, fuentes)
                : CompararMetricaPorRepeticion(m, MuestrasResumen(m.MetricaId, fuentes)))
            .ToList();

        var esComparable = metricas.Any(c => c.Interpretacion != InterpretacionComparacion.NoComparable);
        var nota = esComparable ? null : NotaResumenNoComparable(detalle, fuentes);

        return new ComparacionResumen(detalle.Id, detalle.Nombre, detalle.TipoPrueba, esComparable, metricas, nota);
    }

    /// <summary>Muestras de resumen para una métrica: valor del <c>ResultadoMetrica</c> de cada ejecución fuente.</summary>
    private static IReadOnlyList<Muestra> MuestrasResumen(int metricaId, IReadOnlyList<EjecucionConsulta> fuentes) =>
        fuentes
            .Where(e => e.VersionDotNet is VersionesDotNet.Net8 or VersionesDotNet.Net10)
            .SelectMany(e => e.Resultados
                .Where(r => r.MetricaId == metricaId)
                .Select(r => new Muestra(e.VersionDotNet, e.NumeroEjecucion, r.Valor)))
            .ToList();

    private static string NotaResumenNoComparable(EvaluacionDetalleConsulta detalle, IReadOnlyList<EjecucionConsulta> fuentes)
    {
        var hayNet8 = fuentes.Any(e => e.VersionDotNet == VersionesDotNet.Net8);
        var hayNet10 = fuentes.Any(e => e.VersionDotNet == VersionesDotNet.Net10);
        if (!hayNet8 || !hayNet10)
        {
            return detalle.TipoPrueba == TiposPruebaConocidos.Estres
                ? "Ninguna repetición alcanzó el nivel máximo en ambas versiones (sin resumen comparable)."
                : "No hay ejecuciones completadas en ambas versiones para comparar.";
        }

        return "No hay repeticiones con la misma métrica registrada en ambas versiones.";
    }

    // ------------------------------------------------------------------ Estrés (niveles equivalentes)

    public ComparacionEstres CompararEstres(EvaluacionDetalleConsulta detalle)
    {
        var resumen = CompararResumen(detalle);
        var niveles = CompararNiveles(detalle);
        var contexto = DerivarContexto(detalle);
        return new ComparacionEstres(resumen, niveles, contexto);
    }

    /// <summary>
    /// Comparación por carga equivalente (<c>UsuariosObjetivo</c>). Solo se consideran niveles de ejecuciones
    /// que terminaron su serie de estrés (Completada o CompletadaConSaturacion); las Fallida/Cancelada se
    /// descartan como fuente. Un par normal exige nivel Completado en ambas versiones de la misma repetición.
    /// Tamaño nunca participa por nivel.
    /// </summary>
    private IReadOnlyList<ComparacionNivelEstres> CompararNiveles(EvaluacionDetalleConsulta detalle)
    {
        var fuentes = detalle.Ejecuciones
            .Where(e => e.VersionDotNet is VersionesDotNet.Net8 or VersionesDotNet.Net10)
            .Where(e => e.Estado is EstadosEjecucion.Completada or EstadosEjecucion.CompletadaConSaturacion)
            .ToList();

        // Cargas (UsuariosObjetivo) observadas en cualquier nivel de cualquier versión.
        var cargas = fuentes
            .SelectMany(e => e.Niveles.Select(n => n.UsuariosObjetivo))
            .Distinct()
            .OrderBy(u => u)
            .ToList();

        var metricasPorNivel = detalle.MetricasSeleccionadas
            .Where(m => m.MetricaId != TamanoMetricaId)
            .ToList();

        var resultado = new List<ComparacionNivelEstres>(cargas.Count);
        foreach (var carga in cargas)
        {
            // Repeticiones donde AMBAS versiones completaron un nivel con esta carga. Solo esas repeticiones
            // aportan pares; una repetición saturada en una versión no participa (pero no invalida las demás).
            var repsNet8 = RepeticionesConNivelCompletado(fuentes, VersionesDotNet.Net8, carga);
            var repsNet10 = RepeticionesConNivelCompletado(fuentes, VersionesDotNet.Net10, carga);
            var paresCompletos = repsNet8.Intersect(repsNet10).ToHashSet();

            var metricas = metricasPorNivel
                .Select(m => CompararMetricaPorRepeticion(m, MuestrasNivel(m.MetricaId, fuentes, carga, paresCompletos)))
                .ToList();

            // El nivel es comparable si ALGUNA métrica logró al menos un par completo; cada métrica conserva
            // su propio ParesUtilizados/Interpretacion. Que el nivel se "completara" no basta si ninguna
            // métrica tiene pares comparables.
            var nivelComparable = metricas.Any(m => m.Interpretacion != InterpretacionComparacion.NoComparable);
            var nota = nivelComparable ? null : NotaNivelNoComparable(fuentes, carga, paresCompletos.Count > 0);
            resultado.Add(new ComparacionNivelEstres(carga, nivelComparable, metricas, nota));
        }

        return resultado;
    }

    private static HashSet<int> RepeticionesConNivelCompletado(
        IReadOnlyList<EjecucionConsulta> fuentes, string version, int carga) =>
        fuentes
            .Where(e => e.VersionDotNet == version)
            .Where(e => e.Niveles.Any(n => n.UsuariosObjetivo == carga && n.Estado == EstadosNivelEstres.Completado))
            .Select(e => e.NumeroEjecucion)
            .ToHashSet();

    /// <summary>Muestras de un nivel: valor de la métrica en el nivel Completado de cada par completo.</summary>
    private static IReadOnlyList<Muestra> MuestrasNivel(
        int metricaId, IReadOnlyList<EjecucionConsulta> fuentes, int carga, IReadOnlySet<int> paresCompletos) =>
        fuentes
            .Where(e => paresCompletos.Contains(e.NumeroEjecucion))
            .SelectMany(e => e.Niveles
                .Where(n => n.UsuariosObjetivo == carga && n.Estado == EstadosNivelEstres.Completado)
                .SelectMany(n => n.Resultados
                    .Where(r => r.MetricaId == metricaId)
                    .Select(r => new Muestra(e.VersionDotNet, e.NumeroEjecucion, r.Valor))))
            .ToList();

    private static string NotaNivelNoComparable(IReadOnlyList<EjecucionConsulta> fuentes, int carga, bool hayParCompleto)
    {
        // El nivel se completó en ambas versiones de alguna repetición, pero ninguna métrica tuvo pares
        // comparables (p. ej. solo se seleccionó Tamaño, excluido por nivel, o faltan los valores).
        if (hayParCompleto)
        {
            return "El nivel se completó en ambas versiones, pero ninguna métrica tiene pares comparables.";
        }

        bool TieneNivel(string version) =>
            fuentes.Any(e => e.VersionDotNet == version && e.Niveles.Any(n => n.UsuariosObjetivo == carga));

        var enNet8 = TieneNivel(VersionesDotNet.Net8);
        var enNet10 = TieneNivel(VersionesDotNet.Net10);
        if (enNet8 && !enNet10)
        {
            return "Nivel presente solo en .NET 8.";
        }

        if (enNet10 && !enNet8)
        {
            return "Nivel presente solo en .NET 10.";
        }

        return "El nivel no fue Completado en ambas versiones de una misma repetición (p. ej. saturación).";
    }

    /// <summary>Contexto de saturación observado por versión (información del escenario, no métrica).</summary>
    private static ContextoSaturacion DerivarContexto(EvaluacionDetalleConsulta detalle)
    {
        var fuentes = detalle.Ejecuciones
            .Where(e => e.VersionDotNet is VersionesDotNet.Net8 or VersionesDotNet.Net10)
            .Where(e => e.Estado is EstadosEjecucion.Completada or EstadosEjecucion.CompletadaConSaturacion)
            .ToList();

        return new ContextoSaturacion(ContextoDe(fuentes, VersionesDotNet.Net8), ContextoDe(fuentes, VersionesDotNet.Net10));
    }

    private static ContextoSaturacionVersion ContextoDe(IReadOnlyList<EjecucionConsulta> fuentes, string version)
    {
        var niveles = fuentes.Where(e => e.VersionDotNet == version).SelectMany(e => e.Niveles).ToList();

        var completados = niveles.Where(n => n.Estado == EstadosNivelEstres.Completado).Select(n => n.UsuariosObjetivo).ToList();
        var saturados = niveles.Where(n => n.Estado == EstadosNivelEstres.Saturado).Select(n => n.UsuariosObjetivo).ToList();

        int? maxCompletados = completados.Count > 0 ? completados.Max() : null;
        // Rango de saturación observado: no se colapsa en un único número cuando las repeticiones saturan
        // en niveles distintos (Min == Max indica saturación consistente).
        int? saturacionMin = saturados.Count > 0 ? saturados.Min() : null;
        int? saturacionMax = saturados.Count > 0 ? saturados.Max() : null;
        return new ContextoSaturacionVersion(maxCompletados, saturados.Count > 0, saturacionMin, saturacionMax);
    }

    // ------------------------------------------------------------------ Núcleo matemático (pares completos)

    /// <summary>
    /// Compara una métrica emparejando por repetición: una repetición participa solo si ambas versiones
    /// aportan exactamente un valor limpio. Promedios de pares completos; diferencias sin redondear.
    /// </summary>
    private static ComparacionMetrica CompararMetricaPorRepeticion(MetricaSeleccionadaConsulta metrica, IReadOnlyList<Muestra> muestras)
    {
        var pares = new List<(decimal Net8, decimal Net10)>();
        var huboDuplicados = false;

        foreach (var grupo in muestras.GroupBy(m => m.Repeticion))
        {
            var net8 = grupo.Where(m => m.Version == VersionesDotNet.Net8).ToList();
            var net10 = grupo.Where(m => m.Version == VersionesDotNet.Net10).ToList();

            // Dato duplicado para una versión en la misma repetición: no se elige arbitrariamente, se omite.
            if (net8.Count > 1 || net10.Count > 1)
            {
                huboDuplicados = true;
                continue;
            }

            if (net8.Count == 1 && net10.Count == 1)
            {
                pares.Add((net8[0].Valor, net10[0].Valor));
            }
        }

        var orientacion = Orientacion(metrica.MetricaId);
        if (pares.Count == 0)
        {
            var notaVacia = huboDuplicados ? "Datos duplicados en las repeticiones; no se comparó." : null;
            return NoComparable(metrica, orientacion, 0, notaVacia);
        }

        var promedioNet8 = pares.Sum(p => p.Net8) / pares.Count;
        var promedioNet10 = pares.Sum(p => p.Net10) / pares.Count;
        var (abs, pct, interpretacion) = Calcular(promedioNet8, promedioNet10, orientacion);
        var nota = huboDuplicados ? "Se ignoraron repeticiones con datos duplicados." : null;

        return new ComparacionMetrica(
            metrica.MetricaId, metrica.Nombre, metrica.Unidad, orientacion,
            promedioNet8, promedioNet10, abs, pct, interpretacion, pares.Count, nota);
    }

    /// <summary>
    /// Compara el Tamaño como valor constante por versión (se mide una vez por versión y se repite entre
    /// repeticiones). Valores idénticos repetidos = una sola constante; valores distintos en una misma
    /// versión = inconsistencia (no se promedia ni se elige). <c>ParesUtilizados</c> conceptual = 1.
    /// </summary>
    private static ComparacionMetrica CompararTamano(MetricaSeleccionadaConsulta metrica, IReadOnlyList<EjecucionConsulta> fuentes)
    {
        var orientacion = Orientacion(metrica.MetricaId);

        var (valorNet8, inconsistente8) = ConstantePorVersion(fuentes, VersionesDotNet.Net8, metrica.MetricaId);
        var (valorNet10, inconsistente10) = ConstantePorVersion(fuentes, VersionesDotNet.Net10, metrica.MetricaId);

        if (inconsistente8 || inconsistente10)
        {
            var versionMala = inconsistente8 && inconsistente10 ? "ambas versiones"
                : inconsistente8 ? ".NET 8" : ".NET 10";
            return NoComparable(metrica, orientacion, 0, $"Valores de Tamaño inconsistentes en {versionMala}.");
        }

        if (valorNet8 is null || valorNet10 is null)
        {
            return NoComparable(metrica, orientacion, 0, null);
        }

        var (abs, pct, interpretacion) = Calcular(valorNet8.Value, valorNet10.Value, orientacion);
        return new ComparacionMetrica(
            metrica.MetricaId, metrica.Nombre, metrica.Unidad, orientacion,
            valorNet8, valorNet10, abs, pct, interpretacion, 1, null);
    }

    /// <summary>Valor constante de una métrica para una versión: (valor, inconsistente). null = sin dato.</summary>
    private static (decimal? Valor, bool Inconsistente) ConstantePorVersion(
        IReadOnlyList<EjecucionConsulta> fuentes, string version, int metricaId)
    {
        var distintos = fuentes
            .Where(e => e.VersionDotNet == version)
            .SelectMany(e => e.Resultados.Where(r => r.MetricaId == metricaId).Select(r => r.Valor))
            .Distinct()
            .ToList();

        return distintos.Count switch
        {
            0 => (null, false),
            1 => (distintos[0], false),
            _ => (null, true) // varios valores distintos para la misma versión: inconsistencia
        };
    }

    private static (decimal? Abs, decimal? Pct, InterpretacionComparacion Interpretacion) Calcular(
        decimal promedioNet8, decimal promedioNet10, OrientacionMetrica orientacion)
    {
        var abs = promedioNet10 - promedioNet8;
        decimal? pct = promedioNet8 == 0m ? null : (promedioNet10 - promedioNet8) / promedioNet8 * 100m;

        InterpretacionComparacion interpretacion;
        if (promedioNet10 == promedioNet8)
        {
            interpretacion = InterpretacionComparacion.Igual;
        }
        else
        {
            var net10Menor = promedioNet10 < promedioNet8;
            interpretacion = orientacion == OrientacionMetrica.MenorEsMejor
                ? (net10Menor ? InterpretacionComparacion.Mejora : InterpretacionComparacion.Empeora)
                : (net10Menor ? InterpretacionComparacion.Empeora : InterpretacionComparacion.Mejora);
        }

        return (abs, pct, interpretacion);
    }

    private static ComparacionMetrica NoComparable(
        MetricaSeleccionadaConsulta metrica, OrientacionMetrica orientacion, int pares, string? nota) =>
        new(metrica.MetricaId, metrica.Nombre, metrica.Unidad, orientacion,
            null, null, null, null, InterpretacionComparacion.NoComparable, pares, nota);
}
