using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using DotNetPerformanceComparator.Application.Migrations.ExperimentalMigration;

namespace DotNetPerformanceComparator.Infrastructure.Processes;

/// <summary>
/// Ejecuta <c>dotnet</c> con <see cref="Process"/>: sin shell, sin ventana, argumentos mediante
/// <see cref="ProcessStartInfo.ArgumentList"/>. Cada ejecución usa un directorio de trabajo temporal;
/// si se indica un SDK, se crea allí un <c>global.json</c> que lo fija, aislando la ejecución del
/// <c>global.json</c> (.NET 8) de la herramienta. El directorio temporal se elimina al terminar.
/// </summary>
public sealed class DotNetCommandRunner : IDotNetCommandRunner
{
    private const int MaxLineas = 5000;

    public async Task<DotNetCommandResult> RunAsync(
        IReadOnlyList<string> arguments,
        string? sdkVersion,
        Action<string>? onOutputLine = null,
        CancellationToken cancellationToken = default)
    {
        var trabajo = Directory.CreateTempSubdirectory("dnpc-dotnet-");
        try
        {
            if (sdkVersion is not null)
            {
                var globalJson = JsonSerializer.Serialize(new
                {
                    sdk = new { version = sdkVersion, rollForward = "disable", allowPrerelease = false }
                });
                await File.WriteAllTextAsync(Path.Combine(trabajo.FullName, "global.json"), globalJson, cancellationToken);
            }

            var startInfo = new ProcessStartInfo("dotnet")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = trabajo.FullName,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            foreach (var argumento in arguments)
            {
                startInfo.ArgumentList.Add(argumento);
            }

            PrepararEntorno(startInfo.Environment);

            var lineas = new List<string>();
            void Recibir(string? linea)
            {
                if (linea is null)
                {
                    return;
                }

                lock (lineas)
                {
                    if (lineas.Count < MaxLineas)
                    {
                        lineas.Add(linea);
                    }
                }

                onOutputLine?.Invoke(linea);
            }

            using var proceso = new Process { StartInfo = startInfo };
            proceso.OutputDataReceived += (_, e) => Recibir(e.Data);
            proceso.ErrorDataReceived += (_, e) => Recibir(e.Data);

            try
            {
                if (!proceso.Start())
                {
                    return new DotNetCommandResult(false, -1, []);
                }
            }
            catch (Win32Exception)
            {
                return new DotNetCommandResult(false, -1, []);
            }

            proceso.BeginOutputReadLine();
            proceso.BeginErrorReadLine();

            try
            {
                await proceso.WaitForExitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                try
                {
                    proceso.Kill(entireProcessTree: true);
                }
                catch (InvalidOperationException)
                {
                    // El proceso ya terminó.
                }

                throw;
            }

            proceso.WaitForExit(); // Garantiza que se procesó toda la salida asíncrona.

            lock (lineas)
            {
                return new DotNetCommandResult(true, proceso.ExitCode, lineas.ToList());
            }
        }
        finally
        {
            try
            {
                trabajo.Delete(recursive: true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Directorio temporal: si no puede eliminarse ahora, lo limpiará el sistema.
            }
        }
    }

    /// <summary>
    /// Evita heredar variables de MSBuild/SDK del proceso de la herramienta (que podrían apuntar al SDK .NET 8)
    /// y fuerza una salida UTF-8 sin mensajes de bienvenida ni telemetría.
    /// </summary>
    private static void PrepararEntorno(IDictionary<string, string?> entorno)
    {
        foreach (var clave in entorno.Keys.ToList())
        {
            if (clave.StartsWith("MSBuild", StringComparison.OrdinalIgnoreCase)
                || clave.Equals("DOTNET_HOST_PATH", StringComparison.OrdinalIgnoreCase))
            {
                entorno.Remove(clave);
            }
        }

        entorno["DOTNET_NOLOGO"] = "1";
        entorno["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        entorno["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "1";
        entorno["DOTNET_CLI_FORCE_UTF8_ENCODING"] = "1";
    }
}
