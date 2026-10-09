using DotNetPerformanceComparator.Application.Projects.Incorporation;
using DotNetPerformanceComparator.Domain.Constants;
using DotNetPerformanceComparator.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace DotNetPerformanceComparator.Application.Migrations.ExperimentalMigration;

/// <summary>
/// Migración experimental controlada: verificar el proyecto base, copiarlo, cambiar <c>net8.0</c> por
/// <c>net10.0</c> solo en la copia, restaurar y compilar con un SDK .NET 10 estable instalado.
/// No modifica el proyecto original, no actualiza paquetes ni corrige código automáticamente.
/// </summary>
public sealed class MigrarProyectoService : IMigrarProyectoService
{
    private const int MajorDestino = 10;
    private const int MaxDetalle = 1000;

    private readonly IProyectoDotNetRepository _proyectos;
    private readonly IIncorporarProyectoService _incorporacion;
    private readonly IProjectFileEditor _editor;
    private readonly IProjectCopyService _copia;
    private readonly IDotNetCommandRunner _dotnet;
    private readonly IMigracionRepository _migraciones;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<MigrarProyectoService> _logger;

    public MigrarProyectoService(
        IProyectoDotNetRepository proyectos,
        IIncorporarProyectoService incorporacion,
        IProjectFileEditor editor,
        IProjectCopyService copia,
        IDotNetCommandRunner dotnet,
        IMigracionRepository migraciones,
        TimeProvider timeProvider,
        ILogger<MigrarProyectoService> logger)
    {
        _proyectos = proyectos;
        _incorporacion = incorporacion;
        _editor = editor;
        _copia = copia;
        _dotnet = dotnet;
        _migraciones = migraciones;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ProyectoResumen>> ObtenerProyectosAsync(CancellationToken cancellationToken = default)
    {
        var proyectos = await _proyectos.GetAllAsync(cancellationToken);
        return proyectos.Select(p => new ProyectoResumen(p.Id, p.Nombre, p.RutaOriginal)).ToList();
    }

    public async Task<MigrarProyectoResult> MigrarAsync(
        MigrarProyectoRequest request,
        IProgress<MigrationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var ejecucion = new Reportero(progress);

        // 1. Validación del proyecto base (precondiciones: no se crea nada todavía).
        ejecucion.Etapa(MigrationStage.ValidacionProyecto, MigrationStageStatus.EnProceso);
        Preparacion preparacion;
        try
        {
            preparacion = await PrepararAsync(request, ejecucion, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            preparacion = Preparacion.Fallo("La migración fue cancelada antes de comenzar.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al validar la migración del proyecto {ProyectoId}.", request.ProyectoId);
            preparacion = Preparacion.Fallo("Ocurrió un error inesperado al validar el proyecto base. No se realizó ninguna copia.");
        }

        if (preparacion.Error is not null)
        {
            ejecucion.Etapa(MigrationStage.ValidacionProyecto, MigrationStageStatus.Error);
            ejecucion.Log($"Error: {preparacion.Error}");
            return new MigrarProyectoResult(false, preparacion.Error);
        }

        var (proyecto, csprojOriginal, destino, sdk) = (preparacion.Proyecto!, preparacion.Csproj!, preparacion.Destino!, preparacion.Sdk!);
        ejecucion.Etapa(MigrationStage.ValidacionProyecto, MigrationStageStatus.Correcto);

        var migracion = new Migracion
        {
            ProyectoId = proyecto.Id,
            VersionOrigen = VersionesDotNet.Net8,
            VersionDestino = VersionesDotNet.Net10,
            Estado = EstadosMigracion.EnProceso,
            FechaInicio = Ahora()
        };

        try
        {
            await _migraciones.AddAsync(migracion, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "No fue posible registrar la migración del proyecto {ProyectoId}.", proyecto.Id);
            const string mensaje = "No fue posible registrar la migración. No se realizó ninguna copia.";
            ejecucion.Log($"Error: {mensaje}");
            return new MigrarProyectoResult(false, mensaje);
        }

        var etapa = MigrationStage.CopiaExperimental;
        try
        {
            // 2. Creación de copia experimental.
            ejecucion.Etapa(etapa, MigrationStageStatus.EnProceso);
            ejecucion.Log($"Copiando el proyecto en {destino.FullPath} (se excluyen {string.Join(", ", ProjectCopyRules.ExcludedDirectories)})...");
            var copia = await _copia.CopyAsync(Path.GetDirectoryName(csprojOriginal)!, destino, cancellationToken);
            if (!copia.Success)
            {
                return await FallarAsync(migracion, proyecto, sdk, etapa, $"No fue posible crear la copia experimental. {copia.ErrorMessage}", null, ejecucion);
            }

            migracion.RutaMigrada = destino.FullPath;
            ejecucion.Log($"Copia creada: {copia.FilesCopied} archivo(s).");
            foreach (var enlace in copia.SkippedLinks)
            {
                ejecucion.Log($"Advertencia: se omitió el enlace simbólico {enlace} (no se siguen enlaces).");
            }

            ejecucion.Etapa(etapa, MigrationStageStatus.Correcto);

            // 3. Actualización del framework objetivo (solo en la copia).
            etapa = MigrationStage.ActualizacionFramework;
            ejecucion.Etapa(etapa, MigrationStageStatus.EnProceso);
            var csprojCopia = Path.Combine(destino.FullPath!, Path.GetFileName(csprojOriginal));
            var tfm = _editor.UpdateTargetFramework(csprojCopia);
            if (!tfm.Updated)
            {
                return await FallarAsync(migracion, proyecto, sdk, etapa, $"No fue posible actualizar el framework objetivo de la copia. {tfm.ErrorMessage}", null, ejecucion);
            }

            ejecucion.Log($"Framework objetivo de la copia: {string.Join(";", tfm.TargetFrameworks)}");
            ejecucion.Etapa(etapa, MigrationStageStatus.Correcto);

            // 4. Restauración de dependencias con el SDK .NET 10.
            etapa = MigrationStage.Restauracion;
            ejecucion.Etapa(etapa, MigrationStageStatus.EnProceso);
            ejecucion.Log($"Ejecutando: dotnet restore (SDK {sdk})");
            var restore = await _dotnet.RunAsync(["restore", csprojCopia, "--disable-build-servers"], sdk, ejecucion.Log, cancellationToken);
            if (!restore.Succeeded)
            {
                return await FallarAsync(migracion, proyecto, sdk, etapa, $"La restauración de dependencias falló{CodigoSalida(restore)}.", restore, ejecucion);
            }

            ejecucion.Etapa(etapa, MigrationStageStatus.Correcto);

            // 5. Build preliminar .NET 10 (sin volver a restaurar).
            etapa = MigrationStage.Build;
            ejecucion.Etapa(etapa, MigrationStageStatus.EnProceso);
            ejecucion.Log($"Ejecutando: dotnet build --framework {VersionesDotNet.Net10Tfm} (SDK {sdk})");
            var build = await _dotnet.RunAsync(
                ["build", csprojCopia, "--no-restore", "--framework", VersionesDotNet.Net10Tfm, "--disable-build-servers"],
                sdk, ejecucion.Log, cancellationToken);
            if (!build.Succeeded)
            {
                return await FallarAsync(migracion, proyecto, sdk, etapa, $"El build preliminar con .NET 10 falló{CodigoSalida(build)}.", build, ejecucion);
            }

            migracion.Estado = EstadosMigracion.Exitosa;
            migracion.FechaFin = Ahora();
            migracion.Detalle = $"SDK utilizado: {sdk}. Restauración y build preliminar .NET 10 correctos.";
            await _migraciones.UpdateAsync(migracion, CancellationToken.None);
            ejecucion.Etapa(etapa, MigrationStageStatus.Correcto);
            ejecucion.Log("Migración experimental finalizada correctamente.");

            return new MigrarProyectoResult(
                true,
                $"La copia experimental .NET 10 se preparó correctamente: restauración y build preliminar completados con el SDK {sdk}.",
                Resumen(migracion, proyecto, sdk));
        }
        catch (OperationCanceledException)
        {
            return await FallarAsync(migracion, proyecto, sdk, etapa, "La migración fue cancelada.", null, ejecucion);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado durante la migración {MigracionId}.", migracion.Id);
            return await FallarAsync(migracion, proyecto, sdk, etapa, "Ocurrió un error inesperado durante la migración.", null, ejecucion);
        }
    }

    private async Task<Preparacion> PrepararAsync(MigrarProyectoRequest request, Reportero ejecucion, CancellationToken cancellationToken)
    {
        if (request.ProyectoId is not { } proyectoId)
        {
            return Preparacion.Fallo("Seleccione el proyecto base que desea migrar.");
        }

        var proyecto = await _proyectos.GetByIdAsync(proyectoId, cancellationToken);
        if (proyecto is null)
        {
            return Preparacion.Fallo("El proyecto seleccionado no existe.");
        }

        ejecucion.Log($"Proyecto base: {proyecto.Nombre}");
        ejecucion.Log($"Verificando {proyecto.RutaOriginal}...");

        // Reutiliza la inspección de CU-01: el proyecto debe seguir siendo ASP.NET Core Web con net8.0.
        var inspeccion = await _incorporacion.InspeccionarAsync(proyecto.RutaOriginal, cancellationToken);
        if (!inspeccion.EsValido)
        {
            return Preparacion.Fallo($"El proyecto base ya no cumple las condiciones para migrarse. {inspeccion.Mensaje}");
        }

        var csproj = inspeccion.RutaProyecto!;
        var directorioProyecto = Path.GetDirectoryName(csproj)!;
        ejecucion.Log("Proyecto base verificado: aplicación ASP.NET Core Web con net8.0.");

        var externas = _editor.FindExternalProjectReferences(csproj, directorioProyecto);
        if (externas.Count > 0)
        {
            return Preparacion.Fallo(
                $"El proyecto contiene referencias a proyectos ubicados fuera de su directorio ({string.Join(", ", externas)}). " +
                "Esta versión de la herramienta no copia proyectos externos.");
        }

        var destino = _copia.CheckDestination(directorioProyecto, request.DirectorioSalida);
        if (!destino.IsValid)
        {
            return Preparacion.Fallo(MensajeDestino(destino.Status));
        }

        ejecucion.Log($"Directorio de salida: {destino.FullPath}");

        var sdks = await _dotnet.RunAsync(["--list-sdks"], null, null, cancellationToken);
        if (!sdks.Succeeded)
        {
            return Preparacion.Fallo("No fue posible consultar los SDK de .NET instalados. Verifique que el comando dotnet esté disponible.");
        }

        var sdk = DotNetSdkSelector.SelectLatestStable(sdks.Output, MajorDestino);
        if (sdk is null)
        {
            return Preparacion.Fallo(DotNetSdkSelector.HasPreview(sdks.Output, MajorDestino)
                ? "Solo se encontraron versiones preliminares del SDK .NET 10. Se requiere un SDK .NET 10 estable instalado."
                : "No se encontró un SDK .NET 10 estable instalado. Instálelo para realizar la migración experimental (la herramienta no lo instala automáticamente).");
        }

        // Comprueba que el SDK seleccionado es realmente el que resuelve el host en el entorno aislado.
        var version = await _dotnet.RunAsync(["--version"], sdk, null, cancellationToken);
        var resuelto = version.Output.FirstOrDefault(l => !string.IsNullOrWhiteSpace(l))?.Trim();
        if (!version.Succeeded || !string.Equals(resuelto, sdk, StringComparison.Ordinal))
        {
            return Preparacion.Fallo($"No fue posible utilizar el SDK .NET {sdk} de forma aislada para la migración.");
        }

        ejecucion.Log($"SDK .NET 10 seleccionado y verificado: {sdk}");
        return new Preparacion(null, proyecto, csproj, destino, sdk);
    }

    private async Task<MigrarProyectoResult> FallarAsync(
        Migracion migracion,
        ProyectoDotNet proyecto,
        string sdk,
        MigrationStage etapa,
        string mensaje,
        DotNetCommandResult? salida,
        Reportero ejecucion)
    {
        ejecucion.Etapa(etapa, MigrationStageStatus.Error);
        ejecucion.Log($"Error: {mensaje}");

        var detalle = $"{MigrationStages.Descripcion(etapa)}: {mensaje}";
        var relevantes = salida is null ? null : LineasRelevantes(salida.Output);
        if (!string.IsNullOrEmpty(relevantes))
        {
            detalle += $" {relevantes}";
        }

        migracion.Estado = EstadosMigracion.Fallida;
        migracion.FechaFin = Ahora();
        migracion.Detalle = Truncar($"{detalle} SDK: {sdk}.", MaxDetalle);

        try
        {
            await _migraciones.UpdateAsync(migracion, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No fue posible actualizar el estado de la migración {MigracionId}.", migracion.Id);
        }

        var conservada = migracion.RutaMigrada is not null
            ? " La copia experimental se conservó para diagnóstico."
            : string.Empty;

        return new MigrarProyectoResult(false, mensaje + conservada, Resumen(migracion, proyecto, sdk));
    }

    private DateTime Ahora() => _timeProvider.GetUtcNow().UtcDateTime;

    private static MigracionResumen Resumen(Migracion m, ProyectoDotNet proyecto, string? sdk) =>
        new(m.Id, proyecto.Nombre, m.VersionOrigen, m.VersionDestino, sdk, m.RutaMigrada, m.FechaInicio, m.FechaFin, m.Estado, m.Detalle);

    private static string CodigoSalida(DotNetCommandResult r) =>
        r.Started ? $" (código de salida {r.ExitCode})" : " (no fue posible iniciar el comando dotnet)";

    /// <summary>Hasta tres líneas de error de la salida de dotnet, o la última línea no vacía.</summary>
    private static string? LineasRelevantes(IReadOnlyList<string> salida)
    {
        var errores = salida
            .Where(l => l.Contains("error", StringComparison.OrdinalIgnoreCase))
            .Select(l => Truncar(l.Trim(), 300))
            .Distinct()
            .Take(3)
            .ToList();

        if (errores.Count > 0)
        {
            return string.Join(" | ", errores);
        }

        return salida.LastOrDefault(l => !string.IsNullOrWhiteSpace(l))?.Trim() is { } ultima ? Truncar(ultima, 300) : null;
    }

    private static string Truncar(string texto, int maximo) =>
        texto.Length <= maximo ? texto : texto[..(maximo - 1)] + "…";

    private static string MensajeDestino(DestinationStatus status) => status switch
    {
        DestinationStatus.RutaVacia => "Indique el directorio de salida de la copia experimental.",
        DestinationStatus.RutaInvalida => "El directorio de salida debe ser una ruta absoluta válida.",
        DestinationStatus.IgualAlOrigen => "El directorio de salida no puede ser el mismo directorio del proyecto original.",
        DestinationStatus.DentroDelOrigen => "El directorio de salida no puede estar dentro del proyecto original.",
        DestinationStatus.ContieneAlOrigen => "El directorio de salida no puede contener al proyecto original.",
        DestinationStatus.EsArchivo => "La ruta de salida corresponde a un archivo existente; indique un directorio.",
        DestinationStatus.NoVacio => "El directorio de salida ya existe y no está vacío. Indique un directorio nuevo o vacío; no se sobrescribe contenido existente.",
        DestinationStatus.PadreNoExiste => "La carpeta que contendría el directorio de salida no existe.",
        _ => "El directorio de salida no es válido."
    };

    private sealed record Preparacion(string? Error, ProyectoDotNet? Proyecto, string? Csproj, DestinationCheck? Destino, string? Sdk)
    {
        public static Preparacion Fallo(string error) => new(error, null, null, null, null);
    }

    /// <summary>Publica el progreso de forma segura aunque no haya receptor.</summary>
    private sealed class Reportero(IProgress<MigrationProgress>? progress)
    {
        public void Etapa(MigrationStage etapa, MigrationStageStatus estado) =>
            progress?.Report(MigrationProgress.StageChanged(etapa, estado));

        public void Log(string linea) => progress?.Report(MigrationProgress.Log(linea));
    }
}
