using DotNetPerformanceComparator.Application.Evaluations.Comparison;
using DotNetPerformanceComparator.Application.Evaluations.Results;

namespace DotNetPerformanceComparator.Application.Evaluations.Reporting;

/// <summary>
/// Opciones del reporte comparativo PDF (CU-09). Define el título, qué secciones incluir, las observaciones
/// finales del Usuario y la fecha/hora de generación. Transitorio: no se persiste.
/// </summary>
public sealed record ReporteComparativoOpciones(
    string Titulo,
    bool IncluirDatosGenerales,
    bool IncluirResultadosPorEjecucion,
    bool IncluirPromediosConsolidado,
    bool IncluirTablaComparativa,
    bool IncluirGraficos,
    bool IncluirConclusionObservaciones,
    string? ObservacionesFinales,
    DateTime FechaGeneracion)
{
    /// <summary>Al menos una sección de contenido debe estar seleccionada (el encabezado/pie siempre existen).</summary>
    public bool AlgunaSeccion =>
        IncluirDatosGenerales || IncluirResultadosPorEjecucion || IncluirPromediosConsolidado ||
        IncluirTablaComparativa || IncluirGraficos || IncluirConclusionObservaciones;
}

/// <summary>Resultado de la generación: nombre de archivo seguro y contenido en memoria (sin filesystem).</summary>
public sealed record ReportePdfGenerado(string NombreArchivo, byte[] Contenido);

/// <summary>
/// Genera el reporte comparativo .NET 8 vs .NET 10 (CU-09) en memoria a partir de datos ya calculados por
/// CU-05/CU-07. La implementación (QuestPDF) vive en Infrastructure; Application no depende de QuestPDF.
/// </summary>
public interface IGeneradorReporteComparativo
{
    /// <summary>
    /// Construye el PDF. <paramref name="estres"/> solo aplica a evaluaciones de Estrés (null en otros tipos).
    /// Determinista para los mismos datos/opciones salvo <see cref="ReporteComparativoOpciones.FechaGeneracion"/>
    /// y metadatos propios del PDF. No accede a BD, filesystem, NBomber ni procesos.
    /// </summary>
    ReportePdfGenerado Generar(
        EvaluacionDetalleConsulta detalle,
        ComparacionResumen comparacion,
        ComparacionEstres? estres,
        ReporteComparativoOpciones opciones);
}
