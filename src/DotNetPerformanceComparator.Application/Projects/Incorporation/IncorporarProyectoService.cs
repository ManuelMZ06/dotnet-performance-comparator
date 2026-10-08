using DotNetPerformanceComparator.Domain.Constants;
using DotNetPerformanceComparator.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace DotNetPerformanceComparator.Application.Projects.Incorporation;

public sealed class IncorporarProyectoService : IIncorporarProyectoService
{
    public const string SdkWeb = "Microsoft.NET.Sdk.Web";
    public const string TipoAspNetCoreWeb = "ASP.NET Core Web";

    public const string ValidacionArchivo = "Se localizó el archivo .csproj";
    public const string ValidacionWeb = "El proyecto corresponde a una aplicación web ASP.NET";
    public const string ValidacionVersion = "La versión base es compatible con el proceso de evaluación";
    public const string ValidacionEstructura = "La estructura del proyecto permite continuar con la migración experimental";

    private readonly IProjectInspector _inspector;
    private readonly IProyectoDotNetRepository _repository;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<IncorporarProyectoService> _logger;

    public IncorporarProyectoService(
        IProjectInspector inspector,
        IProyectoDotNetRepository repository,
        TimeProvider timeProvider,
        ILogger<IncorporarProyectoService> logger)
    {
        _inspector = inspector;
        _repository = repository;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <summary>Validaciones de M-02 antes de realizar una inspección.</summary>
    public static IReadOnlyList<ProjectValidation> ValidacionesPendientes { get; } =
    [
        new(ValidacionArchivo, ValidationStatus.Pendiente),
        new(ValidacionWeb, ValidationStatus.Pendiente),
        new(ValidacionVersion, ValidationStatus.Pendiente),
        new(ValidacionEstructura, ValidationStatus.Pendiente)
    ];

    public async Task<ProjectInspectionResult> InspeccionarAsync(string? rutaProyecto, CancellationToken cancellationToken = default)
    {
        var data = await _inspector.InspectAsync(rutaProyecto?.Trim(), cancellationToken);
        return Evaluar(data);
    }

    public async Task<IncorporarProyectoResult> RegistrarAsync(IncorporarProyectoRequest request, CancellationToken cancellationToken = default)
    {
        var nombre = request.Nombre?.Trim();
        if (string.IsNullOrEmpty(nombre))
        {
            return IncorporarProyectoResult.Fallo("Indique un nombre para el proyecto.");
        }

        // Se vuelve a inspeccionar para no registrar un proyecto que cambió o nunca fue validado.
        var inspeccion = await InspeccionarAsync(request.RutaProyecto, cancellationToken);
        if (!inspeccion.EsValido)
        {
            return IncorporarProyectoResult.Fallo($"No se puede registrar el proyecto. {inspeccion.Mensaje}");
        }

        var proyecto = new ProyectoDotNet
        {
            Nombre = nombre,
            RutaOriginal = inspeccion.RutaProyecto!,
            VersionBase = VersionesDotNet.Net8,
            EstadoValidacion = EstadosValidacionProyecto.Valido,
            FechaRegistro = _timeProvider.GetUtcNow().UtcDateTime
        };

        try
        {
            await _repository.AddAsync(proyecto, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error al registrar el proyecto {Ruta}.", proyecto.RutaOriginal);
            return IncorporarProyectoResult.Fallo("No fue posible guardar el proyecto. Inténtelo nuevamente.");
        }

        return new IncorporarProyectoResult(true, $"El proyecto \"{nombre}\" se registró correctamente.", proyecto.Id);
    }

    private static ProjectInspectionResult Evaluar(ProjectInspectionData data)
    {
        var archivoLocalizado = data.FileStatus is ProjectFileStatus.Leido
            or ProjectFileStatus.ContenidoInvalido
            or ProjectFileStatus.AccesoDenegado
            or ProjectFileStatus.NoLegible;

        if (data.FileStatus != ProjectFileStatus.Leido)
        {
            return Resultado(
                data,
                tipo: null,
                version: null,
                framework: null,
                archivo: archivoLocalizado ? ValidationStatus.Correcto : ValidationStatus.Error,
                web: ValidationStatus.Pendiente,
                net8: ValidationStatus.Pendiente,
                mensaje: MensajeArchivo(data.FileStatus));
        }

        var esWeb = data.Sdks.Contains(SdkWeb, StringComparer.OrdinalIgnoreCase);
        var tieneNet8 = data.TargetFrameworks.Contains(VersionesDotNet.Net8Tfm, StringComparer.OrdinalIgnoreCase);

        var mensaje = !esWeb
            ? $"El proyecto no corresponde a una aplicación web ASP.NET Core compatible (se requiere el SDK {SdkWeb})."
            : data.TargetFrameworks.Count == 0
                ? "El archivo .csproj no declara TargetFramework ni TargetFrameworks."
                : !tieneNet8
                    ? $"El proyecto utiliza un framework diferente de .NET 8 ({string.Join(", ", data.TargetFrameworks)})."
                    : "El proyecto cumple las condiciones para ser incorporado. Revise el nombre y guárdelo.";

        return Resultado(
            data,
            tipo: esWeb ? TipoAspNetCoreWeb : null,
            version: tieneNet8 ? VersionesDotNet.Net8 : null,
            framework: tieneNet8 ? VersionesDotNet.Net8Tfm : NullIfEmpty(string.Join("; ", data.TargetFrameworks)),
            archivo: ValidationStatus.Correcto,
            web: esWeb ? ValidationStatus.Correcto : ValidationStatus.Error,
            net8: tieneNet8 ? ValidationStatus.Correcto : ValidationStatus.Error,
            mensaje: mensaje);
    }

    private static ProjectInspectionResult Resultado(
        ProjectInspectionData data,
        string? tipo,
        string? version,
        string? framework,
        ValidationStatus archivo,
        ValidationStatus web,
        ValidationStatus net8,
        string mensaje)
    {
        var valido = archivo == ValidationStatus.Correcto
            && web == ValidationStatus.Correcto
            && net8 == ValidationStatus.Correcto;

        return new ProjectInspectionResult(
            data.FullPath,
            data.ProjectName,
            tipo,
            version,
            framework,
            data.KeyFolders,
            data.DetectedFiles,
            valido ? ProjectInspectionState.Valido : ProjectInspectionState.NoValido,
            [
                new(ValidacionArchivo, archivo),
                new(ValidacionWeb, web),
                new(ValidacionVersion, net8),
                new(ValidacionEstructura, valido ? ValidationStatus.Correcto : ValidationStatus.Error)
            ],
            mensaje);
    }

    private static string MensajeArchivo(ProjectFileStatus status) => status switch
    {
        ProjectFileStatus.RutaVacia => "Indique la ruta absoluta del archivo .csproj.",
        ProjectFileStatus.RutaInvalida => "La ruta indicada no es una ruta absoluta válida.",
        ProjectFileStatus.ExtensionInvalida => "La ruta debe corresponder a un archivo con extensión .csproj.",
        ProjectFileStatus.NoExiste => "El archivo de proyecto indicado no existe.",
        ProjectFileStatus.EsEnlaceSimbolico => "La ruta corresponde a un enlace simbólico. Indique la ruta real del archivo .csproj.",
        ProjectFileStatus.AccesoDenegado => "No se tiene permiso para leer el archivo de proyecto.",
        ProjectFileStatus.NoLegible => "No fue posible leer el archivo de proyecto.",
        ProjectFileStatus.ContenidoInvalido => "El archivo no contiene un proyecto MSBuild válido (XML inválido o estructura no reconocida).",
        _ => "No fue posible inspeccionar el proyecto."
    };

    private static string? NullIfEmpty(string value) => value.Length == 0 ? null : value;
}
