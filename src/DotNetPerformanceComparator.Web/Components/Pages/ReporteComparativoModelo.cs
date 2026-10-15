using DotNetPerformanceComparator.Application.Evaluations.Comparison;
using DotNetPerformanceComparator.Application.Evaluations.Reporting;
using DotNetPerformanceComparator.Application.Evaluations.Results;
using DotNetPerformanceComparator.Domain.Constants;
using DotNetPerformanceComparator.Web.Components.Shared;

namespace DotNetPerformanceComparator.Web.Components.Pages;

/// <summary>
/// Estado y lógica de M-08 (CU-09, Generar reporte PDF). Reutiliza CU-05 (datos) y CU-07 (comparación) y
/// delega la generación en <see cref="IGeneradorReporteComparativo"/> (Infrastructure/QuestPDF). El reporte
/// representa el experimento completo (independiente del filtro visual de CU-06). El PDF generado se guarda
/// en memoria y se invalida al cambiar cualquier opción, evitando descargar una versión obsoleta.
/// </summary>
public sealed class ReporteComparativoModelo
{
    public const int TituloMaximo = 150;
    public const int ObservacionesMaximo = 2000;

    private readonly IConsultaResultadosService _consulta;
    private readonly IComparacionResultadosService _comparacion;
    private readonly IGeneradorReporteComparativo _generador;
    private readonly ILogger _logger;

    public ReporteComparativoModelo(
        IConsultaResultadosService consulta,
        IComparacionResultadosService comparacion,
        IGeneradorReporteComparativo generador,
        ILogger logger)
    {
        _consulta = consulta;
        _comparacion = comparacion;
        _generador = generador;
        _logger = logger;
    }

    public bool CargandoListado { get; private set; }
    public bool CargandoDetalle { get; private set; }
    public IReadOnlyList<EvaluacionResumenConsulta> Evaluaciones { get; private set; } = [];
    public int? SeleccionId { get; private set; }
    public EvaluacionDetalleConsulta? Detalle { get; private set; }
    public ComparacionResumen? Resumen { get; private set; }
    public ComparacionEstres? Estres { get; private set; }
    public bool EsEstres => Estres is not null;

    public string Titulo { get; private set; } = string.Empty;
    public bool IncluirDatosGenerales { get; private set; } = true;
    public bool IncluirResultadosPorEjecucion { get; private set; } = true;
    public bool IncluirPromediosConsolidado { get; private set; } = true;
    public bool IncluirTablaComparativa { get; private set; } = true;
    public bool IncluirGraficos { get; private set; } = true;
    public bool IncluirConclusionObservaciones { get; private set; } = true;
    public string? ObservacionesFinales { get; private set; }

    /// <summary>PDF generado con la configuración actual; null si aún no se generó o se invalidó.</summary>
    public ReportePdfGenerado? PdfGenerado { get; private set; }

    public string? Mensaje { get; private set; }
    public OperationMessageKind MensajeTipo { get; private set; }

    // ----------------------------------------------------------------- validación

    public bool TituloValido => !string.IsNullOrWhiteSpace(Titulo) && Titulo.Trim().Length <= TituloMaximo;
    public bool ObservacionesValidas => (ObservacionesFinales?.Length ?? 0) <= ObservacionesMaximo;
    public bool AlMenosUnaSeccion =>
        IncluirDatosGenerales || IncluirResultadosPorEjecucion || IncluirPromediosConsolidado ||
        IncluirTablaComparativa || IncluirGraficos || IncluirConclusionObservaciones;

    public bool PuedeGenerar => Detalle is not null && TituloValido && ObservacionesValidas && AlMenosUnaSeccion;
    public bool PuedeDescargar => PdfGenerado is not null;

    /// <summary>true cuando la evaluación existe pero no tiene ninguna comparación válida (reporte informativo).</summary>
    public bool SinComparacion =>
        Detalle is not null &&
        !(Resumen?.EsComparable ?? false) &&
        !(Estres?.Niveles.Any(n => n.NivelComparable) ?? false);

    // ----------------------------------------------------------------- carga

    public async Task CargarListadoAsync(int? configuracionId, CancellationToken cancellationToken = default)
    {
        CargandoListado = true;
        Mensaje = null;
        try
        {
            Evaluaciones = await _consulta.ObtenerEvaluacionesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No fue posible cargar las evaluaciones para el reporte.");
            Evaluaciones = [];
            MostrarMensaje(OperationMessageKind.Error, "No fue posible cargar las evaluaciones.");
        }
        finally
        {
            CargandoListado = false;
        }

        if (configuracionId is { } id && Evaluaciones.Any(e => e.Id == id))
        {
            await SeleccionarAsync(id, cancellationToken);
        }
    }

    public async Task SeleccionarAsync(int? configuracionId, CancellationToken cancellationToken = default)
    {
        SeleccionId = configuracionId;
        Detalle = null;
        Resumen = null;
        Estres = null;
        PdfGenerado = null;
        Mensaje = null;

        if (configuracionId is not { } id)
        {
            return;
        }

        CargandoDetalle = true;
        try
        {
            var detalle = await _consulta.ObtenerDetalleAsync(id, cancellationToken);
            if (detalle is null)
            {
                SeleccionId = null;
                MostrarMensaje(OperationMessageKind.Warning, "La evaluación seleccionada ya no existe.");
                return;
            }

            Detalle = detalle;
            if (detalle.TipoPrueba == TiposPruebaConocidos.Estres)
            {
                Estres = _comparacion.CompararEstres(detalle);
                Resumen = Estres.Resumen;
            }
            else
            {
                Resumen = _comparacion.CompararResumen(detalle);
            }

            Titulo = $"Reporte comparativo - {detalle.Nombre}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No fue posible preparar el reporte de la evaluación {ConfiguracionId}.", id);
            Detalle = null;
            MostrarMensaje(OperationMessageKind.Error, "No fue posible preparar el reporte de la evaluación.");
        }
        finally
        {
            CargandoDetalle = false;
        }
    }

    // ----------------------------------------------------------------- opciones (invalidan el PDF)

    public void EstablecerTitulo(string? titulo) { Titulo = titulo ?? string.Empty; InvalidarPdf(); }
    public void EstablecerObservaciones(string? texto) { ObservacionesFinales = string.IsNullOrWhiteSpace(texto) ? null : texto; InvalidarPdf(); }

    public void AlternarSeccion(string clave, bool valor)
    {
        switch (clave)
        {
            case nameof(IncluirDatosGenerales): IncluirDatosGenerales = valor; break;
            case nameof(IncluirResultadosPorEjecucion): IncluirResultadosPorEjecucion = valor; break;
            case nameof(IncluirPromediosConsolidado): IncluirPromediosConsolidado = valor; break;
            case nameof(IncluirTablaComparativa): IncluirTablaComparativa = valor; break;
            case nameof(IncluirGraficos): IncluirGraficos = valor; break;
            case nameof(IncluirConclusionObservaciones): IncluirConclusionObservaciones = valor; break;
        }

        InvalidarPdf();
    }

    private void InvalidarPdf()
    {
        PdfGenerado = null;
        if (MensajeTipo == OperationMessageKind.Success)
        {
            Mensaje = null;
        }
    }

    // ----------------------------------------------------------------- generación

    public void GenerarPdf()
    {
        if (Detalle is null)
        {
            MostrarMensaje(OperationMessageKind.Warning, "Seleccione una evaluación antes de generar el reporte.");
            return;
        }

        if (!TituloValido)
        {
            MostrarMensaje(OperationMessageKind.Warning, "El título del reporte es obligatorio (máximo 150 caracteres).");
            return;
        }

        if (!AlMenosUnaSeccion)
        {
            MostrarMensaje(OperationMessageKind.Warning, "Seleccione al menos una sección de contenido.");
            return;
        }

        if (!ObservacionesValidas)
        {
            MostrarMensaje(OperationMessageKind.Warning, "Las observaciones finales superan el máximo permitido.");
            return;
        }

        try
        {
            var opciones = new ReporteComparativoOpciones(
                Titulo.Trim(),
                IncluirDatosGenerales,
                IncluirResultadosPorEjecucion,
                IncluirPromediosConsolidado,
                IncluirTablaComparativa,
                IncluirGraficos,
                IncluirConclusionObservaciones,
                ObservacionesFinales,
                DateTime.Now);

            PdfGenerado = _generador.Generar(Detalle, Resumen!, Estres, opciones);
            var kb = Math.Max(1, PdfGenerado.Contenido.Length / 1024);
            MostrarMensaje(OperationMessageKind.Success, $"Reporte generado: {PdfGenerado.NombreArchivo} ({kb} KB). Ya puede descargarlo.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falló la generación del reporte PDF de la evaluación {ConfiguracionId}.", Detalle.Id);
            PdfGenerado = null;
            MostrarMensaje(OperationMessageKind.Error, "No fue posible generar el reporte PDF.");
        }
    }

    public string DestinoCancelar() => SeleccionId is { } id ? $"/comparacion/{id}" : "/resultados";

    private void MostrarMensaje(OperationMessageKind tipo, string mensaje)
    {
        MensajeTipo = tipo;
        Mensaje = mensaje;
    }
}
