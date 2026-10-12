using DotNetPerformanceComparator.Application.Evaluations.Execution;
using DotNetPerformanceComparator.Domain.Constants;

namespace DotNetPerformanceComparator.Web.Execution;

/// <summary>
/// Estado mutable y thread-safe de una evaluación activa. Lo escribe la Task de ejecución (vía
/// <see cref="ExecutionProgressSink"/>) y lo leen uno o más circuitos Blazor mediante <see cref="Snapshot"/>,
/// que siempre devuelve una copia inmutable. No contiene ninguna dependencia de UI (C-07.5).
/// </summary>
internal sealed class ActiveEvaluationSession
{
    private const int MaxLogs = 2000;

    private readonly object _lock = new();
    private readonly List<string> _logs = [];
    private readonly List<EjecucionResumen> _resultados = [];
    private readonly List<NivelEstresResumen> _niveles = [];
    private readonly LiveCpuSeries _cpu = new();

    private EstadoSesion _estado = EstadoSesion.Preparando;
    private string? _estadoConfig;
    private FaseEjecucion? _fase;
    private string? _version;
    private int? _numeroEjecucion;
    private int _orden;
    private int _total;
    private decimal? _cpuActual;
    private decimal? _ramActual;
    private int? _nivel;
    private int? _totalNiveles;
    private int? _usuariosNivel;
    private int? _ronda;
    private int? _totalRondas;
    private int? _usuariosConcurrentes;
    private string? _mensaje;
    private bool _cancelacionSolicitada;

    public ActiveEvaluationSession(int configuracionId)
    {
        ConfiguracionId = configuracionId;
        InicioUtc = DateTime.UtcNow;
    }

    public int ConfiguracionId { get; }
    public DateTime InicioUtc { get; }
    public CancellationTokenSource Cts { get; } = new();

    /// <summary>La Task de ejecución; la conserva el manager para observarla.</summary>
    public Task Task { get; set; } = Task.CompletedTask;

    public void Aplicar(EvaluationExecutionProgress p)
    {
        lock (_lock)
        {
            if (_estado == EstadoSesion.Preparando)
            {
                _estado = EstadoSesion.Ejecutando;
            }

            _estadoConfig = p.ConfiguracionEstado;
            if (p.Fase is { } fase) _fase = fase;
            if (p.VersionActual is not null) _version = p.VersionActual;
            if (p.RepeticionActual is not null) _numeroEjecucion = p.RepeticionActual;
            if (p.TotalPasos > 0) _total = p.TotalPasos;
            if (p.OrdenGlobal > 0) _orden = p.OrdenGlobal;
            if (p.CpuActual is not null) _cpuActual = p.CpuActual;
            if (p.RamActual is not null) _ramActual = p.RamActual;
            if (p.NivelActual is not null) _nivel = p.NivelActual;
            if (p.TotalNiveles is not null) _totalNiveles = p.TotalNiveles;
            if (p.UsuariosNivel is not null) _usuariosNivel = p.UsuariosNivel;
            if (p.RondaActual is not null) _ronda = p.RondaActual;
            if (p.TotalRondas is not null) _totalRondas = p.TotalRondas;
            if (p.UsuariosConcurrentes is not null) _usuariosConcurrentes = p.UsuariosConcurrentes;

            if (p.CpuActual is { } cpu)
            {
                // Serie transitoria para el LineChart; se reinicia al cambiar la Ejecucion (versión/repetición).
                _cpu.Observar(_version, _numeroEjecucion, cpu);
            }

            if (p.NivelFinalizado is { } nivel)
            {
                _niveles.Add(nivel);
                _cpuActual = null;
                _ramActual = null;
            }

            if (p.LogLine is not null)
            {
                if (_logs.Count >= MaxLogs) _logs.RemoveAt(0);
                _logs.Add(p.LogLine);
            }

            if (p.EjecucionFinalizada is { } resumen)
            {
                _resultados.Add(resumen);
                _cpuActual = null;
                _ramActual = null;
            }
        }
    }

    public void MarcarCancelacion()
    {
        lock (_lock)
        {
            _cancelacionSolicitada = true;
            if (_estado is EstadoSesion.Preparando or EstadoSesion.Ejecutando)
            {
                _estado = EstadoSesion.Cancelando;
            }
        }
    }

    public void Finalizar(string estadoPersistido, string mensaje)
    {
        lock (_lock)
        {
            _estado = estadoPersistido switch
            {
                EstadosConfiguracionEvaluacion.Finalizada => EstadoSesion.Finalizada,
                EstadosConfiguracionEvaluacion.Cancelada => EstadoSesion.Cancelada,
                _ => EstadoSesion.Fallida
            };
            _estadoConfig = estadoPersistido;
            _mensaje = mensaje;
            _fase = FaseEjecucion.Finalizado;
            _cpuActual = null;
            _ramActual = null;
        }
    }

    public EvaluationExecutionSessionSnapshot Snapshot()
    {
        lock (_lock)
        {
            return new EvaluationExecutionSessionSnapshot(
                ConfiguracionId, _estado, _estadoConfig, _fase, _version, _numeroEjecucion,
                _orden, _total, _cpuActual, _ramActual, _nivel, _totalNiveles, _usuariosNivel,
                _ronda, _totalRondas, _usuariosConcurrentes, _mensaje,
                _logs.ToArray(), _resultados.ToArray(), _niveles.ToArray(), _cpu.Snapshot(),
                InicioUtc, _cancelacionSolicitada);
        }
    }
}

/// <summary>
/// <see cref="IProgress{T}"/> propio (no usa <c>new Progress&lt;T&gt;()</c>, que capturaría el
/// SynchronizationContext del circuito Blazor). Solo actualiza el estado thread-safe de la sesión; no
/// conoce la UI ni invoca StateHasChanged (C-07.5).
/// </summary>
internal sealed class ExecutionProgressSink : IProgress<EvaluationExecutionProgress>
{
    private readonly ActiveEvaluationSession _sesion;

    public ExecutionProgressSink(ActiveEvaluationSession sesion) => _sesion = sesion;

    public void Report(EvaluationExecutionProgress value) => _sesion.Aplicar(value);
}
