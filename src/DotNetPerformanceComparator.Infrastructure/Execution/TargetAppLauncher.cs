using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using DotNetPerformanceComparator.Application.Evaluations.Execution;
using DotNetPerformanceComparator.Domain.Constants;
using Microsoft.Extensions.Logging;

namespace DotNetPerformanceComparator.Infrastructure.Execution;

/// <summary>
/// Inicia la aplicación objetivo publicada con <c>dotnet &lt;entry&gt;.dll</c> en un puerto local libre,
/// sin shell ni ventana. No modifica appsettings/launchSettings/código del proyecto.
/// </summary>
public sealed class TargetAppLauncher : ITargetAppLauncher
{
    private readonly ILoggerFactory _loggerFactory;

    public TargetAppLauncher(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    public Task<ITargetApp> StartAsync(PreparedVersion version, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var puerto = PuertoLibre();
        var baseUrl = new Uri($"http://127.0.0.1:{puerto}/");

        var startInfo = new ProcessStartInfo("dotnet")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = version.PublishDirectory
        };
        startInfo.ArgumentList.Add(version.EntryAssemblyPath);
        startInfo.Environment["ASPNETCORE_URLS"] = baseUrl.ToString().TrimEnd('/');
        // Forzado explícito: la app objetivo no debe heredar el entorno (p. ej. Development) del evaluador.
        startInfo.Environment["DOTNET_ENVIRONMENT"] = ClavesCondicionEjecucion.EntornoObjetivo;
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = ClavesCondicionEjecucion.EntornoObjetivo;
        startInfo.Environment["DOTNET_NOLOGO"] = "1";

        var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        // Se consume la salida para que los buffers no bloqueen el proceso objetivo.
        process.OutputDataReceived += (_, _) => { };
        process.ErrorDataReceived += (_, _) => { };
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        ITargetApp app = new TargetApp(process, baseUrl, _loggerFactory.CreateLogger<TargetApp>());
        return Task.FromResult(app);
    }

    private static int PuertoLibre()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var puerto = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return puerto;
    }
}
