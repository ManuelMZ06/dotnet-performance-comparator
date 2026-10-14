using System.Globalization;
using DotNetPerformanceComparator.Application.Evaluations.Results;
using DotNetPerformanceComparator.Web.Components.Shared;

namespace DotNetPerformanceComparator.Web.Components.Pages;

/// <summary>
/// Estado y lógica de presentación de M-06 (CU-05, Consultar resultados). Solo lectura: consume
/// <see cref="IConsultaResultadosService"/>; no accede a EF, no recalcula, no compara ni filtra. Testeable
/// sin framework de UI. Garantiza que, al cambiar rápidamente de evaluación, solo la última selección
/// escriba el detalle (las respuestas tardías de selecciones previas se descartan).
/// </summary>
public sealed class ConsultaResultadosModelo
{
    private readonly IConsultaResultadosService _servicio;
    private readonly ILogger _logger;
    private int _ticketSeleccion;

    public ConsultaResultadosModelo(IConsultaResultadosService servicio, ILogger logger)
    {
        _servicio = servicio;
        _logger = logger;
    }

    public bool CargandoListado { get; private set; }
    public bool CargandoDetalle { get; private set; }
    public IReadOnlyList<EvaluacionResumenConsulta> Evaluaciones { get; private set; } = [];
    public int? SeleccionId { get; private set; }
    public EvaluacionDetalleConsulta? Detalle { get; private set; }

    /// <summary>true cuando se seleccionó una evaluación que ya no existe.</summary>
    public bool EvaluacionNoEncontrada { get; private set; }

    public string? Mensaje { get; private set; }
    public OperationMessageKind MensajeTipo { get; private set; }

    public bool ListadoVacio => !CargandoListado && Evaluaciones.Count == 0;

    public async Task CargarListadoAsync(CancellationToken cancellationToken = default)
    {
        CargandoListado = true;
        Mensaje = null;
        try
        {
            Evaluaciones = await _servicio.ObtenerEvaluacionesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No fue posible cargar el listado de evaluaciones.");
            Evaluaciones = [];
            MostrarMensaje(OperationMessageKind.Error, "No fue posible cargar las evaluaciones. Inténtelo nuevamente.");
        }
        finally
        {
            CargandoListado = false;
        }
    }

    public async Task SeleccionarAsync(int configuracionId, CancellationToken cancellationToken = default)
    {
        SeleccionId = configuracionId;
        Detalle = null;
        EvaluacionNoEncontrada = false;
        Mensaje = null;
        CargandoDetalle = true;
        var ticket = ++_ticketSeleccion;

        try
        {
            var detalle = await _servicio.ObtenerDetalleAsync(configuracionId, cancellationToken);
            if (ticket != _ticketSeleccion)
            {
                return; // llegó tarde: el Usuario ya seleccionó otra evaluación.
            }

            Detalle = detalle;
            EvaluacionNoEncontrada = detalle is null;
        }
        catch (Exception ex)
        {
            if (ticket != _ticketSeleccion)
            {
                return;
            }

            _logger.LogError(ex, "No fue posible cargar el detalle de la evaluación {ConfiguracionId}.", configuracionId);
            MostrarMensaje(OperationMessageKind.Error, "No fue posible cargar el detalle de la evaluación.");
        }
        finally
        {
            if (ticket == _ticketSeleccion)
            {
                CargandoDetalle = false;
            }
        }
    }

    public void CerrarDetalle()
    {
        _ticketSeleccion++; // invalida cualquier carga en curso
        SeleccionId = null;
        Detalle = null;
        EvaluacionNoEncontrada = false;
        CargandoDetalle = false;
    }

    private void MostrarMensaje(OperationMessageKind tipo, string mensaje)
    {
        MensajeTipo = tipo;
        Mensaje = mensaje;
    }

    // ----------------------------------------------------------------- formato (presentación)

    /// <summary>
    /// Formato de valor numérico: hasta 4 decimales sin ceros de relleno (p. ej. 0.0647, 85.2373, 7.9778),
    /// en cultura invariante. No altera el valor persistido; solo mejora la legibilidad.
    /// </summary>
    public static string FormatoValor(decimal valor) => valor.ToString("0.####", CultureInfo.InvariantCulture);

    public static string ValorConUnidad(ResultadoMetricaConsulta r) =>
        string.IsNullOrEmpty(r.Unidad) ? FormatoValor(r.Valor) : $"{FormatoValor(r.Valor)} {r.Unidad}";

    public static string FechaCorta(DateTime fechaUtc) =>
        fechaUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);

    public static string EstadoEvaluacionTexto(string estado) => estado switch
    {
        "Configurada" => "Configurada",
        "EnEjecucion" => "En ejecución",
        "Finalizada" => "Finalizada",
        "Fallida" => "Fallida",
        "Cancelada" => "Cancelada",
        _ => estado
    };

    public static string EstadoEjecucionTexto(string estado) => estado switch
    {
        "EnProceso" => "En proceso",
        "Completada" => "Completada",
        "CompletadaConSaturacion" => "Completada con saturación",
        "Fallida" => "Fallida",
        "Cancelada" => "Cancelada",
        _ => estado
    };

    public static string EstadoNivelTexto(string estado) => estado switch
    {
        "EnProceso" => "En proceso",
        "Completado" => "Completado",
        "Saturado" => "Saturado",
        "Fallido" => "Fallido",
        "Cancelado" => "Cancelado",
        _ => estado
    };
}
