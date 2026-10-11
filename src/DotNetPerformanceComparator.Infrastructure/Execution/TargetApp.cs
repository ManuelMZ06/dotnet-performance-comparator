using System.Diagnostics;
using DotNetPerformanceComparator.Application.Evaluations.Execution;
using Microsoft.Extensions.Logging;

namespace DotNetPerformanceComparator.Infrastructure.Execution;

/// <summary>
/// Proceso de la aplicación objetivo en ejecución. Expone readiness HTTP y muestreo de recursos del
/// proceso; al liberarse termina el árbol de procesos. No modifica el proyecto objetivo.
/// </summary>
internal sealed class TargetApp : ITargetApp
{
    private static readonly TimeSpan ReadinessTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan ReadinessInterval = TimeSpan.FromMilliseconds(500);

    private readonly Process _process;
    private readonly ILogger _logger;
    private bool _disposed;

    public TargetApp(Process process, Uri baseUrl, ILogger logger)
    {
        _process = process;
        BaseUrl = baseUrl;
        _logger = logger;
    }

    public Uri BaseUrl { get; }

    public bool HasExited
    {
        get
        {
            try
            {
                return _process.HasExited;
            }
            catch (InvalidOperationException)
            {
                return true;
            }
        }
    }

    public async Task<bool> WaitUntilReadyAsync(string rutaObjetivo, CancellationToken cancellationToken = default)
    {
        using var http = new HttpClient { BaseAddress = BaseUrl, Timeout = TimeSpan.FromSeconds(5) };
        var destino = new Uri(BaseUrl, rutaObjetivo);
        var inicio = Stopwatch.GetTimestamp();

        while (Stopwatch.GetElapsedTime(inicio) < ReadinessTimeout)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (HasExited)
            {
                return false;
            }

            try
            {
                using var respuesta = await http.GetAsync(destino, cancellationToken);
                if (respuesta.IsSuccessStatusCode)
                {
                    return true;
                }
            }
            catch (HttpRequestException)
            {
                // El servidor aún no acepta conexiones.
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // Timeout de la solicitud individual; se reintenta.
            }

            await Task.Delay(ReadinessInterval, cancellationToken);
        }

        return false;
    }

    public ResourceSample? Sample()
    {
        try
        {
            if (_process.HasExited)
            {
                return null;
            }

            _process.Refresh();
            var cpuMs = _process.TotalProcessorTime.TotalMilliseconds;
            var workingSet = _process.WorkingSet64;
            var wallMs = Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency * 1000.0;
            return new ResourceSample(cpuMs, workingSet, wallMs);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    public ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return ValueTask.CompletedTask;
        }

        _disposed = true;
        try
        {
            if (!_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
                _process.WaitForExit(5000);
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            _logger.LogWarning(ex, "No fue posible detener limpiamente la aplicación objetivo.");
        }
        finally
        {
            _process.Dispose();
        }

        return ValueTask.CompletedTask;
    }
}
