namespace DotNetPerformanceComparator.Application.Evaluations.Execution;

/// <summary>
/// Exclusión global: solo una evaluación de rendimiento puede estar activa en la herramienta.
/// </summary>
public interface IEvaluationExecutionGate
{
    /// <summary>Adquiere el acceso exclusivo; devuelve false si ya hay una evaluación activa.</summary>
    bool TryAcquire();

    void Release();
}

/// <summary>Una versión preparada (copiada, restaurada y publicada) lista para ejecutarse.</summary>
/// <param name="VersionDotNet">Etiqueta de dominio (.NET 8 / .NET 10).</param>
/// <param name="TargetFramework">TFM (net8.0 / net10.0).</param>
/// <param name="PublishDirectory">Carpeta de publish framework-dependent (Release).</param>
/// <param name="EntryAssemblyPath">DLL de entrada determinado desde el runtimeconfig.</param>
/// <param name="SdkVersion">SDK utilizado para restore/publish.</param>
/// <param name="PublishedSizeBytes">
/// Suma recursiva, en bytes, de todos los archivos del directorio de publish. Se congela inmediatamente
/// después de un publish correcto y antes de iniciar cualquier proceso objetivo (C-07.2).
/// </param>
public sealed record PreparedVersion(string VersionDotNet, string TargetFramework, string PublishDirectory, string EntryAssemblyPath, string SdkVersion, long PublishedSizeBytes);

public sealed record PreparedWorkspace(string RootDirectory, PreparedVersion Net8, PreparedVersion Net10);

/// <param name="RutaOriginalCsproj">Ruta del .csproj original .NET 8 (<c>ProyectoDotNet.RutaOriginal</c>).</param>
/// <param name="RutaMigrada">Directorio de la copia experimental .NET 10 (<c>Migracion.RutaMigrada</c>).</param>
public sealed record PrepareWorkspaceRequest(string RutaOriginalCsproj, string? RutaMigrada);

public sealed record WorkspacePreparationResult(bool Success, string? ErrorMessage, PreparedWorkspace? Workspace);

/// <summary>
/// Prepara en un workspace temporal ambas versiones (copia segura + restore + publish Release
/// framework-dependent), sin tocar los proyectos originales. Reutiliza la política de copia y el
/// ejecutor aislado de <c>dotnet</c> de C-05.
/// </summary>
public interface IExecutionWorkspacePreparer
{
    Task<WorkspacePreparationResult> PrepareAsync(PrepareWorkspaceRequest request, Action<string>? onLog, CancellationToken cancellationToken = default);

    /// <summary>Elimina el workspace temporal. Nunca lanza; registra en el log si no puede limpiarse.</summary>
    Task CleanupAsync(PreparedWorkspace workspace, Action<string>? onLog);
}

/// <summary>Aplicación objetivo en ejecución (un proceso nuevo por cada <c>Ejecucion</c>).</summary>
public interface ITargetApp : IAsyncDisposable
{
    Uri BaseUrl { get; }

    bool HasExited { get; }

    /// <summary>Repite GET a BaseUrl + rutaObjetivo hasta una respuesta 2xx, el timeout o que muera el proceso.</summary>
    Task<bool> WaitUntilReadyAsync(string rutaObjetivo, CancellationToken cancellationToken = default);

    /// <summary>Muestra actual de recursos del proceso objetivo; null si el proceso ya terminó.</summary>
    ResourceSample? Sample();
}

public interface ITargetAppLauncher
{
    /// <summary>Inicia <c>dotnet &lt;entry&gt;.dll</c> en un puerto local libre. No modifica el proyecto.</summary>
    Task<ITargetApp> StartAsync(PreparedVersion version, CancellationToken cancellationToken = default);
}

/// <param name="Ramping">true: sube de forma progresiva hasta <paramref name="Usuarios"/> (fase ramp-up).</param>
public sealed record LoadPhaseRequest(Uri BaseUrl, string RutaObjetivo, int Usuarios, TimeSpan Duracion, int IntervaloSolicitudesMs, bool Ramping);

/// <param name="Started">El motor de carga pudo iniciarse.</param>
/// <param name="TiempoRespuestaMedioMs">Media (Mean) de latencia de las solicitudes exitosas del paso medido.</param>
/// <param name="LatenciaP95Ms">Percentil 95 (P95) de latencia de las solicitudes exitosas del paso medido.</param>
/// <param name="ThroughputExitosoRps">Solicitudes exitosas por segundo (RPS) del paso medido.</param>
public sealed record LoadPhaseResult(
    bool Started,
    long SolicitudesExitosas,
    long SolicitudesFallidas,
    double TiempoRespuestaMedioMs,
    double LatenciaP95Ms,
    double ThroughputExitosoRps);

/// <summary>
/// Motor de generación de carga HTTP GET. Mide solo la solicitud HTTP; el intervalo entre
/// solicitudes no forma parte de la latencia. Implementado en Infrastructure (NBomber).
/// </summary>
public interface ILoadRunner
{
    Task<LoadPhaseResult> RunPhaseAsync(LoadPhaseRequest request, CancellationToken cancellationToken = default);
}
