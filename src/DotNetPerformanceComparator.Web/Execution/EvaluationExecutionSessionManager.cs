using DotNetPerformanceComparator.Application.Evaluations.Execution;
using DotNetPerformanceComparator.Domain.Constants;

namespace DotNetPerformanceComparator.Web.Execution;

public sealed record SessionStartResult(bool Iniciada, string Mensaje);

/// <summary>
/// Gestor server-side del ciclo de vida de una evaluación (C-07.5). La evaluación pertenece al host Web,
/// no al circuito Blazor: cerrar/recargar/desconectar el navegador no la cancela ni reinicia. Registrado
/// como singleton; crea un scope propio por evaluación (no captura dependencias scoped) y es dueño del
/// <see cref="CancellationTokenSource"/>. Solo una sesión activa a la vez (además del gate del motor).
/// </summary>
public interface IEvaluationExecutionSessionManager
{
    Task<SessionStartResult> StartAsync(int configuracionId);

    EvaluationExecutionSessionSnapshot? GetSnapshot(int configuracionId);

    /// <summary>Solicita cancelar la evaluación activa de esa configuración; idempotente. Devuelve false si no hay una activa.</summary>
    bool RequestCancellation(int configuracionId);
}

public sealed class EvaluationExecutionSessionManager : IEvaluationExecutionSessionManager
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EvaluationExecutionSessionManager> _logger;
    private readonly object _lock = new();

    private ActiveEvaluationSession? _activa;
    private EvaluationExecutionSessionSnapshot? _ultima;

    public EvaluationExecutionSessionManager(IServiceScopeFactory scopeFactory, ILogger<EvaluationExecutionSessionManager> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task<SessionStartResult> StartAsync(int configuracionId)
    {
        ActiveEvaluationSession sesion;
        lock (_lock)
        {
            if (_activa is { } a && a.Snapshot().Activa)
            {
                return Task.FromResult(new SessionStartResult(false, "Ya existe una evaluación de rendimiento en ejecución."));
            }

            _ultima = null; // el último snapshot transitorio se descarta al iniciar otra evaluación
            sesion = new ActiveEvaluationSession(configuracionId);
            _activa = sesion;
        }

        // La Task se conserva y se observa; no es async void.
        sesion.Task = EjecutarYObservarAsync(sesion);
        return Task.FromResult(new SessionStartResult(true, "Evaluación iniciada."));
    }

    private async Task EjecutarYObservarAsync(ActiveEvaluationSession sesion)
    {
        var scope = _scopeFactory.CreateAsyncScope();
        try
        {
            var servicio = scope.ServiceProvider.GetRequiredService<IEjecutarEvaluacionService>();
            var sink = new ExecutionProgressSink(sesion);
            var resultado = await servicio.EjecutarAsync(
                new EjecutarEvaluacionRequest(sesion.ConfiguracionId), sink, sesion.Cts.Token);
            sesion.Finalizar(resultado.EstadoFinal, resultado.Mensaje);
        }
        catch (OperationCanceledException)
        {
            sesion.Finalizar(EstadosConfiguracionEvaluacion.Cancelada, "La evaluación fue cancelada.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado en la sesión de evaluación {ConfiguracionId}.", sesion.ConfiguracionId);
            sesion.Finalizar(EstadosConfiguracionEvaluacion.Fallida, "Ocurrió un error inesperado durante la evaluación.");
        }
        finally
        {
            lock (_lock)
            {
                _ultima = sesion.Snapshot();
                if (ReferenceEquals(_activa, sesion))
                {
                    _activa = null;
                }
            }

            sesion.Cts.Dispose();
            await scope.DisposeAsync();
        }
    }

    public EvaluationExecutionSessionSnapshot? GetSnapshot(int configuracionId)
    {
        lock (_lock)
        {
            if (_activa is { } a && a.ConfiguracionId == configuracionId)
            {
                return a.Snapshot();
            }

            return _ultima is { } u && u.ConfiguracionId == configuracionId ? u : null;
        }
    }

    public bool RequestCancellation(int configuracionId)
    {
        lock (_lock)
        {
            if (_activa is { } a && a.ConfiguracionId == configuracionId && a.Snapshot().PuedeCancelar)
            {
                a.MarcarCancelacion();
                try
                {
                    a.Cts.Cancel();
                }
                catch (ObjectDisposedException)
                {
                    // La evaluación terminó justo antes de cancelar; no hay nada que cancelar.
                }

                return true;
            }

            return false;
        }
    }
}
