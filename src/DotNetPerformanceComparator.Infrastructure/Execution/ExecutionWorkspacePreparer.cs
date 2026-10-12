using DotNetPerformanceComparator.Application.Evaluations.Execution;
using DotNetPerformanceComparator.Application.Migrations.ExperimentalMigration;
using DotNetPerformanceComparator.Domain.Constants;
using Microsoft.Extensions.Logging;

namespace DotNetPerformanceComparator.Infrastructure.Execution;

/// <summary>
/// Prepara un workspace temporal <c>%TEMP%/DotNetPerformanceComparator/&lt;guid&gt;/{net8,net10}</c>:
/// copia segura de cada versión (sin tocar los originales), <c>dotnet restore</c> y <c>dotnet publish</c>
/// en Release framework-dependent con el SDK explícito de cada major. Reutiliza la copia y el ejecutor
/// aislado de <c>dotnet</c> de C-05.
/// </summary>
public sealed class ExecutionWorkspacePreparer : IExecutionWorkspacePreparer
{
    private readonly IProjectCopyService _copia;
    private readonly IDotNetCommandRunner _dotnet;
    private readonly ILogger<ExecutionWorkspacePreparer> _logger;

    public ExecutionWorkspacePreparer(IProjectCopyService copia, IDotNetCommandRunner dotnet, ILogger<ExecutionWorkspacePreparer> logger)
    {
        _copia = copia;
        _dotnet = dotnet;
        _logger = logger;
    }

    public async Task<WorkspacePreparationResult> PrepareAsync(PrepareWorkspaceRequest request, Action<string>? onLog, CancellationToken cancellationToken = default)
    {
        void Log(string linea) => onLog?.Invoke(linea);

        if (string.IsNullOrWhiteSpace(request.RutaMigrada))
        {
            return new WorkspacePreparationResult(false, "La migración .NET 10 no tiene una ruta de copia registrada.", null);
        }

        var origenNet8 = Path.GetDirectoryName(Path.GetFullPath(request.RutaOriginalCsproj));
        if (origenNet8 is null || !Directory.Exists(origenNet8))
        {
            return new WorkspacePreparationResult(false, "No se encontró el directorio del proyecto original .NET 8.", null);
        }

        if (!Directory.Exists(request.RutaMigrada))
        {
            return new WorkspacePreparationResult(false, "No se encontró el directorio de la copia experimental .NET 10.", null);
        }

        var sdks = await _dotnet.RunAsync(["--list-sdks"], null, null, cancellationToken);
        if (!sdks.Succeeded)
        {
            return new WorkspacePreparationResult(false, "No fue posible consultar los SDK de .NET instalados.", null);
        }

        var sdk8 = DotNetSdkSelector.SelectLatestStable(sdks.Output, 8);
        var sdk10 = DotNetSdkSelector.SelectLatestStable(sdks.Output, 10);
        if (sdk8 is null)
        {
            return new WorkspacePreparationResult(false, "No se encontró un SDK .NET 8 estable instalado.", null);
        }

        if (sdk10 is null)
        {
            return new WorkspacePreparationResult(false, "No se encontró un SDK .NET 10 estable instalado.", null);
        }

        var root = Path.Combine(Path.GetTempPath(), "DotNetPerformanceComparator", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        Log($"Workspace temporal: {root}");

        try
        {
            var net8 = await PrepararVersionAsync(VersionesDotNet.Net8, VersionesDotNet.Net8Tfm, sdk8, origenNet8, Path.Combine(root, "net8"), Log, cancellationToken);
            if (net8.Error is not null)
            {
                return new WorkspacePreparationResult(false, net8.Error, null);
            }

            var net10 = await PrepararVersionAsync(VersionesDotNet.Net10, VersionesDotNet.Net10Tfm, sdk10, Path.GetFullPath(request.RutaMigrada), Path.Combine(root, "net10"), Log, cancellationToken);
            if (net10.Error is not null)
            {
                return new WorkspacePreparationResult(false, net10.Error, null);
            }

            return new WorkspacePreparationResult(true, null, new PreparedWorkspace(root, net8.Version!, net10.Version!));
        }
        catch (OperationCanceledException)
        {
            await CleanupAsync(new PreparedWorkspace(root, null!, null!), Log);
            throw;
        }
    }

    public Task CleanupAsync(PreparedWorkspace workspace, Action<string>? onLog)
    {
        try
        {
            if (Directory.Exists(workspace.RootDirectory))
            {
                Directory.Delete(workspace.RootDirectory, recursive: true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No fue posible eliminar el workspace temporal {Ruta}.", workspace.RootDirectory);
            onLog?.Invoke($"Advertencia: no fue posible eliminar el workspace temporal {workspace.RootDirectory}.");
        }

        return Task.CompletedTask;
    }

    private async Task<(PreparedVersion? Version, string? Error)> PrepararVersionAsync(
        string versionDotNet, string tfm, string sdk, string origen, string destinoBase, Action<string> log, CancellationToken cancellationToken)
    {
        log($"Preparando {versionDotNet} (SDK {sdk})...");
        var srcDir = Path.Combine(destinoBase, "src");
        var publishDir = Path.Combine(destinoBase, "publish");
        Directory.CreateDirectory(destinoBase);

        var destino = _copia.CheckDestination(origen, srcDir);
        if (!destino.IsValid)
        {
            return (null, $"No fue posible preparar la copia de {versionDotNet}.");
        }

        var copia = await _copia.CopyAsync(origen, destino, cancellationToken);
        if (!copia.Success)
        {
            return (null, $"No fue posible copiar {versionDotNet}. {copia.ErrorMessage}");
        }

        var csproj = Directory.EnumerateFiles(srcDir, "*.csproj", SearchOption.TopDirectoryOnly).FirstOrDefault();
        if (csproj is null)
        {
            return (null, $"No se encontró el archivo .csproj de {versionDotNet} en la copia.");
        }

        log($"> dotnet restore {versionDotNet}");
        var restore = await _dotnet.RunAsync(["restore", csproj, "--disable-build-servers"], sdk, log, cancellationToken);
        if (!restore.Succeeded)
        {
            return (null, $"La restauración de {versionDotNet} falló.");
        }

        log($"> dotnet publish {versionDotNet} -c Release -f {tfm}");
        var publish = await _dotnet.RunAsync(
            ["publish", csproj, "-c", "Release", "-f", tfm, "--no-restore", "--no-self-contained", "-o", publishDir, "--disable-build-servers"],
            sdk, log, cancellationToken);
        if (!publish.Succeeded)
        {
            return (null, $"La publicación de {versionDotNet} falló.");
        }

        if (!EntryAssemblyResolver.TryResolve(publishDir, out var entry, out var error))
        {
            return (null, $"{versionDotNet}: {error}");
        }

        // Tamaño del publish congelado aquí: tras publish correcto y antes de iniciar cualquier proceso
        // objetivo, para que los archivos de runtime (logs, SQLite, cachés) no alteren la medición (C-07.2).
        var tamanoBytes = PublishedSizeCalculator.CalculateBytes(publishDir);
        log($"{versionDotNet}: tamaño del publish {PublishedSizeCalculator.BytesToMb(tamanoBytes):0.##} MB ({tamanoBytes} bytes)");

        return (new PreparedVersion(versionDotNet, tfm, publishDir, entry, sdk, tamanoBytes), null);
    }
}
