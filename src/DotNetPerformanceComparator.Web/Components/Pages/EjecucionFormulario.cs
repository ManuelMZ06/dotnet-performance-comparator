using DotNetPerformanceComparator.Application.Evaluations.Execution;
using DotNetPerformanceComparator.Web.Components.Shared;

namespace DotNetPerformanceComparator.Web.Components.Pages;

/// <summary>
/// Estado e interacción de M-05 (CU-04, C-07.1): muestra la cabecera de la configuración, inicia la
/// evaluación bajo demanda y refleja el progreso (fase, versión, repetición, CPU/RAM y resultados).
/// </summary>
public sealed class EjecucionFormulario
{
    private const int MaxLineasBitacora = 2000;

    private readonly IEjecutarEvaluacionService _servicio;
    private readonly ILogger _logger;
    private readonly Func<Action, Task> _despachar;
    private readonly List<string> _bitacora = [];
    private readonly List<EjecucionResumen> _resultados = [];

    public EjecucionFormulario(IEjecutarEvaluacionService servicio, ILogger logger, Func<Action, Task> despachar)
    {
        _servicio = servicio;
        _logger = logger;
        _despachar = despachar;
    }

    public ConfiguracionEjecutableInfo? Info { get; private set; }
    public bool Ejecutando { get; private set; }
    public bool Finalizado { get; private set; }
    public string? ConfiguracionEstado { get; private set; }
    public FaseEjecucion? Fase { get; private set; }
    public string? VersionActual { get; private set; }
    public int? RepeticionActual { get; private set; }
    public int OrdenGlobal { get; private set; }
    public int TotalPasos { get; private set; }
    public decimal? CpuActual { get; private set; }
    public decimal? RamActual { get; private set; }
    public IReadOnlyList<string> Bitacora => _bitacora;
    public IReadOnlyList<EjecucionResumen> Resultados => _resultados;
    public string? Mensaje { get; private set; }
    public OperationMessageKind MensajeTipo { get; private set; }

    public bool PuedeIniciar => Info is { PuedeEjecutar: true } && !Ejecutando && !Finalizado;

    public double ProgresoPorcentaje => TotalPasos == 0 ? 0 : Math.Round(OrdenGlobal * 100.0 / TotalPasos);

    public async Task CargarAsync(int configuracionId)
    {
        try
        {
            Info = await _servicio.ObtenerInfoAsync(configuracionId);
            ConfiguracionEstado = Info?.Estado;
            if (Info is null)
            {
                MostrarMensaje(OperationMessageKind.Error, "La configuración indicada no existe.");
            }
            else if (!Info.PuedeEjecutar)
            {
                MostrarMensaje(OperationMessageKind.Info, Info.MotivoNoEjecutable ?? "La configuración no puede ejecutarse.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No fue posible cargar la información de ejecución.");
            MostrarMensaje(OperationMessageKind.Error, "No fue posible cargar la información de la evaluación.");
        }
    }

    public async Task IniciarAsync(int configuracionId, CancellationToken cancellationToken = default)
    {
        if (!PuedeIniciar)
        {
            return;
        }

        Ejecutando = true;
        Mensaje = null;
        _bitacora.Clear();
        _resultados.Clear();
        OrdenGlobal = 0;
        TotalPasos = 0;

        try
        {
            var resultado = await _servicio.EjecutarAsync(
                new EjecutarEvaluacionRequest(configuracionId),
                new Progreso(this),
                cancellationToken);

            Finalizado = resultado.Exito;
            ConfiguracionEstado = string.IsNullOrEmpty(resultado.EstadoFinal) ? ConfiguracionEstado : resultado.EstadoFinal;
            MostrarMensaje(resultado.Exito ? OperationMessageKind.Success : OperationMessageKind.Error, resultado.Mensaje);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al ejecutar la evaluación.");
            MostrarMensaje(OperationMessageKind.Error, "Ocurrió un error inesperado durante la evaluación.");
        }
        finally
        {
            Ejecutando = false;
            Fase = null;
        }
    }

    private void Aplicar(EvaluationExecutionProgress progreso)
    {
        ConfiguracionEstado = progreso.ConfiguracionEstado;
        if (progreso.Fase is { } fase)
        {
            Fase = fase;
        }

        if (progreso.VersionActual is not null)
        {
            VersionActual = progreso.VersionActual;
        }

        if (progreso.RepeticionActual is not null)
        {
            RepeticionActual = progreso.RepeticionActual;
        }

        if (progreso.TotalPasos > 0)
        {
            TotalPasos = progreso.TotalPasos;
        }

        if (progreso.OrdenGlobal > 0)
        {
            OrdenGlobal = progreso.OrdenGlobal;
        }

        if (progreso.CpuActual is not null)
        {
            CpuActual = progreso.CpuActual;
        }

        if (progreso.RamActual is not null)
        {
            RamActual = progreso.RamActual;
        }

        if (progreso.LogLine is not null)
        {
            if (_bitacora.Count >= MaxLineasBitacora)
            {
                _bitacora.RemoveAt(0);
            }

            _bitacora.Add(progreso.LogLine);
        }

        if (progreso.EjecucionFinalizada is { } resumen)
        {
            _resultados.Add(resumen);
            CpuActual = null;
            RamActual = null;
        }
    }

    private void MostrarMensaje(OperationMessageKind tipo, string mensaje)
    {
        MensajeTipo = tipo;
        Mensaje = mensaje;
    }

    private sealed class Progreso(EjecucionFormulario formulario) : IProgress<EvaluationExecutionProgress>
    {
        public void Report(EvaluationExecutionProgress value) => _ = formulario._despachar(() => formulario.Aplicar(value));
    }
}
